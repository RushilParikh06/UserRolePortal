using System.ComponentModel.DataAnnotations;
using UserRolePortal.Models;

namespace UserRolePortal.Models
{
    public class User
    {
        public int UserId { get; set; }


        // FullName - Required, max 100 characters
        [Required(ErrorMessage = "Full Name is required")]
        [StringLength(100, MinimumLength = 3,
            ErrorMessage = "Full Name must be between 3 and 100 characters")]
        [Display(Name = "Full Name")]
        public required string FullName { get; set; }


        // Username - Required, max 50 characters, must be unique
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, MinimumLength = 3,
            ErrorMessage = "Username must be between 3 and 50 characters")]
        [Display(Name = "Username")]
        public required string Username { get; set; }


        // Password - Required, min 6 characters
        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6,
            ErrorMessage = "Password must be at least 6 characters")]
        [Display(Name = "Password")]
        public required string Password { get; set; }


        // Email - Required, valid email format
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(50)]
        [Display(Name = "Email")]
        public required string Email { get; set; }


        // MobileNo - Required, exactly 10 digits
        // MobileNo - Required, exactly 10 digits
        [Required(ErrorMessage = "Mobile number is required")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "Mobile number must be exactly 10 digits")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Mobile number must contain exactly 10 digits with no prefixes")]
        [Display(Name = "Mobile Number")]
        public required string MobileNo { get; set; }


        // DOB - Required, must be valid date
        [Required(ErrorMessage = "Date of Birth is required")]
        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime DOB { get; set; }


        // RoleId - Required, must select a role
        [Required(ErrorMessage = "Role is required")]
        [Display(Name = "Role")]
        public int RoleId { get; set; }

        // CreatedDate - Auto set by application
        [Display(Name = "Created Date")]
        public DateTime? CreatedDate { get; set; }

        // Gender - Required
        [Required(ErrorMessage = "Gender is required")]
        [Display(Name = "Gender")]
        public required string Gender { get; set; }

        // Status - 1: Verified, 2: Pending (Default), 3: Suspended
        [Display(Name = "Status")]
        public int Status { get; set; } = 2;

        // StatusReason - Reason for suspension or unsuspension
        [Display(Name = "Status Reason")]
        public string? StatusReason { get; set; }

        // Navigation property - relationship to Role table
        public virtual Role? Role { get; set; }
    }
}