using Hr.Application.DTOs;
using Hr.Application.DTOs.Attendance;
using Hr.Application.DTOs.Employee;
using Hr.Application.Interfaces;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Hr.Application.Common.Global.Permission;
 

namespace Hr.Application.Services.implementation
{
    public class AttendanceServices : IAttendanceServices
    {
        private readonly IUnitOfWork uniteOfWork;
        private readonly IEmployeeServices employeeServices;
        private readonly IGeneralSettingsService generalSettingsService;
        private readonly IDepartmentService departmentService;

        public AttendanceServices(IUnitOfWork uniteOfWork, IEmployeeServices employeeServices, IGeneralSettingsService generalSettingsService, IDepartmentService departmentService)
        {
            this.uniteOfWork = uniteOfWork;
            this.employeeServices = employeeServices;
            this.generalSettingsService = generalSettingsService;
            this.departmentService = departmentService;
        }


        public IEnumerable<GetAllEmployeAttendanceDto> GetAllEmployeeForAttendance()
        {
            var listOfEmployee = new List<GetAllEmployeAttendanceDto>();
            //  var currentDate = DateTime.Now;

            var attendance = GetAllAttendance();

            var currentDate = DateTime.Now.Date;
            // Get the date part of DateTime.Now
            // Create a HashSet to store the dates when attendance was recorded
            var employeesWithAttendance = uniteOfWork.EmployeeRepository.GetAll(includeProperties: "Attendance")
                .Where(employee => !employee.Attendance.Any(a => a.Date.Date == currentDate))
                     .AsEnumerable();

            foreach (var employee in employeesWithAttendance)
            {
                var emp = new GetAllEmployeAttendanceDto()
                {
                    Id = employee.Id,
                    Name = employee.FirstName + " " + employee.LastName,
                };
                listOfEmployee.Add(emp);
            }
            return listOfEmployee;
        }


        public IEnumerable<AttendanceEmployeDto> FilterAttendancesByDateRange(AtendanceFilterDto filterDto)
        {
            if (!DateTime.TryParse(filterDto.From, out DateTime from) || !DateTime.TryParse(filterDto.To, out DateTime to))
            {
                // Handle parsing error
                throw new ArgumentException("Invalid date format");
            }

            var attendances = uniteOfWork.AttendanceRepository.GetAll(includeProperties: "Employee")
                .Where(attend => attend.Date.Date >= from.Date && attend.Date.Date <= to.Date).ToList();

            var filteredAttendances = new List<AttendanceEmployeDto>();
            foreach (var item in attendances)
            {
                var department = departmentService.GetDepartmentId(item.Employee.DepartmentId);

                filteredAttendances.Add(new AttendanceEmployeDto
                {
                    Id = item.Id,
                    Date = item.Date.ToString("yyyy-MM-dd"),
                    ArrivalTime = item.ArrivalTime.ToString("hh\\:mm"),  // Assuming ArrivalTime is in DateTime format
                    LeaveTime = item.LeaveTime?.ToString("hh\\:mm") ?? "N/A",  // Assuming LeaveTime is nullable DateTime
                    DepartmentName = department.Name,
                    EmployeeName = item.Employee.FirstName + " " + item.Employee.LastName,
                    SelectedEmployee= item.EmployeeId   
                     
                });
            }
            
            return filteredAttendances;
        }


        public AttendanceEmployeDto GetAttendanceById(int id)
        {
            var attendance = uniteOfWork.AttendanceRepository.Get(x => x.Id == id);
            if (attendance != null)
            {
                var attendanceDto = new AttendanceEmployeDto()
                {

                    Id = attendance.Id,
                    ArrivalTime = attendance.ArrivalTime.ToString("hh\\:mm"),
                    LeaveTime = attendance.LeaveTime?.ToString("hh\\:mm"),

                    Date = attendance.Date.ToString("yyyy-MM-dd"),

                    SelectedEmployee = attendance.EmployeeId
                };
                return attendanceDto;
            }
            else
            {
                throw new Exception("Not Found");
            }
        }


        public IEnumerable<AttendanceEmployeDto> GetAllAttendance()
        {
            var attendanceDto = new List<AttendanceEmployeDto>();
            var attendances = uniteOfWork.AttendanceRepository.GetAll().Where(x=>x.Date.Date == DateTime.Now.Date);
            if (attendances != null)
            {
                foreach (var attendance in attendances)
                {
                    var employee = employeeServices.GetEmployeeId(attendance.EmployeeId);
                    
                    var employeAttendance = new AttendanceEmployeDto()
                    {
                        Id = attendance.Id,
                        SelectedEmployee = employee.ID,
                        Date = attendance.Date.ToString("yyyy-MM-dd"),
                        ArrivalTime = attendance.ArrivalTime.ToString("hh\\:mm"),
                        DepartmentName = employee.DeptName,
                        LeaveTime = attendance.LeaveTime?.ToString("hh\\:mm"),

                    };
                    employeAttendance.EmployeeName = $"{employee.FirstName} {employee.LastName}";
                    attendanceDto.Add(employeAttendance);
                }
                return attendanceDto;
            }
            else
            {
                throw new Exception("No Attendance is Founded");
            }
        }


        public bool CheckAttendanceExists(AttendanceEmployeDto attendanceDto)
        {
            DateTime date= DateTime.Parse(attendanceDto.Date).Date;
            return uniteOfWork.AttendanceRepository.Any(x => x.Date.Date == date && x.EmployeeId == attendanceDto.SelectedEmployee);
        }

        public string GetDayOfWeekForDate(DateTime date)
        {
            return date.DayOfWeek.ToString();
        }

        public List<string> GetEmployeeWeekendDays(int employeeId)
        {
            var weekendDays = new List<string>(); // Change the type to string
            var employee = uniteOfWork.EmployeeRepository.Get(x => x.Id == employeeId, includeProperties: "GeneralSettings");
            int generalId = 0;
            var getall = generalSettingsService.GetAllGeneralSettings();
            var generalSettingsWithNullEmployeeId = generalSettingsService.GetGeneralSettingForAll();

            if (generalSettingsWithNullEmployeeId != null)
            {
                generalId = generalSettingsWithNullEmployeeId.Id;
            }
            if (employee != null)
            {
                var generalSettings = employee.GeneralSettings;

                if (generalSettings.Count != 0)
                {
                    foreach (var setting in generalSettings) // Iterate over the collection
                    {
                        weekendDays.AddRange(generalSettingsService.GetWeekendDaysForGeneralSettings(setting.Id));
                    }
                }
                else
                {
                    var matchingWeekends = uniteOfWork.WeekendRepository.GetAll(weekend => weekend.GeneralSettingsId == generalId);

                    weekendDays.AddRange(matchingWeekends.Select(weekend => weekend.Name));
                }
            }

            return weekendDays;
        }

        public AttendanceEmployeDto GetAttendanceId(int id)
        {
            var existedAttendance = uniteOfWork.AttendanceRepository.Get(x => x.Id == id);
            if (existedAttendance != null)
            {
                var attendance = new AttendanceEmployeDto()
                {
                    Id = id,
                    ArrivalTime = existedAttendance.ArrivalTime.ToString("hh\\:mm"),
                    LeaveTime = existedAttendance.LeaveTime?.ToString(@"hh\:mm"),
                    Date = existedAttendance.Date.ToString("yyyy-MM-dd"),
                    SelectedEmployee = existedAttendance.EmployeeId,
                    
                };
                return attendance;
            }
            else
            {
                throw new Exception("Not Found");
            }
          

        }
        public void CreateAttendance(AttendanceEmployeDto attendanceDto)
        {
            try
            {
                TimeSpan arrivalTime = TimeSpan.ParseExact(attendanceDto.ArrivalTime, "hh\\:mm", CultureInfo.InvariantCulture);
                var attendance = new Domain.Entities.Attendance()
                {
                    ArrivalTime = arrivalTime,
                    LeaveTime = null,
                    Date = DateTime.Now,
                    EmployeeId = attendanceDto.SelectedEmployee,
                };
                if (attendance != null)
                {
                    uniteOfWork.AttendanceRepository.Add(attendance);
                    uniteOfWork.Save();
                }
                else
                {
                    throw new Exception("Attendance is error");
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public void UpdateAttendance(AttendanceEmployeDto attendanceDto, int id)
        {
            try
            {
                var attendanceFromDb = uniteOfWork.AttendanceRepository.Get(x => x.Id == id);
                TimeSpan arrivalTime = TimeSpan.ParseExact(attendanceDto.ArrivalTime, "hh\\:mm", CultureInfo.InvariantCulture);
                TimeSpan leaveTime =new TimeSpan();
                if (attendanceDto.LeaveTime != null)
                {
                    leaveTime = TimeSpan.ParseExact(attendanceDto.LeaveTime, "hh\\:mm", CultureInfo.InvariantCulture);
                    attendanceFromDb.Date = DateTime.Now;
                    attendanceFromDb.ArrivalTime = arrivalTime;
                    attendanceFromDb.LeaveTime = leaveTime;
                    attendanceFromDb.EmployeeId = attendanceDto.SelectedEmployee;
                    uniteOfWork.AttendanceRepository.update(attendanceFromDb);
                    uniteOfWork.Save();
                }
                else if (attendanceDto.LeaveTime == null)
                {
                    attendanceFromDb.Date = DateTime.Now;
                    attendanceFromDb.ArrivalTime = arrivalTime;
                    attendanceFromDb.LeaveTime = null;
                    attendanceFromDb.EmployeeId = attendanceDto.SelectedEmployee;
                    uniteOfWork.AttendanceRepository.update(attendanceFromDb);
                    uniteOfWork.Save();
                }
                else
                {
                    throw new Exception("Not found");
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public bool DeleteAttendance(int id)
        {
            var attandence = uniteOfWork.AttendanceRepository.Get(x => x.Id == id);
            if (attandence != null)
            {
                uniteOfWork.AttendanceRepository.Remove(attandence);
                uniteOfWork.Save();
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
