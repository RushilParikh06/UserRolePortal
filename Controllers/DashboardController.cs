using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using System.Security.Claims;
using UserRolePortal.Models;
using System.IO;
using UserRolePortal.Data;
using Dapper;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace UserPortal.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly DbConnectionFactory _dbFactory;

        // We removed ApponDbContext since we are using Dapper for all logic!
        public DashboardController(IWebHostEnvironment webHostEnvironment, DbConnectionFactory dbFactory)
        {
            _webHostEnvironment = webHostEnvironment;
            _dbFactory = dbFactory;
        }

        // ============== DASHBOARD ==============

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            string fullName = User.FindFirst("FullName")?.Value ?? "User";
            ViewBag.FullName = fullName;

            if (User.IsInRole("Admin"))
            {
                using (var connection = _dbFactory.CreateConnection())
                {
                    // Get counts using Dapper
                    string countSql = @"
                        SELECT 
                            COUNT(1) AS TotalUsers,
                            SUM(CASE WHEN ""RoleId"" = 1 THEN 1 ELSE 0 END) AS AdminCount,
                            SUM(CASE WHEN ""RoleId"" = 2 THEN 1 ELSE 0 END) AS UserCount
                        FROM ""Users""";
                    
                    var stats = await connection.QuerySingleAsync(countSql);
                    ViewBag.TotalUsers = stats.totalusers;
                    ViewBag.AdminCount = stats.admincount;
                    ViewBag.UserCount = stats.usercount;

                    // Get recent users
                    string recentSql = @"SELECT * FROM ""Users"" ORDER BY ""CreatedDate"" DESC LIMIT 5";
                    var recentUsers = (await connection.QueryAsync<User>(recentSql)).ToList();
                    ViewBag.RecentUsers = recentUsers;
                }
            }

            return View();
        }

        // ============== USER LIST (ADMIN ONLY) ==============

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UserList()
        {
            using (var connection = _dbFactory.CreateConnection())
            {
                string sql = @"
                    SELECT u.*, r.* 
                    FROM ""Users"" u
                    LEFT JOIN ""Roles"" r ON u.""RoleId"" = r.""RoleId""
                    ORDER BY u.""CreatedDate"" DESC";

                // Dapper Multi-mapping to include the Role object
                var users = await connection.QueryAsync<User, Role, User>(
                    sql,
                    (user, role) =>
                    {
                        user.Role = role;
                        return user;
                    },
                    splitOn: "RoleId"
                );

                return View(users.ToList());
            }
        }

        // ============== UPDATE USER (ADMIN ONLY) ==============

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUser([FromBody] UserUpdatedDto dto)
        {
            try
            {
                using (var connection = _dbFactory.CreateConnection())
                {
                    string sql = @"
                        UPDATE ""Users"" 
                        SET ""FullName"" = @FullName, 
                            ""Email"" = @Email, 
                            ""MobileNo"" = @MobileNo, 
                            ""RoleId"" = @RoleId 
                        WHERE ""UserId"" = @UserId";

                    int rowsAffected = await connection.ExecuteAsync(sql, dto);
                    
                    if (rowsAffected == 0)
                        return Json(new { success = false, message = "User not found." });

                    return Json(new { success = true, message = "User updated successfully." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating user: " + ex.Message });
            }
        }

        // ============== DELETE USER (ADMIN ONLY) ==============

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                using (var connection = _dbFactory.CreateConnection())
                {
                    string sql = @"DELETE FROM ""Users"" WHERE ""UserId"" = @Id";
                    int rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });

                    if (rowsAffected == 0)
                        return Json(new { success = false, message = "User not found." });

                    return Json(new { success = true, message = "User deleted successfully." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting user: " + ex.Message });
            }
        }

        // ============== UPLOAD DOCUMENTS ==============

        [HttpGet]
        public async Task<IActionResult> UploadDocument()
        {
            using (var connection = _dbFactory.CreateConnection())
            {
                int currentUserId = int.Parse(User.FindFirst("UserId")!.Value);
                bool isAdmin = User.IsInRole("Admin");

                string sql = @"
                    SELECT d.*, u.* 
                    FROM ""Documents"" d
                    LEFT JOIN ""Users"" u ON d.""UserId"" = u.""UserId"" ";

                if (!isAdmin)
                {
                    sql += @" WHERE d.""UserId"" = @CurrentUserId ";
                }

                sql += @" ORDER BY d.""UploadedDate"" DESC";

                var documents = await connection.QueryAsync<Document, User, Document>(
                    sql,
                    (doc, user) =>
                    {
                        doc.UploadedBy = user;
                        return doc;
                    },
                    param: new { CurrentUserId = currentUserId },
                    splitOn: "UserId"
                );

                return View(documents.ToList());
            }
        }

        [HttpPost]
        public async Task<IActionResult> UploadDocument(IFormFile aadharFile, IFormFile panFile, List<IFormFile> optionalFiles)
        {
            // Validate Compulsory Files
            if (aadharFile == null || aadharFile.Length == 0 || panFile == null || panFile.Length == 0)
            {
                TempData["Error"] = "Aadhar Card and PAN Card are strictly required.";
                return RedirectToAction("UploadDocument");
            }

            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            int userId = int.Parse(User.FindFirst("UserId")!.Value);

            using (var connection = _dbFactory.CreateConnection())
            {
                string sql = @"
                    INSERT INTO ""Documents"" (""FileName"", ""FilePath"", ""UploadedDate"", ""UserId"", ""IsVerified"", ""DocumentType"")
                    VALUES (@FileName, @FilePath, @UploadedDate, @UserId, false, @DocumentType)";

                // Local helper function to save to disk and DB
                async Task SaveFileAndDb(IFormFile file, string documentType)
                {
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }

                    await connection.ExecuteAsync(sql, new
                    {
                        FileName = file.FileName,
                        FilePath = "/uploads/" + uniqueFileName,
                        UploadedDate = DateTime.UtcNow,
                        UserId = userId,
                        DocumentType = documentType
                    });
                }

                // 1. Save Aadhar
                await SaveFileAndDb(aadharFile, "Aadhar Card");
                
                // 2. Save PAN
                await SaveFileAndDb(panFile, "PAN Card");

                // 3. Save Optional Files
                if (optionalFiles != null && optionalFiles.Any())
                {
                    foreach (var optFile in optionalFiles)
                    {
                        if (optFile.Length > 0)
                        {
                            await SaveFileAndDb(optFile, "Other");
                        }
                    }
                }
            }

            TempData["Success"] = "All documents uploaded successfully!";
            return RedirectToAction("UploadDocument");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            try
            {
                using (var connection = _dbFactory.CreateConnection())
                {
                    // First get the file path, IsVerified, and UserId so we can enforce rules
                    string selectSql = @"SELECT ""FilePath"", ""IsVerified"", ""UserId"" FROM ""Documents"" WHERE ""DocumentId"" = @Id";
                    var docData = await connection.QueryFirstOrDefaultAsync<dynamic>(selectSql, new { Id = id });

                    if (docData == null || string.IsNullOrEmpty((string)docData.FilePath))
                    {
                        return Json(new { success = false, message = "Document not found." });
                    }

                    // Enforce Security Rule: Non-admins cannot delete verified docs or other users' docs
                    bool isAdmin = User.IsInRole("Admin");
                    int currentUserId = int.Parse(User.FindFirst("UserId")!.Value);

                    if (!isAdmin)
                    {
                        if ((bool)docData.IsVerified)
                        {
                            return Json(new { success = false, message = "You cannot delete a document that has already been verified." });
                        }
                        if ((int)docData.UserId != currentUserId)
                        {
                            return Json(new { success = false, message = "You can only delete your own documents." });
                        }
                    }

                    // Delete the physical file
                    string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, ((string)docData.FilePath).TrimStart('/'));
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }

                    // Delete from DB
                    string deleteSql = @"DELETE FROM ""Documents"" WHERE ""DocumentId"" = @Id";
                    await connection.ExecuteAsync(deleteSql, new { Id = id });

                    return Json(new { success = true, message = "Document deleted successfully." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting document: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> VerifyDocument(int id)
        {
            try
            {
                using (var connection = _dbFactory.CreateConnection())
                {
                    string sql = @"UPDATE ""Documents"" SET ""IsVerified"" = true WHERE ""DocumentId"" = @Id";
                    int rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });

                    if (rowsAffected == 0)
                        return Json(new { success = false, message = "Document not found." });

                    return Json(new { success = true, message = "Document verified successfully." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error verifying document: " + ex.Message });
            }
        }
    }
}