using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISHMS.Core.DTOs.Analytics
{
    public class AlertTrendDto
    {
        public DateTime Date { get; set; }

        public int CriticalCount { get; set; }

        public int WarningCount { get; set; }

        public int InfoCount { get; set; }

        public int Total { get; set; }
    }
}
