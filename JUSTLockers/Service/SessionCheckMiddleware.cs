using Microsoft.AspNetCore.Authentication;

namespace JUSTLockers.Service
{
    public class SessionCheckMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionCheckMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // Skip check for login page and static files
            if (context.Request.Path.StartsWithSegments("/Account/Login") ||
                context.Request.Path.StartsWithSegments("/css") ||
                context.Request.Path.StartsWithSegments("/js") ||
                context.Request.Path.StartsWithSegments("/lib"))
            {
                await _next(context);
                return;
            }

            // Check if user is authenticated
            if (!context.User.Identity.IsAuthenticated)
            {
                context.Response.Redirect("/Account/Login");
                return;
            }

            // Check if session exists (if you're using session)
            if (context.Session.GetInt32("UserId") == null)
            {
                await context.SignOutAsync();
                context.Response.Redirect("/Account/Login");
                return;
            }

            await _next(context);
        }
    }
}
