﻿using Hr.Application.Common;
using Hr.Application.Common.Global;
using Hr.Application.DTOs.Role;
using Hr.Application.Services.implementation;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security;
using System.Security.Claims;

namespace Hr.System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleManager : ControllerBase
    {
        private readonly IRoleService _roleService;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly UserManager<ApplicationUser> userManager;

        public RoleManager(IRoleService roleService, RoleManager<IdentityRole> roleManager,
             UserManager<ApplicationUser> userManager)
        {
            _roleService = roleService;
            this.roleManager = roleManager;
            this.userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRoles()
        {
            try
            {
                var roles = await _roleService.GetAllRolesAsync();
                var rolesDto = roles.Select(x => new RoleDto
                {
                    Id = x.Id,
                    Name = x.Name,
                }).ToList();
                return Ok(rolesDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", message = ex.Message });
            }
        }

        [HttpGet("GetRole")]
        public async Task<IActionResult> GetRole(string roleId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(roleId))
                {
                    return BadRequest("Invalid roleId");
                }

                var role = await roleManager.FindByIdAsync(roleId);

                if (role == null)
                {
                    return NotFound("Role not found");
                }

                var roleClaims = await roleManager.GetClaimsAsync(role);
                var allPermissions = Permission.GenerateAllPermissions();
                var roleDto = new PermissionFormDto
                {
                    RoleId = roleId,
                    RoleName = role.Name,
                    RoleClaims = allPermissions.Select(permission => new RolePermissionCheckDto
                    {
                        IsSeleced = roleClaims.Any(roleClaim => roleClaim.Value == permission),
                        DisplayValue = permission
                    }).ToList()
                };

                return Ok(roleDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", message = ex.Message });
            }
        }


        [HttpGet("Create")]
        public IActionResult Create()
        {

            try
            {
                var RoleDto = new PermissionFormDto
                {
                    RoleId = "",
                    RoleName = "",
                    RoleClaims = Permission.GenerateAllPermissions().Select(c => new RolePermissionCheckDto
                    {
                        DisplayValue = c
                    }).ToList()
                };
                return Ok(RoleDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", message = ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> CreateRole(PermissionFormDto model)
        {
            try
            {
                int counter = 0;
                foreach (var claim in model.RoleClaims)
                {
                    if (!claim.IsSeleced)
                    {
                        counter++;
                    }

                } 
                if (counter == 24 || (counter == 1 && model.RoleClaims.Any(x => x.DisplayValue == null)))
                {
                    ModelState.AddModelError("RoleClaims", "Please Select the Permissions");
                    return BadRequest(ModelState);
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(model);
                }

                if (roleManager.RoleExistsAsync(model.RoleName).Result)
                {
                    ModelState.AddModelError("RoleName", "This Group Exists!!");
                    return BadRequest(ModelState) ;
                }

                await roleManager.CreateAsync(new IdentityRole { Name = model.RoleName.Trim() });
                var createRole = roleManager.FindByNameAsync(model.RoleName).Result;
                var selectedClaims = model.RoleClaims.Where(x => x.IsSeleced).Select(x => x.DisplayValue).ToList();

                foreach (var claimValue in selectedClaims)
                {
                    await roleManager.AddClaimAsync(createRole, new Claim(SD.PermissionType, claimValue));
                }

                return Ok(model);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", message = ex.Message });
            }
        }



        [HttpPut("{roleId}")]
        public async Task<IActionResult> UpdateRole(string roleId,PermissionFormDto model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var role = await roleManager.FindByIdAsync(roleId);
                    if (role == null)
                    {
                        return NotFound();
                    }
                    int counter = 0;
                    foreach (var claim in model.RoleClaims)
                    {
                        if (!claim.IsSeleced)
                        {
                            counter++;
                        }

                    }

                    if (_roleService.GetAllRolesAsync().Result.Any(R=>R.Name == model.RoleName && R.Id != roleId))
                    {
                        ModelState.AddModelError("RoleName", "This Group Exists!!");
                        return BadRequest(ModelState);
                    }

                    if (counter == 24)
                    {
                        ModelState.AddModelError("RoleClaims", "Please Select the Permissions");
                        return BadRequest(ModelState);
                    }
                    
                    var roleClaim = await roleManager.GetClaimsAsync(role);
                    foreach (var claim in roleClaim)
                    {
                        await roleManager.RemoveClaimAsync(role, claim);
                    }
                    var selectedClaims = model.RoleClaims.Where(x => x.IsSeleced).Select(x => x.DisplayValue).ToList();
                    foreach (var claimValue in selectedClaims)
                    {
                        await roleManager.AddClaimAsync(role, new Claim(SD.PermissionType, claimValue));
                    }
                    role.Name = model.RoleName;
                    await roleManager.UpdateAsync(role);

                    var usersWithUpdatedRole = await userManager.GetUsersInRoleAsync(role.Name);
                    foreach (var user in usersWithUpdatedRole)
                    {
                        // Increment the token version
                        await userManager.UpdateSecurityStampAsync(user);
                    }

                    return Ok(model);

                }
                else
                {
                    return BadRequest(model);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", message = ex.Message });
            }
        }


        [HttpDelete("{roleId}")]
        public async Task<IActionResult> DeleteRole(string roleId)
        {
            try
            {
                var role = await roleManager.FindByIdAsync(roleId);
                if (role != null && role.Name == SD.Roles.SuperAdmin.ToString())
                {
                   
                    ModelState.AddModelError("Role", "Can't Delete Super Admin Role ");
                    return BadRequest(ModelState);
                }
                await _roleService.DeleteRoleAsync(roleId);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", message = ex.Message });
            }
        }

    }
}
