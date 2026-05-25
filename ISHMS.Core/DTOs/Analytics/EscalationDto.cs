using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISHMS.Core.DTOs.Analytics
{
    public class EscalationDto
    {
        public string PatientName { get; set; }

        public int EscalationLevel { get; set; }

        public string LevelName { get; set; }

        public string ActionRequired { get; set; }

        public List<string> Reasons { get; set; }
    }
}
