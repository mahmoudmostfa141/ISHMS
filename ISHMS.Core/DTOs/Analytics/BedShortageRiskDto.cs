using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISHMS.Core.DTOs.Analytics
{
    public class BedShortageRiskDto
    {
        public string DepartmentName { get; set; }

        public int TotalBeds { get; set; }

        public int AvailableBeds { get; set; }

        public double OccupancyRate { get; set; }

        public double AverageDailyAdmissions { get; set; }

        public int ExpectedDischarges { get; set; }

        public string RiskLevel { get; set; }
    }
}
