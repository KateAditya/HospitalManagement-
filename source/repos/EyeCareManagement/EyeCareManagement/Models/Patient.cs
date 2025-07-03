using System.ComponentModel.DataAnnotations;

namespace EyeCareManagement.Models
{
    public class Patient
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public Gender Gender { get; set; }

        [StringLength(500)]
        public string Address { get; set; } = string.Empty;

        [StringLength(100)]
        public string EmergencyContact { get; set; } = string.Empty;

        [StringLength(20)]
        public string EmergencyContactPhone { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public virtual ICollection<WardAllocation> WardAllocations { get; set; } = new List<WardAllocation>();
        public virtual ICollection<OpticalOrder> OpticalOrders { get; set; } = new List<OpticalOrder>();
        public virtual ICollection<Bill> Bills { get; set; } = new List<Bill>();
    }

    public enum Gender
    {
        Male = 0,
        Female = 1,
        Other = 2
    }
}