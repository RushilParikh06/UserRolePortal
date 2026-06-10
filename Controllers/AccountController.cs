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

        // EF Core context is removed since we use Dapper!
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
                string userCheckSql = @"SELECT COUNT(1) FROM ""Users"" WHERE ""Username"" = @Username";
                int userExists = await connection.ExecuteScalarAsync<int>(userCheckSql, new { Username = user.Username });

                if (userExists > 0)
                {
                    ModelState.AddModelError("Username", "Username already exists");
                    return View(user);
                }

                // Check if email already exists using Dapper
                string emailCheckSql = @"SELECT COUNT(1) FROM ""Users"" WHERE ""Email"" = @Email";
                int emailExists = await connection.ExecuteScalarAsync<int>(emailCheckSql, new { Email = user.Email });

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
                string insertSql = @"
                    INSERT INTO ""Users"" (""FullName"", ""Username"", ""Password"", ""Email"", ""MobileNo"", ""DOB"", ""RoleId"", ""Gender"", ""CreatedDate"")
                    VALUES (@FullName, @Username, @Password, @Email, @MobileNo, @DOB, @RoleId, @Gender, @CreatedDate)";

                await connection.ExecuteAsync(insertSql, user);
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

            User user;
            using (var connection = _dbFactory.CreateConnection())
            {
                string sql = @"
                    SELECT * 
                    FROM ""Users"" 
                    WHERE ""Username"" = @Username";
                    
                user = await connection.QueryFirstOrDefaultAsync<User>(sql, new { Username = username });
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
                new Claim(ClaimTypes.Role, user.RoleId == 1 ? "Admin" : "User"),
                new Claim("UserId", user.UserId.ToString())
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