using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISHMS.Core.Enums;

namespace ISHMS.Core.DTOs.Analytics
{
    public class BedMapDto
    {
        public int TotalBeds { get; set; }

        public int OccupiedBeds { get; set; }

        public int AvailableBeds { get; set; }

        public List<BedInfoDto> Beds { get; set; }
    }
    public class BedInfoDto
    {
        public string DepartmentName { get; set; }

        public string RoomNumber { get; set; }

        public int BedNumber { get; set; }

        public string? PatientName { get; set; }

        public PatientFlowStatus? FlowStatus { get; set; }

        public int? NewsScore { get; set; }

        public int? LengthOfStayDays { get; set; }

        public string BedStatus { get; set; }
    }
}
