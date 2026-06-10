using System.ComponentModel.DataAnnotations;

namespace UserRolePortal.Models
{
    public class LoginViewModel
    {
        [Required]
        public string Username { get; set; } = string.Empty; // <-- Added = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty; // <-- Added = string.Empty;
    }
}