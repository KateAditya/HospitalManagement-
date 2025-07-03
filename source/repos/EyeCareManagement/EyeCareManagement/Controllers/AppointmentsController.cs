using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using EyeCareManagement.Data;
using EyeCareManagement.Models;

namespace EyeCareManagement.Controllers
{
    [Authorize(Policy = "DoctorOrEmployee")]
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AppointmentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Appointments
        public async Task<IActionResult> Index(DateTime? date, string doctorId, AppointmentStatus? status)
        {
            ViewBag.SelectedDate = date ?? DateTime.Today;
            ViewBag.DoctorId = doctorId;
            ViewBag.Status = status;

            var doctors = await _userManager.GetUsersInRoleAsync("Doctor");
            ViewBag.Doctors = new SelectList(doctors, "Id", "FirstName");

            var appointments = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .AsQueryable();

            if (date.HasValue)
            {
                appointments = appointments.Where(a => a.AppointmentDate.Date == date.Value.Date);
            }

            if (!string.IsNullOrEmpty(doctorId))
            {
                appointments = appointments.Where(a => a.DoctorId == doctorId);
            }

            if (status.HasValue)
            {
                appointments = appointments.Where(a => a.Status == status.Value);
            }

            var result = await appointments
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .ToListAsync();

            return View(result);
        }

        // GET: Appointments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
        }

        // GET: Appointments/Create
        public async Task<IActionResult> Create(int? patientId)
        {
            await PopulateDropdownsAsync();

            var appointment = new Appointment();
            if (patientId.HasValue)
            {
                appointment.PatientId = patientId.Value;
                ViewBag.PatientName = await _context.Patients
                    .Where(p => p.Id == patientId.Value)
                    .Select(p => p.FirstName + " " + p.LastName)
                    .FirstOrDefaultAsync();
            }

            return View(appointment);
        }

        // POST: Appointments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Appointment appointment)
        {
            // Remove validation for navigation properties
            ModelState.Remove("Patient");
            ModelState.Remove("Doctor");

            if (ModelState.IsValid)
            {
                // Validate that the selected patient and doctor exist
                var patientExists = await _context.Patients.AnyAsync(p => p.Id == appointment.PatientId);
                var doctorExists = await _userManager.FindByIdAsync(appointment.DoctorId) != null;

                if (!patientExists)
                {
                    ModelState.AddModelError("PatientId", "Selected patient does not exist.");
                }

                if (!doctorExists)
                {
                    ModelState.AddModelError("DoctorId", "Selected doctor does not exist.");
                }

                // Validate appointment date is not in the past
                if (appointment.AppointmentDate.Date < DateTime.Today)
                {
                    ModelState.AddModelError("AppointmentDate", "Appointment date cannot be in the past.");
                }

                // If validation errors occurred, return to form
                if (!ModelState.IsValid)
                {
                    await PopulateDropdownsAsync();
                    return View(appointment);
                }

                // Check for scheduling conflicts
                var conflictingAppointment = await _context.Appointments
                    .Where(a => a.DoctorId == appointment.DoctorId
                           && a.AppointmentDate.Date == appointment.AppointmentDate.Date
                           && a.AppointmentTime == appointment.AppointmentTime
                           && a.Status != AppointmentStatus.Cancelled)
                    .FirstOrDefaultAsync();

                if (conflictingAppointment != null)
                {
                    ModelState.AddModelError("", "Doctor is not available at this time. Please choose a different time slot.");
                    await PopulateDropdownsAsync();
                    return View(appointment);
                }

                // Set default values
                appointment.Status = AppointmentStatus.Scheduled;
                appointment.CreatedAt = DateTime.UtcNow;

                try
                {
                    _context.Add(appointment);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Appointment scheduled successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while saving the appointment. Please try again.");
                    // Log the exception if you have logging configured
                    // _logger.LogError(ex, "Error creating appointment");
                }
            }

            await PopulateDropdownsAsync();
            return View(appointment);
        }

        // GET: Appointments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }

            await PopulateDropdownsAsync();
            return View(appointment);
        }

        // POST: Appointments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Appointment appointment)
        {
            if (id != appointment.Id)
            {
                return NotFound();
            }

            // Remove validation for navigation properties
            ModelState.Remove("Patient");
            ModelState.Remove("Doctor");

            if (ModelState.IsValid)
            {
                try
                {
                    // Check for scheduling conflicts (excluding current appointment)
                    var conflictingAppointment = await _context.Appointments
                        .Where(a => a.Id != appointment.Id
                               && a.DoctorId == appointment.DoctorId
                               && a.AppointmentDate.Date == appointment.AppointmentDate.Date
                               && a.AppointmentTime == appointment.AppointmentTime
                               && a.Status != AppointmentStatus.Cancelled)
                        .FirstOrDefaultAsync();

                    if (conflictingAppointment != null)
                    {
                        ModelState.AddModelError("", "Doctor is not available at this time. Please choose a different time slot.");
                        await PopulateDropdownsAsync();
                        return View(appointment);
                    }

                    var originalAppointment = await _context.Appointments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
                    appointment.CreatedAt = originalAppointment?.CreatedAt ?? DateTime.UtcNow;
                    appointment.UpdatedAt = DateTime.UtcNow;

                    _context.Update(appointment);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Appointment updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AppointmentExists(appointment.Id))
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
            return View(appointment);
        }

        // POST: Appointments/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, AppointmentStatus status)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }

            appointment.Status = status;
            appointment.UpdatedAt = DateTime.UtcNow;

            _context.Update(appointment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Appointment status updated to {status}.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Appointments/Delete/5
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
        }

        // POST: Appointments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Appointment deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Appointments/Calendar
        public async Task<IActionResult> Calendar()
        {
            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.Status != AppointmentStatus.Cancelled)
                .ToListAsync();

            var calendarEvents = appointments.Select(a => new
            {
                id = a.Id,
                title = $"{a.Patient.FirstName} {a.Patient.LastName} - {a.Type}",
                start = a.AppointmentDate.ToString("yyyy-MM-dd") + "T" + a.AppointmentTime.ToString(@"hh\:mm"),
                doctor = $"{a.Doctor.FirstName} {a.Doctor.LastName}",
                status = a.Status.ToString(),
                backgroundColor = GetStatusColor(a.Status)
            });

            ViewBag.CalendarEvents = System.Text.Json.JsonSerializer.Serialize(calendarEvents);
            return View();
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

        private string GetStatusColor(AppointmentStatus status)
        {
            return status switch
            {
                AppointmentStatus.Scheduled => "#007bff",
                AppointmentStatus.Confirmed => "#28a745",
                AppointmentStatus.InProgress => "#ffc107",
                AppointmentStatus.Completed => "#6f42c1",
                AppointmentStatus.Cancelled => "#dc3545",
                AppointmentStatus.NoShow => "#6c757d",
                _ => "#007bff"
            };
        }

        private bool AppointmentExists(int id)
        {
            return _context.Appointments.Any(e => e.Id == id);
        }
    }
}