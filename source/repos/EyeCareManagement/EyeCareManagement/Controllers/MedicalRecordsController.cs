using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using EyeCareManagement.Data;
using EyeCareManagement.Models;

namespace EyeCareManagement.Controllers
{
    [Authorize(Policy = "DoctorOnly")]
    public class MedicalRecordsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MedicalRecordsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: MedicalRecords
        public async Task<IActionResult> Index(int? patientId, string searchString)
        {
            ViewBag.PatientId = patientId;
            ViewBag.SearchString = searchString;

            var records = _context.MedicalRecords
                .Include(mr => mr.Patient)
                .Include(mr => mr.Doctor)
                .AsQueryable();

            if (patientId.HasValue)
            {
                records = records.Where(mr => mr.PatientId == patientId.Value);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                records = records.Where(mr => mr.Patient.FirstName.Contains(searchString) ||
                                            mr.Patient.LastName.Contains(searchString) ||
                                            mr.Diagnosis.Contains(searchString));
            }

            var result = await records
                .OrderByDescending(mr => mr.VisitDate)
                .ToListAsync();

            return View(result);
        }

        // GET: MedicalRecords/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medicalRecord = await _context.MedicalRecords
                .Include(mr => mr.Patient)
                .Include(mr => mr.Doctor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (medicalRecord == null)
            {
                return NotFound();
            }

            return View(medicalRecord);
        }

        // GET: MedicalRecords/Create
        public async Task<IActionResult> Create(int? patientId)
        {
            await PopulateDropdownsAsync();

            var record = new MedicalRecord
            {
                VisitDate = DateTime.Today,
                DoctorId = _userManager.GetUserId(User)
            };

            if (patientId.HasValue)
            {
                record.PatientId = patientId.Value;
                ViewBag.PatientName = await _context.Patients
                    .Where(p => p.Id == patientId.Value)
                    .Select(p => p.FirstName + " " + p.LastName)
                    .FirstOrDefaultAsync();
            }

            return View(record);
        }

        // POST: MedicalRecords/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PatientId,DoctorId,VisitDate,Diagnosis,Treatment,Prescription,Notes,VisualAcuityLeft,VisualAcuityRight,IntraocularPressureLeft,IntraocularPressureRight,NextVisitDate")] MedicalRecord medicalRecord)
        {
            // Ensure DoctorId is set if not provided
            if (string.IsNullOrEmpty(medicalRecord.DoctorId))
            {
                medicalRecord.DoctorId = _userManager.GetUserId(User) ?? "";
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
                return View(medicalRecord);
            }

            try
            {
                medicalRecord.CreatedAt = DateTime.UtcNow;
                _context.Add(medicalRecord);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Medical record created successfully.";
                return RedirectToAction("Details", "Patients", new { id = medicalRecord.PatientId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error saving medical record: {ex.Message}";
            }

            await PopulateDropdownsAsync();
            return View(medicalRecord);
        }

        // GET: MedicalRecords/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medicalRecord = await _context.MedicalRecords.FindAsync(id);
            if (medicalRecord == null)
            {
                return NotFound();
            }

            // Only allow editing by the doctor who created the record or admin
            var currentUserId = _userManager.GetUserId(User);
            if (medicalRecord.DoctorId != currentUserId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            await PopulateDropdownsAsync();
            return View(medicalRecord);
        }

        // POST: MedicalRecords/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,PatientId,DoctorId,VisitDate,Diagnosis,Treatment,Prescription,Notes,VisualAcuityLeft,VisualAcuityRight,IntraocularPressureLeft,IntraocularPressureRight,NextVisitDate")] MedicalRecord medicalRecord)
        {
            if (id != medicalRecord.Id)
            {
                return NotFound();
            }

            // Only allow editing by the doctor who created the record or admin
            var currentUserId = _userManager.GetUserId(User);
            if (medicalRecord.DoctorId != currentUserId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var originalRecord = await _context.MedicalRecords.AsNoTracking().FirstOrDefaultAsync(mr => mr.Id == id);
                    medicalRecord.CreatedAt = originalRecord?.CreatedAt ?? DateTime.UtcNow;

                    _context.Update(medicalRecord);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Medical record updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MedicalRecordExists(medicalRecord.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Details", "Patients", new { id = medicalRecord.PatientId });
            }

            await PopulateDropdownsAsync();
            return View(medicalRecord);
        }

        // GET: MedicalRecords/Delete/5
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medicalRecord = await _context.MedicalRecords
                .Include(mr => mr.Patient)
                .Include(mr => mr.Doctor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (medicalRecord == null)
            {
                return NotFound();
            }

            return View(medicalRecord);
        }

        // POST: MedicalRecords/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var medicalRecord = await _context.MedicalRecords.FindAsync(id);
            if (medicalRecord != null)
            {
                _context.MedicalRecords.Remove(medicalRecord);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Medical record deleted successfully.";
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

            ViewBag.PatientId = new SelectList(patients, "Id", "Name");
            ViewBag.DoctorId = new SelectList(doctorList, "Id", "Name");
        }

        private bool MedicalRecordExists(int id)
        {
            return _context.MedicalRecords.Any(e => e.Id == id);
        }
    }
}