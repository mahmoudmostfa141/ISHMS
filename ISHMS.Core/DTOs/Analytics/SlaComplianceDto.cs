using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISHMS.Core.DTOs.Analytics
{
    public class SlaComplianceDto
    {
        public string RoleName { get; set; }

        public int TotalTasks { get; set; }

        public int CompletedInTime { get; set; }

        public double ComplianceRate { get; set; }

    }
}
