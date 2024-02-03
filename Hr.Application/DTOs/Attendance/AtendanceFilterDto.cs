using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hr.Application.DTOs.Attendance
{
    public class AtendanceFilterDto
    {
        [DataType(DataType.Date)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:yyyy-MM-dd}")]
        public string? From{ get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:yyyy-MM-dd}")]
        public string? To{ get; set; }

        public List<AttendanceEmployeDto>? attendanceEmployeDtos { get; set; } = new List<AttendanceEmployeDto>();
    }
}
