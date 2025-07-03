using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using EyeCareManagement.Data;
using EyeCareManagement.Models;

namespace EyeCareManagement.Controllers
{
    [Authorize(Policy = "EmployeeOnly")]
    public class BillsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BillsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Bills
        public async Task<IActionResult> Index(BillStatus? status, DateTime? fromDate, DateTime? toDate, string searchString)
        {
            ViewBag.Status = status;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.SearchString = searchString;

            var bills = _context.Bills
                .Include(b => b.Patient)
                .Include(b => b.GeneratedBy)
                .AsQueryable();

            if (status.HasValue)
            {
                bills = bills.Where(b => b.Status == status.Value);
            }

            if (fromDate.HasValue)
            {
                bills = bills.Where(b => b.BillDate.Date >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                bills = bills.Where(b => b.BillDate.Date <= toDate.Value.Date);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                bills = bills.Where(b => b.Patient.FirstName.Contains(searchString) ||
                                        b.Patient.LastName.Contains(searchString) ||
                                        b.BillNumber.Contains(searchString));
            }

            var result = await bills
                .OrderByDescending(b => b.BillDate)
                .ToListAsync();

            return View(result);
        }

        // GET: Bills/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bill = await _context.Bills
                .Include(b => b.Patient)
                .Include(b => b.GeneratedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (bill == null)
            {
                return NotFound();
            }

            return View(bill);
        }

        // GET: Bills/Create
        public async Task<IActionResult> Create(int? patientId)
        {
            await PopulateDropdownsAsync();

            var bill = new Bill
            {
                BillDate = DateTime.Today,
                BillNumber = await GenerateBillNumberAsync(),
                GeneratedById = _userManager.GetUserId(User)
            };

            if (patientId.HasValue)
            {
                bill.PatientId = patientId.Value;
                ViewBag.PatientName = await _context.Patients
                    .Where(p => p.Id == patientId.Value)
                    .Select(p => p.FirstName + " " + p.LastName)
                    .FirstOrDefaultAsync();
            }

            return View(bill);
        }

        // POST: Bills/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PatientId,GeneratedById,BillNumber,BillDate,Type,Status,Description,ConsultationCharges,ProcedureCharges,OpticalCharges,WardCharges,MedicineCharges,OtherCharges,Discount,TaxAmount,PaidAmount,PaymentMethod,PaymentReference,Notes")] Bill bill)
        {
            // Ensure required fields are set
            if (string.IsNullOrEmpty(bill.GeneratedById))
            {
                bill.GeneratedById = _userManager.GetUserId(User) ?? "";
            }

            if (string.IsNullOrEmpty(bill.BillNumber))
            {
                bill.BillNumber = await GenerateBillNumberAsync();
            }

            // Debug: Check model state
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { Field = x.Key, Errors = x.Value.Errors.Select(e => e.ErrorMessage) });

                foreach (var error in errors)
                {
                    TempData["ErrorMessage"] = $"Validation error in {error.Field}: {string.Join(", ", error.Errors)}";
                }

                await PopulateDropdownsAsync();
                return View(bill);
            }

            try
            {
                // Calculate total amount
                bill.TotalAmount = bill.ConsultationCharges + bill.ProcedureCharges + bill.OpticalCharges +
                                  bill.WardCharges + bill.MedicineCharges + bill.OtherCharges - bill.Discount + bill.TaxAmount;

                bill.BalanceAmount = bill.TotalAmount - bill.PaidAmount;
                bill.Status = BillStatus.Pending;
                bill.CreatedAt = DateTime.UtcNow;

                _context.Add(bill);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Bill created successfully.";
                return RedirectToAction(nameof(Details), new { id = bill.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error saving bill: {ex.Message}";
            }

            await PopulateDropdownsAsync();
            return View(bill);
        }

        // GET: Bills/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bill = await _context.Bills.FindAsync(id);
            if (bill == null)
            {
                return NotFound();
            }

            await PopulateDropdownsAsync();
            return View(bill);
        }

        // POST: Bills/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,PatientId,GeneratedById,BillNumber,BillDate,Type,Status,Description,ConsultationCharges,ProcedureCharges,OpticalCharges,WardCharges,MedicineCharges,OtherCharges,Discount,TaxAmount,PaidAmount,PaymentMethod,PaymentReference,PaymentDate,Notes")] Bill bill)
        {
            if (id != bill.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Recalculate total and balance
                    bill.TotalAmount = bill.ConsultationCharges + bill.ProcedureCharges + bill.OpticalCharges +
                                      bill.WardCharges + bill.MedicineCharges + bill.OtherCharges - bill.Discount + bill.TaxAmount;

                    bill.BalanceAmount = bill.TotalAmount - bill.PaidAmount;

                    // Update status based on payment
                    if (bill.PaidAmount == 0)
                        bill.Status = BillStatus.Pending;
                    else if (bill.PaidAmount >= bill.TotalAmount)
                        bill.Status = BillStatus.Paid;
                    else
                        bill.Status = BillStatus.Partial;

                    var originalBill = await _context.Bills.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
                    bill.CreatedAt = originalBill?.CreatedAt ?? DateTime.UtcNow;
                    bill.UpdatedAt = DateTime.UtcNow;

                    _context.Update(bill);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Bill updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BillExists(bill.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Details), new { id = bill.Id });
            }

            await PopulateDropdownsAsync();
            return View(bill);
        }

        // POST: Bills/RecordPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordPayment(int id, decimal amount, PaymentMethod paymentMethod, string paymentReference)
        {
            var bill = await _context.Bills.FindAsync(id);
            if (bill == null)
            {
                return NotFound();
            }

            bill.PaidAmount += amount;
            bill.PaymentMethod = paymentMethod;
            bill.PaymentReference = paymentReference;
            bill.PaymentDate = DateTime.UtcNow;
            bill.BalanceAmount = bill.TotalAmount - bill.PaidAmount;

            // Update status
            if (bill.PaidAmount >= bill.TotalAmount)
                bill.Status = BillStatus.Paid;
            else
                bill.Status = BillStatus.Partial;

            bill.UpdatedAt = DateTime.UtcNow;

            _context.Update(bill);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Payment of ${amount:F2} recorded successfully.";
            return RedirectToAction(nameof(Details), new { id = bill.Id });
        }

        // GET: Bills/Print/5
        public async Task<IActionResult> Print(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bill = await _context.Bills
                .Include(b => b.Patient)
                .Include(b => b.GeneratedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (bill == null)
            {
                return NotFound();
            }

            return View(bill);
        }

        // GET: Bills/Delete/5
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bill = await _context.Bills
                .Include(b => b.Patient)
                .Include(b => b.GeneratedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (bill == null)
            {
                return NotFound();
            }

            return View(bill);
        }

        // POST: Bills/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var bill = await _context.Bills.FindAsync(id);
            if (bill != null)
            {
                _context.Bills.Remove(bill);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Bill deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<string> GenerateBillNumberAsync()
        {
            var today = DateTime.Today;
            var prefix = $"BILL{today:yyyyMMdd}";

            var lastBill = await _context.Bills
                .Where(b => b.BillNumber.StartsWith(prefix))
                .OrderByDescending(b => b.BillNumber)
                .FirstOrDefaultAsync();

            if (lastBill == null)
            {
                return $"{prefix}001";
            }

            var lastNumber = lastBill.BillNumber.Substring(prefix.Length);
            if (int.TryParse(lastNumber, out int number))
            {
                return $"{prefix}{(number + 1):000}";
            }

            return $"{prefix}001";
        }

        private async Task PopulateDropdownsAsync()
        {
            var patients = await _context.Patients
                .Where(p => p.IsActive)
                .OrderBy(p => p.LastName)
                .Select(p => new { p.Id, Name = p.FirstName + " " + p.LastName })
                .ToListAsync();

            ViewBag.PatientId = new SelectList(patients, "Id", "Name");
        }

        private bool BillExists(int id)
        {
            return _context.Bills.Any(e => e.Id == id);
        }
    }
}