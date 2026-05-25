using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISHMS.Core.DTOs.Analytics
{
    public class VitalTrendDto
    {
        public string PatientName { get; set; }

        public List<VitalReadingDto> Readings { get; set; }
    }
    public class VitalReadingDto
    {
        public DateTime RecordedAt { get; set; }

        public double OxygenSaturation { get; set; }

        public int HeartRate { get; set; }

        public double Temperature { get; set; }

        public int SystolicBP { get; set; }

        public int RespiratoryRate { get; set; }
    }
}
