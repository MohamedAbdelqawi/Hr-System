using Hr.Application.Common;
using Hr.Application.Common.Enums;
using Hr.Application.Common.Global;
using Hr.Application.Interfaces;
using Hr.Domain.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;


namespace Hr.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async void Configure(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.CreateScope())
            {
                var roleManager = serviceScope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = serviceScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                await roleManager.SeedAdminRoleAsync();
                await userManager.SeedAdminUserAsync(roleManager);
                SeedStoredProcedureData(serviceScope.ServiceProvider);
            }
            
            // Other configuration code
        }
        public static async Task SeedAdminRoleAsync(this RoleManager<IdentityRole> roleManager)
        {
            await roleManager.CreateAsync(new IdentityRole(SD.Roles.SuperAdmin.ToString()));
        }

        public static async Task SeedAdminUserAsync(this UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            var adminUser = new ApplicationUser
            {
                UserName = SD.AdminUserName,
                Email = SD.AdminUserName,
                PasswordHash = SD.AdminPasswoed,
                EmailConfirmed = true
            };

            var user = await userManager.FindByEmailAsync(adminUser.Email);
            if (user == null)
            {
                await userManager.CreateAsync(adminUser);
               
                await userManager.AddToRoleAsync(adminUser, SD.Roles.SuperAdmin.ToString());
            }
            await roleManager.SeedClaimsToAdmin(adminUser);
        }

        public static async Task SeedClaimsToAdmin(this RoleManager<IdentityRole> roleManager, ApplicationUser adminUser)
        {
            var adminRole = await roleManager.FindByNameAsync(SD.Roles.SuperAdmin.ToString());
            if (adminRole != null)
            {
                foreach (Modules module in Enum.GetValues(typeof(Modules)))
                {
                    var allPermissions = Permission.GeneratePermissionList(module.ToString());
                    var allClaims = await roleManager.GetClaimsAsync(adminRole);
                    foreach (var permission in allPermissions)
                    {
                        if (!allClaims.Any(c => c.Type == SD.PermissionType && c.Value == permission))
                            await roleManager.AddClaimAsync(adminRole, new Claim(SD.PermissionType, permission));
                    }
                }
            }
        }
        private static void SeedStoredProcedureData(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Use different methods to get the current directory
                var baseDirectoryPath = AppDomain.CurrentDomain.BaseDirectory;
                var currentDirectoryPath = Directory.GetCurrentDirectory();

                Console.WriteLine($"Base Directory: {baseDirectoryPath}");
                Console.WriteLine($"Current Directory: {currentDirectoryPath}");

                // Construct the correct path to the SQL script file
                var scriptPath = Path.Combine(baseDirectoryPath, "SqlScripts", "sp_CalculateEmployeeSalaryReport.sql");

                Console.WriteLine($"Attempting to read file at: {scriptPath}");

                if (!File.Exists(scriptPath))
                {
                    Console.WriteLine($"Error: The file '{scriptPath}' does not exist.");
                    return;
                }

                // Read the stored procedure script from the file
                var script = File.ReadAllText(scriptPath);

                // Execute the stored procedure script
                dbContext.Database.ExecuteSqlRaw(script);
            }
        }



    }



}
