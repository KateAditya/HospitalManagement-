using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using EyeCareManagement.Data;
using EyeCareManagement.Models;

namespace EyeCareManagement.Controllers
{
    [Authorize(Policy = "OpticianOrDoctor")]
    public class OpticalOrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OpticalOrdersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: OpticalOrders
        public async Task<IActionResult> Index(OpticalOrderStatus? status, string searchString)
        {
            ViewBag.Status = status;
            ViewBag.SearchString = searchString;

            var orders = _context.OpticalOrders
                .Include(oo => oo.Patient)
                .Include(oo => oo.Doctor)
                .Include(oo => oo.Optician)
                .AsQueryable();

            if (status.HasValue)
            {
                orders = orders.Where(oo => oo.Status == status.Value);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                orders = orders.Where(oo => oo.Patient.FirstName.Contains(searchString) ||
                                          oo.Patient.LastName.Contains(searchString) ||
                                          oo.FrameDetails.Contains(searchString));
            }

            // If user is optician, show only orders assigned to them or unassigned orders
            if (User.IsInRole("Optician") && !User.IsInRole("Admin"))
            {
                var currentUserId = _userManager.GetUserId(User);
                orders = orders.Where(oo => oo.OpticianId == currentUserId || oo.OpticianId == null);
            }

            var result = await orders
                .OrderByDescending(oo => oo.OrderDate)
                .ToListAsync();

            return View(result);
        }

        // GET: OpticalOrders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var opticalOrder = await _context.OpticalOrders
                .Include(oo => oo.Patient)
                .Include(oo => oo.Doctor)
                .Include(oo => oo.Optician)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (opticalOrder == null)
            {
                return NotFound();
            }

            return View(opticalOrder);
        }

        // GET: OpticalOrders/Create
        [Authorize(Policy = "DoctorOnly")]
        public async Task<IActionResult> Create(int? patientId)
        {
            await PopulateDropdownsAsync();

            var order = new OpticalOrder
            {
                OrderDate = DateTime.Today,
                DoctorId = _userManager.GetUserId(User)
            };

            if (patientId.HasValue)
            {
                order.PatientId = patientId.Value;
                ViewBag.PatientName = await _context.Patients
                    .Where(p => p.Id == patientId.Value)
                    .Select(p => p.FirstName + " " + p.LastName)
                    .FirstOrDefaultAsync();
            }

            return View(order);
        }

        // POST: OpticalOrders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "DoctorOnly")]
        public async Task<IActionResult> Create([Bind("PatientId,DoctorId,OrderDate,Type,Status,RightEyeSphere,RightEyeCylinder,RightEyeAxis,LeftEyeSphere,LeftEyeCylinder,LeftEyeAxis,PupillaryDistance,FrameDetails,LensType,LensCoating,SpecialInstructions,EstimatedCost")] OpticalOrder opticalOrder)
        {
            // Ensure DoctorId is set if not provided
            if (string.IsNullOrEmpty(opticalOrder.DoctorId))
            {
                opticalOrder.DoctorId = _userManager.GetUserId(User) ?? "";
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
                return View(opticalOrder);
            }

            try
            {
                opticalOrder.Status = OpticalOrderStatus.Pending;
                opticalOrder.CreatedAt = DateTime.UtcNow;

                _context.Add(opticalOrder);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Optical order created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error saving optical order: {ex.Message}";
            }

            await PopulateDropdownsAsync();
            return View(opticalOrder);
        }

        // GET: OpticalOrders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var opticalOrder = await _context.OpticalOrders.FindAsync(id);
            if (opticalOrder == null)
            {
                return NotFound();
            }

            // Check permissions
            var currentUserId = _userManager.GetUserId(User);
            if (User.IsInRole("Doctor") && !User.IsInRole("Admin"))
            {
                if (opticalOrder.DoctorId != currentUserId)
                {
                    return Forbid();
                }
            }
            else if (User.IsInRole("Optician") && !User.IsInRole("Admin"))
            {
                if (opticalOrder.OpticianId != currentUserId && opticalOrder.OpticianId != null)
                {
                    return Forbid();
                }
            }

            await PopulateDropdownsAsync();
            return View(opticalOrder);
        }

        // POST: OpticalOrders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,PatientId,DoctorId,OpticianId,OrderDate,CompletionDate,Type,Status,RightEyeSphere,RightEyeCylinder,RightEyeAxis,LeftEyeSphere,LeftEyeCylinder,LeftEyeAxis,PupillaryDistance,FrameDetails,LensType,LensCoating,SpecialInstructions,EstimatedCost,FinalCost")] OpticalOrder opticalOrder)
        {
            if (id != opticalOrder.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check permissions
                    var currentUserId = _userManager.GetUserId(User);
                    if (User.IsInRole("Doctor") && !User.IsInRole("Admin"))
                    {
                        if (opticalOrder.DoctorId != currentUserId)
                        {
                            return Forbid();
                        }
                    }
                    else if (User.IsInRole("Optician") && !User.IsInRole("Admin"))
                    {
                        if (opticalOrder.OpticianId != currentUserId && opticalOrder.OpticianId != null)
                        {
                            return Forbid();
                        }
                    }

                    var originalOrder = await _context.OpticalOrders.AsNoTracking().FirstOrDefaultAsync(oo => oo.Id == id);
                    opticalOrder.CreatedAt = originalOrder?.CreatedAt ?? DateTime.UtcNow;
                    opticalOrder.UpdatedAt = DateTime.UtcNow;

                    // If status is being marked as completed, set completion date
                    if (opticalOrder.Status == OpticalOrderStatus.Completed && opticalOrder.CompletionDate == null)
                    {
                        opticalOrder.CompletionDate = DateTime.UtcNow;
                    }

                    _context.Update(opticalOrder);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Optical order updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OpticalOrderExists(opticalOrder.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync();
            return View(opticalOrder);
        }

        // POST: OpticalOrders/AssignToMe/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "OpticianOnly")]
        public async Task<IActionResult> AssignToMe(int id)
        {
            var opticalOrder = await _context.OpticalOrders.FindAsync(id);
            if (opticalOrder == null)
            {
                return NotFound();
            }

            if (opticalOrder.OpticianId != null)
            {
                TempData["ErrorMessage"] = "This order is already assigned to another optician.";
                return RedirectToAction(nameof(Index));
            }

            opticalOrder.OpticianId = _userManager.GetUserId(User);
            opticalOrder.Status = OpticalOrderStatus.InProgress;
            opticalOrder.UpdatedAt = DateTime.UtcNow;

            _context.Update(opticalOrder);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Optical order assigned to you successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: OpticalOrders/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OpticalOrderStatus status)
        {
            var opticalOrder = await _context.OpticalOrders.FindAsync(id);
            if (opticalOrder == null)
            {
                return NotFound();
            }

            opticalOrder.Status = status;
            opticalOrder.UpdatedAt = DateTime.UtcNow;

            if (status == OpticalOrderStatus.Completed)
            {
                opticalOrder.CompletionDate = DateTime.UtcNow;
            }

            _context.Update(opticalOrder);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Order status updated to {status}.";
            return RedirectToAction(nameof(Index));
        }

        // GET: OpticalOrders/Delete/5
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var opticalOrder = await _context.OpticalOrders
                .Include(oo => oo.Patient)
                .Include(oo => oo.Doctor)
                .Include(oo => oo.Optician)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (opticalOrder == null)
            {
                return NotFound();
            }

            return View(opticalOrder);
        }

        // POST: OpticalOrders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var opticalOrder = await _context.OpticalOrders.FindAsync(id);
            if (opticalOrder != null)
            {
                _context.OpticalOrders.Remove(opticalOrder);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Optical order deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdownsAsync()
        {
            var patients = await _context.Patients
                .Where(p => p.IsActive)
                .OrderBy(p => p.LastName)
                .Select(p => new { p.Id, Name = p.FirstName + " " + p.LastName })
                .ToListAsync();

            var doctors = await _userManager.GetUsersInRoleAsync("Doctor");
            var doctorList = doctors.Where(d => d.IsActive)
                .Select(d => new { d.Id, Name = d.FirstName + " " + d.LastName })
                .OrderBy(d => d.Name);

            var opticians = await _userManager.GetUsersInRoleAsync("Optician");
            var opticianList = opticians.Where(o => o.IsActive)
                .Select(o => new { o.Id, Name = o.FirstName + " " + o.LastName })
                .OrderBy(o => o.Name);

            ViewBag.PatientId = new SelectList(patients, "Id", "Name");
            ViewBag.DoctorId = new SelectList(doctorList, "Id", "Name");
            ViewBag.OpticianId = new SelectList(opticianList, "Id", "Name");
        }

        private bool OpticalOrderExists(int id)
        {
            return _context.OpticalOrders.Any(e => e.Id == id);
        }
    }
}