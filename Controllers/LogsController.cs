using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace UserRolePortal.Controllers
{
    // The user requested that the option to view logs is shown whenever an error occurs.
    // We allow authenticated users to view logs for testing/debugging as per requirements.
    [Authorize]
    public class LogsController : Controller
    {
        public IActionResult Viewer()
        {
            string logsDir = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "Logs");
            string logContent = "No logs found.";

            if (Directory.Exists(logsDir))
            {
                var latestLogFile = Path.Combine(logsDir, $"app-log-{System.DateTime.Now:yyyy-MM-dd}.txt");
                
                if (System.IO.File.Exists(latestLogFile))
                {
                    // Open with FileShare.ReadWrite so we don't lock out the logger
                    using (var fs = new FileStream(latestLogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs))
                    {
                        logContent = sr.ReadToEnd();
                    }
                }
            }

            ViewBag.LogContent = logContent;
            return View();
        }
    }
}
