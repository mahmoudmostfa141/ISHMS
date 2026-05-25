using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISHMS.Core.DTOs.Analytics
{
    public class PeakHourDto
    {
        public int Hour { get; set; }

        public int AdmissionCount { get; set; }

        public double AverageNewsScore { get; set; }
    }
}
