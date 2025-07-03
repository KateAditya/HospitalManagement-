using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace EyeCareManagement.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public UserRole Role { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public virtual ICollection<OpticalOrder> OpticalOrders { get; set; } = new List<OpticalOrder>();
        public virtual ICollection<Bill> Bills { get; set; } = new List<Bill>();
    }

    public enum UserRole
    {
        Admin = 0,
        Employee = 1,
        Doctor = 2,
        Optician = 3
    }
}