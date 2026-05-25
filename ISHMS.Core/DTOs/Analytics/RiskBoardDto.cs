using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISHMS.Core.Enums;

namespace ISHMS.Core.DTOs.Analytics
{
    public class RiskBoardDto
    {
        public string PatientName { get; set; }

        public string Department { get; set; }

        public int NewsScore { get; set; }

        public int DeteriorationScore { get; set; }

        public string RiskLevel { get; set; }

        public double OxygenSaturation { get; set; }

        public int HeartRate { get; set; }

        public double Temperature { get; set; }

        public int ActiveAlerts { get; set; }

        public int LengthOfStayDays { get; set; }

        public PatientFlowStatus FlowStatus { get; set; }

        public DateTime LastVitalsTime { get; set; }

        public List<string> Findings { get; set; }

        public List<string> Recommendations { get; set; }
    }
}
