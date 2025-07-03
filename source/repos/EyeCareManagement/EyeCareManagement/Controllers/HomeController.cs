using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using EyeCareManagement.Data;
using EyeCareManagement.Models;
using System.Diagnostics;

namespace EyeCareManagement.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var dashboardStats = new DashboardStatsViewModel
            {
                TotalPatients = await _context.Patients.CountAsync(),
                TodayAppointments = await _context.Appointments
                    .Where(a => a.AppointmentDate.Date == DateTime.Today)
                    .CountAsync(),
                ActiveWardAllocations = await _context.WardAllocations
                    .Where(wa => wa.Status == AllocationStatus.Active)
                    .CountAsync(),
                PendingOpticalOrders = await _context.OpticalOrders
                    .Where(oo => oo.Status == OpticalOrderStatus.Pending)
                    .CountAsync(),
                TodayRevenue = await _context.Bills
                    .Where(b => b.BillDate.Date == DateTime.Today && b.Status == BillStatus.Paid)
                    .SumAsync(b => b.TotalAmount),
                PendingBills = await _context.Bills
                    .Where(b => b.Status == BillStatus.Pending)
                    .CountAsync()
            };

            ViewBag.RecentPatients = await _context.Patients
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.TodayAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.AppointmentDate.Date == DateTime.Today)
                .OrderBy(a => a.AppointmentTime)
                .Take(10)
                .ToListAsync();

            return View(dashboardStats);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class DashboardStatsViewModel
    {
        public int TotalPatients { get; set; }
        public int TodayAppointments { get; set; }
        public int ActiveWardAllocations { get; set; }
        public int PendingOpticalOrders { get; set; }
        public decimal TodayRevenue { get; set; }
        public int PendingBills { get; set; }
    }

    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}