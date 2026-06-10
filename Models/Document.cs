using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserRolePortal.Models
{
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

        // Document type
        [Required]
        public required string DocumentType { get; set; }

        public bool IsVerified { get; set; } = false;
    }
}
