using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EyeCareManagement.Models
{
    public class OpticalOrder
    {
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required]
        public string DoctorId { get; set; } = string.Empty;

        public string? OpticianId { get; set; }

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public DateTime? CompletionDate { get; set; }

        [Required]
        public OpticalOrderType Type { get; set; }

        [Required]
        public OpticalOrderStatus Status { get; set; } = OpticalOrderStatus.Pending;

        // Prescription details
        [StringLength(20)]
        public string RightEyeSphere { get; set; } = string.Empty;

        [StringLength(20)]
        public string RightEyeCylinder { get; set; } = string.Empty;

        [StringLength(20)]
        public string RightEyeAxis { get; set; } = string.Empty;

        [StringLength(20)]
        public string LeftEyeSphere { get; set; } = string.Empty;

        [StringLength(20)]
        public string LeftEyeCylinder { get; set; } = string.Empty;

        [StringLength(20)]
        public string LeftEyeAxis { get; set; } = string.Empty;

        [StringLength(20)]
        public string PupillaryDistance { get; set; } = string.Empty;

        // Frame and lens details
        [StringLength(200)]
        public string FrameDetails { get; set; } = string.Empty;

        [StringLength(200)]
        public string LensType { get; set; } = string.Empty;

        [StringLength(200)]
        public string LensCoating { get; set; } = string.Empty;

        [StringLength(1000)]
        public string SpecialInstructions { get; set; } = string.Empty;

        [Column(TypeName = "decimal(8,2)")]
        public decimal EstimatedCost { get; set; }

        [Column(TypeName = "decimal(8,2)")]
        public decimal? FinalCost { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("PatientId")]
        public virtual Patient Patient { get; set; } = null!;

        [ForeignKey("DoctorId")]
        public virtual ApplicationUser Doctor { get; set; } = null!;

        [ForeignKey("OpticianId")]
        public virtual ApplicationUser? Optician { get; set; }
    }

    public enum OpticalOrderType
    {
        Eyeglasses = 0,
        ContactLenses = 1,
        SunGlasses = 2,
        ReadingGlasses = 3,
        SafetyGlasses = 4
    }

    public enum OpticalOrderStatus
    {
        Pending = 0,
        InProgress = 1,
        Completed = 2,
        Delivered = 3,
        Cancelled = 4
    }
}