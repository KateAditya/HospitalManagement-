using System.ComponentModel.DataAnnotations;

namespace EyeCareManagement.Models
{
    public class Ward
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int Capacity { get; set; }

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        public WardType Type { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<WardAllocation> WardAllocations { get; set; } = new List<WardAllocation>();
    }

    public class WardAllocation
    {
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required]
        public int WardId { get; set; }

        [StringLength(20)]
        public string BedNumber { get; set; } = string.Empty;

        [Required]
        public DateTime AdmissionDate { get; set; }

        public DateTime? DischargeDate { get; set; }

        [StringLength(1000)]
        public string AdmissionReason { get; set; } = string.Empty;

        [StringLength(1000)]
        public string DischargeNotes { get; set; } = string.Empty;

        public AllocationStatus Status { get; set; } = AllocationStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Patient Patient { get; set; } = null!;
        public virtual Ward Ward { get; set; } = null!;
    }

    public enum WardType
    {
        General = 0,
        ICU = 1,
        Surgery = 2,
        Recovery = 3,
        Emergency = 4
    }

    public enum AllocationStatus
    {
        Active = 0,
        Discharged = 1,
        Transferred = 2
    }
}