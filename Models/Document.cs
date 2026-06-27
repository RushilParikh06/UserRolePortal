using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserRolePortal.Models
{
    /// <summary>
    /// StatusId values:
    ///   1 = Pending  (default, awaiting admin review)
    ///   2 = Verified (approved by admin)
    ///   3 = Rejected (rejected by admin with mandatory reason)
    /// </summary>
    public class Document
    {
        [Key]
        public int DocumentId { get; set; }

        [Required]
        [StringLength(255)]
        [Display(Name = "File Name")]
        public required string FileName { get; set; }

        [Required]
        [StringLength(500)]
        public required string FilePath { get; set; }

        [Required]
        public DateTime UploadedDate { get; set; }

        // Foreign Key to the User who uploaded the document
        [Required]
        public int UserId { get; set; }

        // Navigation property
        [ForeignKey("UserId")]
        public virtual User? UploadedBy { get; set; }

        // Document type (e.g. Aadhar Card, PAN Card, Other)
        [Required]
        public required string DocumentType { get; set; }

        // State: 1=Pending, 2=Verified, 3=Rejected
        public int StatusId { get; set; } = 1;

        // Populated only when StatusId = 3 (Rejected)
        [StringLength(1000)]
        public string? RejectionReason { get; set; }

        // Kept for backward compatibility with deletion check logic
        [NotMapped]
        public bool IsVerified => StatusId == 2;
    }
}
