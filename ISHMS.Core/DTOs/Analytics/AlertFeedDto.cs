using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISHMS.Core.Enums;

namespace ISHMS.Core.DTOs.Analytics
{
    public class AlertFeedDto
    {
        public string PatientName { get; set; }

        public string Message { get; set; }

        public AlertSeverity Severity { get; set; }
        public int NewsScore { get; set; } = 0;
        public bool IsRead { get; set; }

        public int MinutesAgo { get; set; }

        public int RepeatedCount { get; set; }

        public string Priority { get; set; }
    }
}
