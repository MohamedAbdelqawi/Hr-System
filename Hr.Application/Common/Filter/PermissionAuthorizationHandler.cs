using Hr.Application.Common.Filter;
using Hr.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;


public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirment>
{
    private readonly IConfiguration configuration;
    private readonly UserManager<ApplicationUser> userManager;
     
      private readonly IHttpContextAccessor httpContextAccessor;
      private readonly SignInManager<ApplicationUser> signInManager;


    public PermissionAuthorizationHandler(IConfiguration configuration,
          UserManager<ApplicationUser> userManager,
            IHttpContextAccessor httpContextAccessor,
             SignInManager<ApplicationUser> signInManager
            )
    {
        this.configuration = configuration;
        this.userManager = userManager;
        this.httpContextAccessor = httpContextAccessor;
        this.signInManager = signInManager;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirment requirement)
    {
        if (context.User == null)      
            return;

        var incomingTokenVersion = context.User?.Claims.FirstOrDefault(c => c.Type == "token_version")?.Value;

        var userId = context.User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            ForceLogout();
            context.Fail(); // Return unauthorized
            return;
        }

        var user = await userManager.FindByIdAsync(userId);

        if (user == null)
        {
            ForceLogout();
            context.Fail(); // Return unauthorized
            return;
        }

        try
        {
            // Retrieve the current token version from the user or database
            var currentUserTokenVersion = await userManager.GetSecurityStampAsync(user);

            if (incomingTokenVersion != currentUserTokenVersion)
            {
                //ForceLogout();
                context.Fail(); // Return unauthorized
                return;
            }
        }
        catch (Exception ex)
        {
            ForceLogout();
            context.Fail(); // Return unauthorized
            return;
        }
         
        string validIssuer = configuration["JWT:ValidIssuer"];
        var canAccess = context.User.Claims.Any(c => c.Type == "Permission" && c.Value == requirement.Permission && c.Issuer == validIssuer);

        if (canAccess)
        {
            context.Succeed(requirement);
            return;
        }

        

    }
    private void ForceLogout()
    {
        var httpContext = httpContextAccessor.HttpContext;

        // Sign out the user
        signInManager.SignOutAsync();

        // Redirect to the login page
        httpContext.Response.Redirect("/Account/Login");

    }
}
