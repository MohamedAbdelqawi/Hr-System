using Hr.Application.DTOs;
using Hr.Application.DTOs.Attendance;
using Hr.Application.DTOs.Employee;
using Hr.Application.Services.implementation;
using Hr.Application.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Hr.System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceServices attendanceServices;
        private readonly IEmployeeServices employeeServices;

        public AttendanceController(IAttendanceServices attendanceServices, IEmployeeServices employeeServices)
        {
            this.attendanceServices = attendanceServices;
            this.employeeServices = employeeServices;
        }


        [HttpGet("GetAllEmployeeWithoutAttendance")]
        public IActionResult GetAllEmployeeWithoutAttendance()
        {
            try
            {
                var EmployeDto = attendanceServices.GetAllEmployeeForAttendance();

                return Ok(EmployeDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", message = ex.Message });
            }
        }




        [HttpPost("FilterAttendances")]
        public IActionResult FilterAttendances(AtendanceFilterDto filter)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (!DateTime.TryParse(filter.From, out DateTime from) || !DateTime.TryParse(filter.To, out DateTime to))
                    {
                        ModelState.AddModelError("Error", "Invalid date format");
                        return BadRequest(ModelState);
                    }

                    if (from > to)
                    {
                        ModelState.AddModelError("Error", "From date cannot be greater than To date");
                        return BadRequest(ModelState);
                    }
                    var filteredAttendances = attendanceServices.FilterAttendancesByDateRange(filter);

                    // You can now use the 'filteredAttendances' list as needed

                    return Ok(filteredAttendances);
                }
                else
                {
                    return BadRequest(ModelState);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet("GetEmployeeList")]
        public ActionResult GetEmployeeList()
        {
            try
            {
                var AttendanceDto = employeeServices.GetAllEmployeeForAttendance();
                return Ok(AttendanceDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            try
            {
                var attendanceDtos = attendanceServices.GetAllAttendance();

                return Ok(attendanceDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", message = ex.Message });
            }

        }

        [HttpGet("{id:int}")]
        public IActionResult GetAttendanceById(int id)
        {
            try
            {
                var attendanceDto = attendanceServices.GetAttendanceId(id);

                if (attendanceDto == null)
                {
                    return NotFound();
                }
                return Ok(attendanceDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult Create(AttendanceEmployeDto attendanceEmployeDto)
        {
            try
            {
                if (ModelState.IsValid)
                {

                    DateTime date = DateTime.Parse(attendanceEmployeDto.Date);
                    DateTime dateToCheck = date;



                    string dayOfWeek = attendanceServices.GetDayOfWeekForDate(dateToCheck);
                    var employee = employeeServices.GetEmployeeId(attendanceEmployeDto.SelectedEmployee);

                    TimeSpan arrivalTime = TimeSpan.Parse(attendanceEmployeDto.ArrivalTime);
                    TimeSpan arrivalTimeFromDb = TimeSpan.Parse(employee.ArrivalTime);
                    List<string> employeeWeekendDays = attendanceServices.GetEmployeeWeekendDays(attendanceEmployeDto.SelectedEmployee);
                    if (attendanceServices.CheckAttendanceExists(attendanceEmployeDto))
                    {
                        ModelState.AddModelError("Exists", "The Employee Has Attendance in this day");
                        return BadRequest(ModelState);
                    }
                    if (employeeWeekendDays.Contains(dayOfWeek))
                    {
                        ModelState.AddModelError("Date", "Attendance on a weekend day is not allowed.");
                        return BadRequest(ModelState);
                    }
 
                    TimeSpan.TryParse(employee.LeaveTime, out TimeSpan leave);
                    if (leave < arrivalTime)
                    {
                        ModelState.AddModelError("Error", $"Arrival Time Must Be less Than Defualt Leave For Employee {leave}");
                        return BadRequest(ModelState);
                    }


                    if (arrivalTime < arrivalTimeFromDb)
                    {
                        ModelState.AddModelError("ArrivalTime", $"Arrival Time Must Be Greater Than Defualt Arrival For Employee {arrivalTimeFromDb}");
                        return BadRequest(ModelState);
                    }
                    var attendanceDto = new AttendanceEmployeDto
                    {
                        ArrivalTime = attendanceEmployeDto.ArrivalTime,
                        LeaveTime = attendanceEmployeDto.LeaveTime,
                        Date = attendanceEmployeDto.Date,
                        SelectedEmployee = attendanceEmployeDto.SelectedEmployee
                    };

                    attendanceServices.CreateAttendance(attendanceDto);

                    return Ok(new { message = "Attendance record created successfully." });
                }
                else
                {
                    return BadRequest(ModelState);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public IActionResult Update(AttendanceEmployeDto attendanceEmployeDto, int id)
        {
            try
            {
                if (ModelState.IsValid)
                {

                    TimeSpan.TryParse(attendanceEmployeDto.ArrivalTime, out TimeSpan arrivalTime);
                    TimeSpan.TryParse(attendanceEmployeDto.LeaveTime, out TimeSpan leaveTime);
                    if (attendanceEmployeDto.LeaveTime != null && arrivalTime >= leaveTime)
                    {
                        ModelState.AddModelError("Error", "Leave time cannot be Less than or equal to arrival time.");
                        return BadRequest(ModelState);
                    }
                    var employee = employeeServices.GetEmployeeId(attendanceEmployeDto.SelectedEmployee);
                    TimeSpan arrivalTimeFromDb = TimeSpan.Parse(employee.ArrivalTime);
                    if (arrivalTime < arrivalTimeFromDb)
                    {
                        ModelState.AddModelError("Error", $"Arrival Time Must Be Greater Than Defualt Arrival For Employee {arrivalTimeFromDb}");
                        return BadRequest(ModelState);
                    }
                    if (attendanceServices.GetAllAttendance().Any(
                        x => x.Date == attendanceEmployeDto.Date &&
                        x.SelectedEmployee == attendanceEmployeDto.SelectedEmployee &&
                        x.Id != attendanceEmployeDto.Id))
                    {
                        ModelState.AddModelError("Error", "the name is founded ");
                        return BadRequest(ModelState);
                    }
                    attendanceServices.UpdateAttendance(attendanceEmployeDto, id);
                    return Ok(new { message = "Attendance record updeted successfully." });
                }
                return BadRequest(ModelState);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", message = ex.Message });
            }
        }



        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                bool deleted = attendanceServices.DeleteAttendance(id);

                if (deleted)
                {
                    return Ok(new { message = "Attendance record deleted successfully." });
                }
                else
                {
                    return NotFound(new { message = "Attendance record not found." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", message = ex.Message });
            }
        }
    }
}
