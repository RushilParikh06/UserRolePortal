using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using UserRolePortal.Models;
using UserRolePortal.Data;
using Dapper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UserRolePortal.Controllers
{
    // This controller handles user registration, login, and logout
    public class AccountController : Controller
    {
        private readonly DbConnectionFactory _dbFactory;

        
        public AccountController(DbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        // ================= REGISTRATION =================

        [HttpGet]
        public IActionResult Register()
        {
            return View(); 
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User user, string confirmPassword)
        {
            var expectedCaptcha = HttpContext.Session.GetString("CaptchaCode");
            var userCaptcha = Request.Form["CaptchaInput"];

            if (string.IsNullOrEmpty(userCaptcha) || !string.Equals(userCaptcha, expectedCaptcha, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("", "Incorrect CAPTCHA. Please try again.");
                return View(user);
            }

            HttpContext.Session.Remove("CaptchaCode"); 

            if (!ModelState.IsValid)
            {
                return View(user);
            }

            if (user.Password != confirmPassword)
            {
                ModelState.AddModelError("Password", "Passwords do not match");
                return View(user);
            }

            if (user.DOB.Date > DateTime.Today)
            {
                ModelState.AddModelError("DOB", "Date of Birth cannot be in the future");
                return View(user);
            }

            if (user.Password.Length < 6)
            {
                ModelState.AddModelError("Password", "Password must be at least 6 characters");
                return View(user);
            }

            using (var connection = _dbFactory.CreateConnection())
            {
                // Check if username already exists using Dapper
                int userExists = await connection.ExecuteScalarAsync<int>(
                    "SELECT sp_check_username_exists(@p_username)",
                    new { p_username = user.Username });

                if (userExists > 0)
                {
                    ModelState.AddModelError("Username", "Username already exists");
                    return View(user);
                }

                // Check if email already exists using Dapper
                int emailExists = await connection.ExecuteScalarAsync<int>(
                    "SELECT sp_check_email_exists(@p_email)",
                    new { p_email = user.Email });

                if (emailExists > 0)
                {
                    ModelState.AddModelError("Email", "Email already registered");
                    return View(user);
                }

                // Hash password
                var passwordHasher = new PasswordHasher<User>();
                user.Password = passwordHasher.HashPassword(user, user.Password);

                user.CreatedDate = DateTime.UtcNow;
                user.DOB = user.DOB.ToUniversalTime(); // PostgreSQL requires all DateTime values to be in UTC

                // Insert into database using Dapper
                await connection.ExecuteAsync(
                    "SELECT sp_insert_user(@p_fullname, @p_username, @p_password, @p_email, @p_mobileno, @p_dob, @p_roleid, @p_gender, @p_createddate, @p_status, @p_statusreason)",
                    new {
                        p_fullname = user.FullName,
                        p_username = user.Username,
                        p_password = user.Password,
                        p_email = user.Email,
                        p_mobileno = user.MobileNo,
                        p_dob = user.DOB,
                        p_roleid = user.RoleId,
                        p_gender = user.Gender,
                        p_createddate = user.CreatedDate,
                        p_status = user.RoleId == 3 ? 1 : 2, // Super Admin is verified immediately, others pending
                        p_statusreason = (string?)null
                    });
            }

            TempData["Success"] = "Registration successful! Proceed to login.";
            return RedirectToAction("Login");
        }

        // ================= CAPTCHA =================
        [HttpGet]
        public IActionResult GetCaptchaImage()
        {
            var captchaCode = CaptchaService.GenerateRandomString(5);
            HttpContext.Session.SetString("CaptchaCode", captchaCode);

            if (!OperatingSystem.IsWindows())
            {
                return StatusCode(501, "Captcha generation is only supported on Windows on this server.");
            }

            var result = CaptchaService.GenerateCaptchaImage(captchaCode);
            return File(result, "image/png");
        }

        // ================= LOGIN =================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            var expectedCaptcha = HttpContext.Session.GetString("CaptchaCode");
            var userCaptcha = Request.Form["CaptchaInput"];

            if (string.IsNullOrEmpty(userCaptcha) || !string.Equals(userCaptcha, expectedCaptcha))
            {
                ModelState.AddModelError("", "Incorrect CAPTCHA. Please try again.");
                return View(); 
            }
            
            HttpContext.Session.Remove("CaptchaCode");

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Username and password are required");
                return View();
            }

            User? user;
            using (var connection = _dbFactory.CreateConnection())
            {
                user = await connection.QueryFirstOrDefaultAsync<User>(
                    "SELECT * FROM sp_get_user_by_username(@p_username)",
                    new { p_username = username });
            }

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid username or password");
                return View();
            }

            var passwordHasher = new PasswordHasher<User>();
            var result = passwordHasher.VerifyHashedPassword(user, user.Password, password);

            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("", "Invalid username or password");
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("FullName", user.FullName),
                new Claim(ClaimTypes.Role, user.RoleId == 3 ? "SuperAdmin" : (user.RoleId == 1 ? "Admin" : "User")),
                new Claim("UserId", user.UserId.ToString()),
                new Claim("Status", user.Status.ToString()),
                new Claim("StatusReason", user.StatusReason ?? "")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            return RedirectToAction("Index", "Dashboard");
        }

        // ================= LOGOUT =================

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}