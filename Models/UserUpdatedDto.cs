using System.ComponentModel.DataAnnotations;

namespace UserRolePortal.Models
{
    public class UserUpdatedDto
    {
        public int UserId { get; set; }

        [Required]
        public string FullName { get; set; } = string.Empty; // <-- Added = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty; // <-- Added = string.Empty;

        [Required]
        public string MobileNo { get; set; } = string.Empty; // <-- Added = string.Empty;

        [Required]
        public DateTime? DOB { get; set; }

        [Required]
        public int RoleId { get; set; }
    }
}