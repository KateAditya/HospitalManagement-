using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EyeCareManagement.Models
{
    public class MedicalRecord
    {
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required]
        public string DoctorId { get; set; } = string.Empty;

        [Required]
        public DateTime VisitDate { get; set; }

        [Required]
        [StringLength(1000)]
        public string Diagnosis { get; set; } = string.Empty;

        [StringLength(2000)]
        public string Treatment { get; set; } = string.Empty;

        [StringLength(500)]
        public string Prescription { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Notes { get; set; } = string.Empty;

        [StringLength(100)]
        public string VisualAcuityLeft { get; set; } = string.Empty;

        [StringLength(100)]
        public string VisualAcuityRight { get; set; } = string.Empty;

        [Column(TypeName = "decimal(5,2)")]
        public decimal? IntraocularPressureLeft { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? IntraocularPressureRight { get; set; }

        public DateTime? NextVisitDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("PatientId")]
        public virtual Patient Patient { get; set; } = null!;

        [ForeignKey("DoctorId")]
        public virtual ApplicationUser Doctor { get; set; } = null!;
    }
}