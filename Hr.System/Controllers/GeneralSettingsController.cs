using Hr.Application.Common.Enums;
using Hr.Application.DTOs;
using Hr.Application.DTOs.Employee;
using Hr.Application.Services.implementation;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

using System.Collections.Generic;
using System.Diagnostics.Metrics;




namespace Hr.System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GeneralSettingsController : ControllerBase
    {
        IWeekendService weekendService;
        IGeneralSettingsService generalSettingsService;
        IEmployeeServices employeeServices;
        public GeneralSettingsController(IWeekendService weekendService, IGeneralSettingsService generalSettingsService, IEmployeeServices employeeServices)
        {
            this.weekendService = weekendService;
            this.generalSettingsService = generalSettingsService;
            this.employeeServices = employeeServices;
        }
        #region all emps

        [HttpGet]
        //Display CheckBox for Weekends
        public ActionResult Create()
        {
            try
            {
                var general = generalSettingsService.GetGeneralSettingForAll();

                List<string> weekDays = weekendService.Days();

                var weekendDTO = new WeekendDTO();
                if (general == null)
                {
                    weekendDTO = new WeekendDTO
                    {
                        Weekends = weekDays.Select(day => new WeekendCheckDTO
                        {
                            displayValue = day,
                            isSelected = false,
                        }).ToList()
                    };
                }
                else
                {
                    var weekdaysDb = weekendService.GetAllWeekends().Where(x => x.GeneralSettings.EmployeeId == null);

                    weekendDTO = new WeekendDTO
                    {
                        OvertimeHour = general.OvertimeHour,
                        DiscountHour = general.DiscountHour,
                        Id = general.Id,
                        empid = general.EmployeeId,
                        Weekends = weekDays.Select(day => new WeekendCheckDTO
                        {
                            displayValue = day,
                            isSelected = weekdaysDb.Any(x => x.Name == day)
                        }).ToList()
                    };
                }
                IEnumerable<GetAllEmployeeDto> employeeDTOs = employeeServices.GetAllEmployee();
                IEnumerable<SelectListItem> employeeSelectList = employeeDTOs.Select(dto => new SelectListItem
                {
                    Value = dto.ID.ToString(),
                    Text = $"{dto.FirstName} {dto.LastName}"
                }).ToList();
                weekendDTO.EmployeeList = employeeSelectList;


                return Ok(weekendDTO);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }


        #endregion

        #region custom 

        [HttpGet("{empid}")]
        //when relation between emp & his settings (custom Settings)
        public ActionResult GetById(int empid)
        {
            try
            {
                List<string> weekDays = weekendService.Days();
                var weekdaysDb = weekendService.GetAllWeekends().Where(x => x.GeneralSettings.EmployeeId == empid);

                var Customsettings = generalSettingsService.GetGeneralSettingId(empid);

                if (Customsettings == null)
                {
                    var Deafultweekend = new WeekendDTO
                    {
                        empid = empid,
                        Weekends = weekDays.Select(day => new WeekendCheckDTO
                        {
                            displayValue = day,
                            isSelected = false
                        }).ToList(),
                        EmployeeList = employeeServices.GetAllEmployeeForAttendance().Select(x => new SelectListItem
                        {
                            Value = x.Id.ToString(),
                            Text = x.Name
                        }).ToList()

                    };
                    return Ok(Deafultweekend);

                }
                else
                {
                    var Weekends = weekendService.GetById(Customsettings.Id);

                    var DTO = new WeekendDTO
                    {
                        Id = Customsettings.Id,
                        OvertimeHour = Customsettings.OvertimeHour,
                        DiscountHour = Customsettings.DiscountHour,
                        empid = Customsettings.EmployeeId,
                        Weekends = weekDays.Select(day => new WeekendCheckDTO
                        {
                            displayValue = day,
                            isSelected = weekdaysDb.Any(x => x.Name == day)
                        }).ToList(),
                        EmployeeList = employeeServices.GetAllEmployeeForAttendance().Select(x => new SelectListItem
                        {
                            Value = x.Id.ToString(),
                            Text = x.Name
                        }).ToList()

                    };
                    return Ok(DTO);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public IActionResult UpdateGeneralSettings(WeekendDTO updatedSettings , int id)
        {

            try
            {
                if (ModelState.IsValid)
                {
                    int Counter = 0;
                    foreach (var item in updatedSettings.Weekends)
                    {
                        if (!item.isSelected)
                        {
                            Counter++;
                        }
                    }

                    if (Counter == 7)
                    {
                        ModelState.AddModelError("Days", "Please Select Day ");
                        return BadRequest(ModelState);
                    }

                    if (!ModelState.IsValid)
                    {
                        return BadRequest(new { updatedSettings });
                    }

                    var CustomSettingsExisted = generalSettingsService.CheckGeneralSettingsExists(updatedSettings.empid);

                    if (CustomSettingsExisted == false )
                    {
                       
                            var employeeSetting = generalSettingsService.GetGeneralSettingByID(id);

                            var state = weekendService.Update(updatedSettings, employeeSetting.Id);
                            if (state == false)
                            {
                                return BadRequest(new { error = "Invalid request data." });
                            }
                            else
                            {
                                //var updatedweekends = weekendService.GetAllWeekends().Where(x=>x.GeneralSettingsId== id);
                                //employeeSetting.Weekends =updatedweekends.ToList();
                                employeeSetting.OvertimeHour = updatedSettings.OvertimeHour;
                                employeeSetting.DiscountHour = updatedSettings.DiscountHour;
                                generalSettingsService.Update(employeeSetting);
                                
                                return Ok(updatedSettings);

                            }
                      
                    }
                }
                else
                {
                    return BadRequest(updatedSettings);
                }

                var employeeSettings = generalSettingsService.GetGeneralSettingId(updatedSettings.empid.Value);

                var states = weekendService.Update(updatedSettings, employeeSettings.Id);
                if (states == false)
                {
                    return BadRequest(new { error = "Invalid request data." });
                }
                else
                {
                    //var updatedweekends = weekendService.GetAllWeekends().Where(x => x.GeneralSettingsId == id);
                    //employeeSettings.Weekends = updatedweekends.ToList();
                    employeeSettings.OvertimeHour = updatedSettings.OvertimeHour;
                    employeeSettings.DiscountHour = updatedSettings.DiscountHour;
                    generalSettingsService.Update(employeeSettings);
                    return Ok(updatedSettings);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult CreateSettings(WeekendDTO model)
        {
            try
            {

                if (model.Weekends == null)
                {
                    model.Weekends = new List<WeekendCheckDTO>();

                    // Initialize the list with days of the week and isSelected set to false
                    string[] daysOfWeek = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

                    foreach (var day in daysOfWeek)
                    {
                        model.Weekends.Add(new WeekendCheckDTO
                        {
                            displayValue = day,
                            isSelected = false
                        });
                    }
                }
                int Counter = 0;
                foreach (var item in model.Weekends)
                {
                    if (!item.isSelected)
                    {
                        Counter++;
                    }
                }
                        
                if (Counter == 7)
                {
                    ModelState.AddModelError("Days", "Please Select Day ");
                    return BadRequest(ModelState);
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new { model });
                }

                var generalExists = generalSettingsService.CheckGeneralSettingsExists(model.empid);

                if (generalExists)
                {
                    ModelState.AddModelError("CustomeSettings", "Custom settings Already Exists!");
                    return BadRequest(ModelState);
                }

                if (model.empid == 0)
                {
                    model.empid = null;
                    var existsNull = generalSettingsService.GetGeneralSettingForAll();
                    if (existsNull != null)
                    {
                        ModelState.AddModelError("GeneralSettings", "General Settings Already Exists!");
                        return BadRequest(ModelState);
                    }
                }

                var general = new GeneralSettings
                {
                    OvertimeHour = model.OvertimeHour,
                    DiscountHour = model.DiscountHour,
                    EmployeeId = model.empid,
                };

                generalSettingsService.Create(general);

                var selectedWeekends = model.Weekends.Where(x => x.isSelected).Select(x => x.displayValue).ToList();
                var created = new List<WeekendDTO>();
                foreach (var selectedDay in selectedWeekends)
                {
                    var weekend = new Weekend
                    {
                        Name = selectedDay,
                        GeneralSettingsId = general.Id
                    };
                    if (weekendService.CheckPublicHolidaysExists(weekend))
                    {
                        ModelState.AddModelError("WeekendDay", $"the day {weekend.Name} already selected before!");
                        return BadRequest(ModelState);
                    }
                    weekendService.Create(weekend);

                    created.Add(model);
                }
                if (model.empid == null)
                {
                    model.empid = 0;
                }
                model.Id= general.Id;
               
                return Ok(model);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", message = ex.Message });
            }
        }


        [HttpDelete("{id}")]
        public ActionResult Remove(int id)
        {
            try
            {
                var remove = generalSettingsService.GetGeneralSettingByID(id);
                if (remove == null)
                {
                    return NotFound(new { error = "Not Found" });
                }
                generalSettingsService.Remove(remove);
                return Ok(remove);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", message = ex.Message });
            }

        }

        #endregion
    }
}





