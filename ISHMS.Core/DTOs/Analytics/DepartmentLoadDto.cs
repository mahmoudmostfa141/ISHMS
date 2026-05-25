using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISHMS.Core.DTOs.Analytics
{
    public class DepartmentLoadDto
    {
        public string DepartmentName { get; set; }

        public int TotalBeds { get; set; }

        public int OccupiedBeds { get; set; }

        public double OccupancyRate { get; set; }

        public int ActivePatients { get; set; }

        public int UnreadAlerts { get; set; }

        public int OverdueTasks { get; set; }
        public int TotalNewsScore { get; set; }


        public double LoadScore { get; set; }

        public string LoadLevel { get; set; }
    }
}
