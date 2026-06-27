using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserRolePortal.Models
{
    public class UserStatusHistory
    {
        [Key]
        public int HistoryId { get; set; }

        [Required]
        public int UserId { get; set; }

        public int PreviousStatus { get; set; }

        public int NewStatus { get; set; }

        [MaxLength(1000)]
        public string? Reason { get; set; }

        [Required]
        public DateTime ChangeDate { get; set; }

        // Navigation property
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
