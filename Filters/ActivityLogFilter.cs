using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using UserRolePortal.Services;

namespace UserRolePortal.Filters
{
    public class ActivityLogFilter : IAsyncActionFilter, IAsyncExceptionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User.Identity?.IsAuthenticated == true
                ? context.HttpContext.User.Identity.Name
                : "Anonymous";
            var actionName = context.ActionDescriptor.DisplayName;

            AppLogger.LogActivity($"User '{user}' initiated '{actionName}' (Method: {context.HttpContext.Request.Method})");

            var resultContext = await next();

            if (resultContext.Exception == null)
            {
                AppLogger.LogActivity($"User '{user}' completed '{actionName}'");
            }
        }

        public Task OnExceptionAsync(ExceptionContext context)
        {
            var user = context.HttpContext.User.Identity?.IsAuthenticated == true
                ? context.HttpContext.User.Identity.Name
                : "Anonymous";
            
            AppLogger.LogError($"Unhandled exception in '{context.ActionDescriptor.DisplayName}' by user '{user}'", context.Exception);

            // Do not handle the exception here. Let the global error handler process it.
            // We just want to log it.
            return Task.CompletedTask;
        }
    }
}
