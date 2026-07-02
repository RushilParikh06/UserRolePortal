using Microsoft.AspNetCore.Mvc;
using UserRolePortal.Services;
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

            if (User.IsInRole("Admin") || User.IsInRole("SuperAdmin"))
            {
                using (var connection = _dbFactory.CreateConnection())
                {
                    // Get counts using Dapper
                    var stats = await connection.QuerySingleAsync(
                        "SELECT * FROM sp_get_dashboard_stats()");
                    ViewBag.TotalUsers = stats.totalusers;
                    ViewBag.AdminCount = stats.admincount;
                    ViewBag.UserCount = stats.usercount;

                    // Get recent users
                    var recentUsers = (await connection.QueryAsync<User>(
                        "SELECT * FROM sp_get_recent_users()")).ToList();
                    ViewBag.RecentUsers = recentUsers;
                }
            }

            return View();
        }

        // ============== USER LIST (ADMIN ONLY) ==============

        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> UserList()
        {
            if (User.IsInRole("Admin") && !User.IsInRole("SuperAdmin") && User.FindFirst("Status")?.Value != "1")
            {
                return RedirectToAction("Index");
            }

            using (var connection = _dbFactory.CreateConnection())
            {
                // Dapper Multi-mapping to include the Role object
                var users = await connection.QueryAsync<User, Role, User>(
                    "SELECT * FROM sp_get_all_users_with_roles()",
                    (user, role) =>
                    {
                        user.Role = role;
                        return user;
                    },
                    splitOn: "Role_RoleId"
                );

                if (User.IsInRole("SuperAdmin"))
                {
                    return View(users.Where(u => u.RoleId == 1 || u.RoleId == 2).ToList());
                }
                else
                {
                    return View(users.Where(u => u.RoleId == 2).ToList());
                }
            }
        }

        // ============== UPDATE USER (ADMIN ONLY) ==============

        [HttpPost]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateUser([FromBody] UserUpdatedDto dto)
        {
            if (User.IsInRole("Admin") && !User.IsInRole("SuperAdmin") && User.FindFirst("Status")?.Value != "1")
                return Json(new { success = false, message = "Access denied. You must be a verified admin to perform this action." });

            try
            {
                using (var connection = _dbFactory.CreateConnection())
                {
                    var targetUser = await connection.QueryFirstOrDefaultAsync<User>("SELECT * FROM \"Users\" WHERE \"UserId\" = @uid", new { uid = dto.UserId });
                    if (targetUser != null && (targetUser.RoleId == 1 || targetUser.RoleId == 3) && !User.IsInRole("SuperAdmin"))
                        return Json(new { success = false, message = "Access denied. Only SuperAdmins can modify other Admins." });

                    await connection.ExecuteAsync(
                        "SELECT sp_update_user(@p_userid, @p_fullname, @p_email, @p_mobileno, @p_roleid)",
                        new { p_userid = dto.UserId, p_fullname = dto.FullName, p_email = dto.Email, p_mobileno = dto.MobileNo, p_roleid = dto.RoleId });

                    return Json(new { success = true, message = "User updated successfully." });
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Error updating user", ex);
                return Json(new { success = false, message = "Error updating user: " + ex.Message + "<br/><a href='/Logs/Viewer' class='btn btn-sm btn-outline-danger mt-2'>View Logs & Errors</a>" });
            }
        }

        // ============== DELETE USER (ADMIN ONLY) ==============

        [HttpPost]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (User.IsInRole("Admin") && !User.IsInRole("SuperAdmin") && User.FindFirst("Status")?.Value != "1")
                return Json(new { success = false, message = "Access denied. You must be a verified admin to perform this action." });

            try
            {
                using (var connection = _dbFactory.CreateConnection())
                {
                    var targetUser = await connection.QueryFirstOrDefaultAsync<User>("SELECT * FROM \"Users\" WHERE \"UserId\" = @uid", new { uid = id });
                    if (targetUser != null && (targetUser.RoleId == 1 || targetUser.RoleId == 3) && !User.IsInRole("SuperAdmin"))
                        return Json(new { success = false, message = "Access denied. Only SuperAdmins can delete Admins." });

                    await connection.ExecuteAsync(
                        "SELECT sp_delete_user(@p_userid)",
                        new { p_userid = id });

                    return Json(new { success = true, message = "User deleted successfully." });
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Error deleting user", ex);
                return Json(new { success = false, message = "Error deleting user: " + ex.Message + "<br/><a href='/Logs/Viewer' class='btn btn-sm btn-outline-danger mt-2'>View Logs & Errors</a>" });
            }
        }

        // ============== STATUS ACTIONS (ADMIN ONLY) ==============

        [HttpPost]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateUserStatus(int userId, int newStatus, string reason)
        {
            if (User.IsInRole("Admin") && !User.IsInRole("SuperAdmin") && User.FindFirst("Status")?.Value != "1")
                return Json(new { success = false, message = "Access denied. You must be a verified admin to perform this action." });

            try
            {
                if ((newStatus == 3 || newStatus == 1) && string.IsNullOrWhiteSpace(reason))
                {
                    return Json(new { success = false, message = "A reason is mandatory for this action." });
                }

                using (var connection = _dbFactory.CreateConnection())
                {
                    var targetUser = await connection.QueryFirstOrDefaultAsync<User>("SELECT * FROM \"Users\" WHERE \"UserId\" = @uid", new { uid = userId });
                    if (targetUser != null && (targetUser.RoleId == 1 || targetUser.RoleId == 3) && !User.IsInRole("SuperAdmin"))
                        return Json(new { success = false, message = "Access denied. Only SuperAdmins can modify Admins." });

                    await connection.ExecuteAsync(
                        "SELECT sp_update_user_status(@p_userid, @p_newstatus, @p_reason, @p_changedate)",
                        new { p_userid = userId, p_newstatus = newStatus, p_reason = reason?.Trim(), p_changedate = DateTime.UtcNow });

                    return Json(new { success = true, message = "User status updated successfully." });
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Error updating user status", ex);
                return Json(new { success = false, message = "Error updating user status: " + ex.Message + "<br/><a href='/Logs/Viewer' class='btn btn-sm btn-outline-danger mt-2'>View Logs & Errors</a>" });
            }
        }

        // ============== UPLOAD DOCUMENTS ==============

        [HttpGet]
        public async Task<IActionResult> UploadDocument(int? userId = null)
        {
            // Block suspended users
            if (User.FindFirst("Status")?.Value == "3" && !User.IsInRole("SuperAdmin"))
            {
                return RedirectToAction("Index");
            }

            using (var connection = _dbFactory.CreateConnection())
            {
                int currentUserId = int.Parse(User.FindFirst("UserId")!.Value);
                bool isSuperAdmin = User.IsInRole("SuperAdmin");
                bool isAdmin = User.IsInRole("Admin") && User.FindFirst("Status")?.Value == "1";

                int? targetUserId = (isAdmin || isSuperAdmin) ? userId : currentUserId;
                ViewBag.IsAdminReviewMode = (isAdmin || isSuperAdmin);

                var documents = await connection.QueryAsync<Document, User, Document>(
                    "SELECT * FROM sp_get_documents(@p_userid)",
                    (doc, user) =>
                    {
                        doc.UploadedBy = user;
                        return doc;
                    },
                    param: new { p_userid = targetUserId },
                    splitOn: "User_UserId"
                );

                var documentList = documents.ToList();
                if (isAdmin && !isSuperAdmin)
                {
                    documentList = documentList.Where(d => d.UploadedBy?.RoleId == 2 || d.UserId == currentUserId).ToList();
                }

                return View(documentList);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UploadDocument([FromForm] IFormFile aadharFile, [FromForm] IFormFile panFile, [FromForm] List<IFormFile> optionalFiles)
        {
            // Block suspended users
            if (User.FindFirst("Status")?.Value == "3" && !User.IsInRole("SuperAdmin"))
            {
                return RedirectToAction("Index");
            }

            // Validate Compulsory Files
            if (aadharFile == null || panFile == null)
            {
                TempData["Error"] = "Aadhar Card and PAN Card are strictly required.";
                return RedirectToAction("UploadDocument");
            }

            // Local helper function to validate file constraints
            async Task<(bool IsValid, string ErrorMessage)> ValidateFileAsync(IFormFile file)
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (extension != ".pdf")
                {
                    return (false, $"Only PDF files are accepted. File {file.FileName} is invalid.");
                }

                if (file.Length == 0)
                {
                    return (false, $"File {file.FileName} is empty.");
                }

                long maxFileSize = 2 * 1024 * 1024; // 2MB
                if (file.Length > maxFileSize)
                {
                    return (false, $"File {file.FileName} exceeds the maximum limit of 2MB.");
                }

                var pdfSignature = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }; // %PDF-
                if (file.Length < pdfSignature.Length)
                {
                    return (false, $"File {file.FileName} is invalid or corrupted.");
                }

                using (var stream = file.OpenReadStream())
                {
                    var buffer = new byte[pdfSignature.Length];
                    await stream.ReadExactlyAsync(buffer, 0, buffer.Length);

                    if (!buffer.SequenceEqual(pdfSignature))
                    {
                        return (false, $"File {file.FileName} is not a valid PDF document.");
                    }

                    stream.Position = 0; // Reset stream position for saving later
                }

                return (true, string.Empty);
            }

            // Validate all files before doing any saving
            var filesToValidate = new List<IFormFile> { aadharFile, panFile };
            if (optionalFiles != null)
            {
                filesToValidate.AddRange(optionalFiles.Where(f => f.Length > 0));
            }

            foreach (var fileToValidate in filesToValidate)
            {
                var validationResult = await ValidateFileAsync(fileToValidate);
                if (!validationResult.IsValid)
                {
                    TempData["Error"] = validationResult.ErrorMessage;
                    return RedirectToAction("UploadDocument");
                }
            }

            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            int userId = int.Parse(User.FindFirst("UserId")!.Value);

            using (var connection = _dbFactory.CreateConnection())
            {
                // Local helper function to save to disk and DB
                async Task SaveFileAndDb(IFormFile file, string documentType)
                {
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }

                    await connection.ExecuteAsync(
                        "SELECT sp_insert_document(@p_filename, @p_filepath, @p_uploadeddate, @p_userid, @p_documenttype)",
                        new
                        {
                            p_filename = file.FileName,
                            p_filepath = "/uploads/" + uniqueFileName,
                            p_uploadeddate = DateTime.UtcNow,
                            p_userid = userId,
                            p_documenttype = documentType
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
                    var docData = await connection.QueryFirstOrDefaultAsync<Document>(
                        "SELECT * FROM sp_get_document_for_deletion(@p_documentid)",
                        new { p_documentid = id });

                    if (docData == null || string.IsNullOrEmpty(docData.FilePath))
                    {
                        return Json(new { success = false, message = "Document not found." });
                    }

                    // Enforce Security Rule: Non-admins cannot delete verified docs or other users' docs
                    bool isSuperAdmin = User.IsInRole("SuperAdmin");
                    bool isAdmin = isSuperAdmin || (User.IsInRole("Admin") && User.FindFirst("Status")?.Value == "1");
                    int currentUserId = int.Parse(User.FindFirst("UserId")!.Value);

                    if (!isAdmin)
                    {
                        if (docData.StatusId == 2) // 2 = Verified
                        {
                            return Json(new { success = false, message = "You cannot delete a document that has already been verified." });
                        }
                        if (docData.UserId != currentUserId)
                        {
                            return Json(new { success = false, message = "You can only delete your own documents." });
                        }
                    }
                    else if (!isSuperAdmin)
                    {
                        var targetUser = await connection.QueryFirstOrDefaultAsync<User>("SELECT * FROM \"Users\" WHERE \"UserId\" = @uid", new { uid = docData.UserId });
                        if (targetUser != null && (targetUser.RoleId == 1 || targetUser.RoleId == 3) && targetUser.UserId != currentUserId)
                        {
                            return Json(new { success = false, message = "Access denied. Only SuperAdmins can delete other Admins' documents." });
                        }
                    }

                    // Delete the physical file
                    string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, docData.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }

                    // Delete from DB
                    await connection.ExecuteAsync(
                        "SELECT sp_delete_document(@p_documentid)",
                        new { p_documentid = id });

                    if (isAdmin || isSuperAdmin)
                    {
                        var targetUserToUpdate = await connection.QueryFirstOrDefaultAsync<User>("SELECT * FROM \"Users\" WHERE \"UserId\" = @uid", new { uid = docData.UserId });
                        if (targetUserToUpdate != null && targetUserToUpdate.RoleId != 3)
                        {
                            await connection.ExecuteAsync(
                                "SELECT sp_update_user_status(@p_userid, @p_newstatus, @p_reason, @p_changedate)",
                                new { 
                                    p_userid = docData.UserId, 
                                    p_newstatus = 2, 
                                    p_reason = "A required document was deleted. Please upload your documents again for verification.", 
                                    p_changedate = DateTime.UtcNow 
                                });
                        }
                    }

                    return Json(new { success = true, message = "Document deleted successfully." });
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Error deleting document", ex);
                return Json(new { success = false, message = "Error deleting document: " + ex.Message + "<br/><a href='/Logs/Viewer' class='btn btn-sm btn-outline-danger mt-2'>View Logs & Errors</a>" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> VerifyDocument(int id)
        {
            if (User.IsInRole("Admin") && !User.IsInRole("SuperAdmin") && User.FindFirst("Status")?.Value != "1")
                return Json(new { success = false, message = "Access denied. You must be a verified admin to perform this action." });

            try
            {
                using (var connection = _dbFactory.CreateConnection())
                {
                    var uploaderId = await connection.QuerySingleOrDefaultAsync<int?>(
                        "SELECT \"UserId\" FROM \"Documents\" WHERE \"DocumentId\" = @id",
                        new { id = id });

                    if (uploaderId.HasValue && !User.IsInRole("SuperAdmin"))
                    {
                        var targetUser = await connection.QueryFirstOrDefaultAsync<User>("SELECT * FROM \"Users\" WHERE \"UserId\" = @uid", new { uid = uploaderId.Value });
                        if (targetUser != null && (targetUser.RoleId == 1 || targetUser.RoleId == 3))
                            return Json(new { success = false, message = "Access denied. Only SuperAdmins can verify Admin documents." });
                    }

                    // 1. Verify the specific document
                    await connection.ExecuteAsync(
                        "SELECT sp_verify_document(@p_documentid)",
                        new { p_documentid = id });

                    // 2. Auto-verify the user if all mandatory documents are now verified
                    var uploaderIdCheck = await connection.QuerySingleOrDefaultAsync<int?>(
                        "SELECT \"UserId\" FROM \"Documents\" WHERE \"DocumentId\" = @id",
                        new { id = id });

                    if (uploaderIdCheck.HasValue)
                    {
                        var docs = await connection.QueryAsync<Document>(
                            "SELECT * FROM \"Documents\" WHERE \"UserId\" = @uid",
                            new { uid = uploaderIdCheck.Value });

                        bool hasVerifiedAadhar = docs.Any(d => d.DocumentType == "Aadhar Card" && d.StatusId == 2);
                        bool hasVerifiedPan = docs.Any(d => d.DocumentType == "PAN Card" && d.StatusId == 2);

                        if (hasVerifiedAadhar && hasVerifiedPan)
                        {
                            var userStatus = await connection.QuerySingleOrDefaultAsync<int?>(
                                "SELECT \"Status\" FROM \"Users\" WHERE \"UserId\" = @uid",
                                new { uid = uploaderIdCheck.Value });

                            if (userStatus == 2) // Pending Verify
                            {
                                await connection.ExecuteAsync(
                                    "SELECT sp_update_user_status(@p_userid, @p_newstatus, @p_reason, @p_changedate)",
                                    new { 
                                        p_userid = uploaderIdCheck.Value, 
                                        p_newstatus = 1, 
                                        p_reason = "Documents approved. User successfully verified.", 
                                        p_changedate = DateTime.UtcNow 
                                    });
                            }
                        }
                    }

                    return Json(new { success = true, message = "Document verified successfully." });
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Error verifying document", ex);
                return Json(new { success = false, message = "Error verifying document: " + ex.Message + "<br/><a href='/Logs/Viewer' class='btn btn-sm btn-outline-danger mt-2'>View Logs & Errors</a>" });
            }
        }

        // ============== REJECT DOCUMENT (ADMIN ONLY) ==============

        [HttpPost]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> RejectDocument(int id, string reason)
        {
            if (User.IsInRole("Admin") && !User.IsInRole("SuperAdmin") && User.FindFirst("Status")?.Value != "1")
                return Json(new { success = false, message = "Access denied. You must be a verified admin to perform this action." });

            try
            {
                if (string.IsNullOrWhiteSpace(reason))
                {
                    return Json(new { success = false, message = "A rejection reason is mandatory." });
                }

                using (var connection = _dbFactory.CreateConnection())
                {
                    var uploaderId = await connection.QuerySingleOrDefaultAsync<int?>(
                        "SELECT \"UserId\" FROM \"Documents\" WHERE \"DocumentId\" = @id",
                        new { id = id });

                    if (uploaderId.HasValue && !User.IsInRole("SuperAdmin"))
                    {
                        var targetUser = await connection.QueryFirstOrDefaultAsync<User>("SELECT * FROM \"Users\" WHERE \"UserId\" = @uid", new { uid = uploaderId.Value });
                        if (targetUser != null && (targetUser.RoleId == 1 || targetUser.RoleId == 3))
                            return Json(new { success = false, message = "Access denied. Only SuperAdmins can reject Admin documents." });
                    }

                    await connection.ExecuteAsync(
                        "SELECT sp_reject_document(@p_documentid, @p_reason)",
                        new { p_documentid = id, p_reason = reason.Trim() });

                    return Json(new { success = true, message = "Document rejected successfully." });
                }
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Error rejecting document", ex);
                return Json(new { success = false, message = "Error rejecting document: " + ex.Message + "<br/><a href='/Logs/Viewer' class='btn btn-sm btn-outline-danger mt-2'>View Logs & Errors</a>" });
            }
        }
    }
}