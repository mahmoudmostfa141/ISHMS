using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISHMS.Core.DTOs.Analytics
{
    public class ExecutiveSummaryDto
    {
        public int CurrentPatients { get; set; }

        public double BedOccupancyRate { get; set; }

        public double AverageNewsScore { get; set; }

        public int CriticalAlertsToday { get; set; }

        public int EmptyBeds { get; set; }

        public int OverdueTasks { get; set; }

        public string OccupancyStatus { get; set; }
    }
}
