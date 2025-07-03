using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EyeCareManagement.Models
{
    public class Bill
    {
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required]
        public string GeneratedById { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string BillNumber { get; set; } = string.Empty;

        [Required]
        public DateTime BillDate { get; set; } = DateTime.UtcNow;

        [Required]
        public BillType Type { get; set; }

        [Required]
        public BillStatus Status { get; set; } = BillStatus.Pending;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal ConsultationCharges { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal ProcedureCharges { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal OpticalCharges { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal WardCharges { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal MedicineCharges { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal OtherCharges { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Discount { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal PaidAmount { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal BalanceAmount { get; set; }

        public PaymentMethod? PaymentMethod { get; set; }

        [StringLength(100)]
        public string PaymentReference { get; set; } = string.Empty;

        public DateTime? PaymentDate { get; set; }

        [StringLength(1000)]
        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("PatientId")]
        public virtual Patient Patient { get; set; } = null!;

        [ForeignKey("GeneratedById")]
        public virtual ApplicationUser GeneratedBy { get; set; } = null!;
    }

    public enum BillType
    {
        Consultation = 0,
        Surgery = 1,
        Optical = 2,
        Ward = 3,
        Emergency = 4,
        Comprehensive = 5
    }

    public enum BillStatus
    {
        Pending = 0,
        Partial = 1,
        Paid = 2,
        Cancelled = 3,
        Refunded = 4
    }

    public enum PaymentMethod
    {
        Cash = 0,
        Card = 1,
        BankTransfer = 2,
        Insurance = 3,
        Cheque = 4
    }
}