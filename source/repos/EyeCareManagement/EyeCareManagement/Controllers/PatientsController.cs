using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using EyeCareManagement.Data;
using EyeCareManagement.Models;

namespace EyeCareManagement.Controllers
{
    [Authorize(Policy = "DoctorOrEmployee")]
    public class PatientsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PatientsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Patients
        public async Task<IActionResult> Index(string searchString, string sortOrder)
        {
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";
            ViewData["CurrentFilter"] = searchString;

            var patients = from p in _context.Patients
                           select p;

            if (!String.IsNullOrEmpty(searchString))
            {
                patients = patients.Where(p => p.FirstName.Contains(searchString)
                                       || p.LastName.Contains(searchString)
                                       || p.Email.Contains(searchString)
                                       || p.PhoneNumber.Contains(searchString));
            }

            switch (sortOrder)
            {
                case "name_desc":
                    patients = patients.OrderByDescending(p => p.LastName);
                    break;
                case "Date":
                    patients = patients.OrderBy(p => p.CreatedAt);
                    break;
                case "date_desc":
                    patients = patients.OrderByDescending(p => p.CreatedAt);
                    break;
                default:
                    patients = patients.OrderBy(p => p.LastName);
                    break;
            }

            return View(await patients.AsNoTracking().ToListAsync());
        }

        // GET: Patients/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .Include(p => p.MedicalRecords)
                    .ThenInclude(mr => mr.Doctor)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Doctor)
                .Include(p => p.WardAllocations)
                    .ThenInclude(wa => wa.Ward)
                .Include(p => p.OpticalOrders)
                    .ThenInclude(oo => oo.Doctor)
                .Include(p => p.Bills)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (patient == null)
            {
                return NotFound();
            }

            return View(patient);
        }

        // GET: Patients/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Patients/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FirstName,LastName,Email,PhoneNumber,DateOfBirth,Gender,Address,EmergencyContact,EmergencyContactPhone")] Patient patient)
        {
            if (ModelState.IsValid)
            {
                // Check if email already exists
                var existingPatient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.Email == patient.Email);

                if (existingPatient != null)
                {
                    ModelState.AddModelError("Email", "A patient with this email already exists.");
                    return View(patient);
                }

                patient.CreatedAt = DateTime.UtcNow;
                _context.Add(patient);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Patient created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(patient);
        }

        // GET: Patients/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients.FindAsync(id);
            if (patient == null)
            {
                return NotFound();
            }
            return View(patient);
        }

        // POST: Patients/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName,Email,PhoneNumber,DateOfBirth,Gender,Address,EmergencyContact,EmergencyContactPhone,IsActive")] Patient patient)
        {
            if (id != patient.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if email already exists for another patient
                    var existingPatient = await _context.Patients
                        .FirstOrDefaultAsync(p => p.Email == patient.Email && p.Id != patient.Id);

                    if (existingPatient != null)
                    {
                        ModelState.AddModelError("Email", "A patient with this email already exists.");
                        return View(patient);
                    }

                    var originalPatient = await _context.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
                    patient.CreatedAt = originalPatient?.CreatedAt ?? DateTime.UtcNow;
                    patient.UpdatedAt = DateTime.UtcNow;

                    _context.Update(patient);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Patient updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PatientExists(patient.Id))
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
            return View(patient);
        }

        // GET: Patients/Delete/5
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .FirstOrDefaultAsync(m => m.Id == id);
            if (patient == null)
            {
                return NotFound();
            }

            return View(patient);
        }

        // POST: Patients/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient != null)
            {
                _context.Patients.Remove(patient);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Patient deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Patients/MedicalHistory/5
        public async Task<IActionResult> MedicalHistory(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .Include(p => p.MedicalRecords)
                    .ThenInclude(mr => mr.Doctor)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (patient == null)
            {
                return NotFound();
            }

            return View(patient);
        }

        private bool PatientExists(int id)
        {
            return _context.Patients.Any(e => e.Id == id);
        }
    }
}