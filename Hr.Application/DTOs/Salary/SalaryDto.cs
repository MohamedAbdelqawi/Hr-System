using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hr.Application.DTOs.Salary
{
    public class SalaryDto
    {
        public string Name { get; set; }

        public string Department { get; set; }
        public string BaseSalary { get; set; } = "0";
        public string AttendanceDays { get; set; } = "0";
        public string AbsenceDays { get; set; } = "0";
        public string AdditionalPerHour { get; set; } = "0";
        public string HourlyDiscount { get; set; } = "0";
        public string TotalDiscount { get; set; } = "0";
        public string TotalAdditional { get; set; } ="0";
        public string NetSalary { get; set; }
    }
}
