using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISHMS.Core.DTOs.Analytics;
using ISHMS.Core.Enums;
using ISHMS.Core.Interfaces;
using ISHMS.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ISHMS.BLL.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly AppDbContext _context;

        public AnalyticsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ExecutiveSummaryDto> ExecutiveSummary()
        {
            int countPatients = await _context.Patients.Where(p => p.FlowStatus != PatientFlowStatus.Discharged).CountAsync();

            double TotalBeds = await _context.Beds.CountAsync();
            double OccupiedBeds = await _context.Beds.Where(b => b.IsOccupied).CountAsync();
            double BedOccupancyRate = ((double)OccupiedBeds / TotalBeds) * 100;

            double AvgNewsScore = await _context.Patients.Select(p => (double?)p.NewsScore).AverageAsync() ?? 0;

            int CriticalAlertsToday = await _context.Alerts.CountAsync(a => a.Severity == AlertSeverity.Critical && a.CreatedAt.Date == DateTime.Today);

            int EmptyBeds = await _context.Beds.CountAsync(b => !b.IsOccupied);

            int OverdueTasks = await _context.PatientTasks.CountAsync(t => t.Status == PatientTaskStatus.Pending || t.Status == PatientTaskStatus.InProgress);

            string OccupancyStatus = BedOccupancyRate < 60 ? "Normal" : BedOccupancyRate < 85 ? "Warning" : "Criticl";

            ExecutiveSummaryDto summary = new ExecutiveSummaryDto
            {
                CurrentPatients = countPatients,
                BedOccupancyRate = BedOccupancyRate,
                AverageNewsScore = AvgNewsScore,
                CriticalAlertsToday = CriticalAlertsToday,
                EmptyBeds = EmptyBeds,
                OverdueTasks = OverdueTasks,
                OccupancyStatus = OccupancyStatus
            };
            return summary;
        }
        public async Task<List<DepartmentLoadDto>> DepartmentLoad()
        {
            var Depts = await _context.Departments

                .Select(d => new DepartmentLoadDto

                {
                    DepartmentName = d.Name,
                    TotalBeds = d.Rooms.SelectMany(r => r.Beds).Count(),
                    OccupiedBeds = d.Rooms.SelectMany(r => r.Beds).Count(b => b.IsOccupied),
                    OccupancyRate = d.Rooms.SelectMany(r => r.Beds).Count() == 0 ? 0 : ((double)d.Rooms.SelectMany(r => r.Beds).Count(b => b.IsOccupied) / d.Rooms.SelectMany(r => r.Beds).Count()) * 100,
                    ActivePatients = d.Rooms.SelectMany(r => r.Beds).Count(b => b.IsOccupied),
                    UnreadAlerts = d.Rooms.SelectMany(r => r.Beds).Where(b => b.Patient != null).SelectMany(b => b.Patient.Alerts).Count(a => !a.IsRead),
                    OverdueTasks = d.Rooms.SelectMany(r => r.Beds).Where(b => b.Patient != null).SelectMany(b => b.Patient.Tasks).Count(t => (t.Status == PatientTaskStatus.Pending || t.Status == PatientTaskStatus.InProgress) && t.DueAt < DateTime.Now),
                    TotalNewsScore = d.Rooms.SelectMany(r => r.Beds).Where(b => b.Patient != null).Sum(b => b.Patient.NewsScore),
                    LoadScore = 0,
                    LoadLevel = "Pending"
                }).ToListAsync();
            foreach (var d in Depts)
            {
                d.LoadScore =
                    d.ActivePatients * 1.0 + d.TotalNewsScore * .5 + d.UnreadAlerts * 2.0 + d.OverdueTasks * 3.0;


                d.LoadLevel = d.LoadScore >= 80 ? "Critical"
              : d.LoadScore >= 50 ? "High"
              : d.LoadScore >= 25 ? "Medium" : "Low";
            }
            return Depts;
        }
        public async Task<List<AlertTrendDto>> AlertTrend(int days = 7)
        {
            var startDate = DateTime.UtcNow.AddDays(-days);

            var alerts = await _context.Alerts.Where(a => a.CreatedAt >= startDate)
                .GroupBy(a => a.CreatedAt.Date)
                .OrderBy(g => g.Key)
                .Select(g => new AlertTrendDto
                {
                    Date = g.Key,
                    CriticalCount = g.Count(a => a.Severity == AlertSeverity.Critical),
                    WarningCount = g.Count(a => a.Severity == AlertSeverity.Warning),
                    InfoCount = g.Count(a => a.Severity == AlertSeverity.Info),
                    Total = g.Count()

                }).ToListAsync();
            return alerts;
        }
        public async Task<List<SlaComplianceDto>> SlaCompliance()
        {
            var comps = await _context.PatientTasks.GroupBy(t => t.AssignedToRole).Select(g => new SlaComplianceDto
            {
                RoleName = g.Key,
                TotalTasks = g.Count(),
                CompletedInTime = g.Count(t => t.CompletedAt != null && t.CompletedAt < t.DueAt),
                ComplianceRate = 0//g.Count() == 0 ? 0 : ((double)(g.Count(t => t.CompletedAt < t.DueAt)) / g.Count()) * 100
            }).ToListAsync();
            foreach (var t in comps)
            {
                t.ComplianceRate = t.TotalTasks == 0 ? 0 : ((double)t.CompletedInTime / t.TotalTasks) * 100;
            }
            return comps;
        }
        public async Task<List<RiskBoardDto>> RiskBoard(
string? department = null,
string? riskBand = null)
        {
            var patients = await _context.Patients

            .Where(p =>
                department == null
                ||
                p.Bed.Room.Department.Name == department)

            .Select(p => new RiskBoardDto
            {
                PatientName = p.FullName,

                Department =
                p.Bed.Room.Department.Name,

                NewsScore = p.NewsScore,

                OxygenSaturation =
                p.VitalSigns
                .OrderByDescending(v => v.RecordedAt)
                .Select(v => v.OxygenLevel)
                .FirstOrDefault(),

                HeartRate =
                p.VitalSigns
                .OrderByDescending(v => v.RecordedAt)
                .Select(v => v.HeartRate)
                .FirstOrDefault(),

                Temperature =
                p.VitalSigns
                .OrderByDescending(v => v.RecordedAt)
                .Select(v => v.Temperature)
                .FirstOrDefault(),

                ActiveAlerts =
                p.Alerts
                .Count(a => !a.IsRead),

                LengthOfStayDays =
                EF.Functions.DateDiffDay(
                    p.AdmittedAt,
                    DateTime.UtcNow
                ),

                FlowStatus = p.FlowStatus,

                LastVitalsTime =
                p.VitalSigns
                .OrderByDescending(v => v.RecordedAt)
                .Select(v => v.RecordedAt)
                .FirstOrDefault(),

                DeteriorationScore = 0,

                RiskLevel = "",

                Findings = new List<string>(),

                Recommendations = new List<string>()
            })

            .ToListAsync();


            foreach (var p in patients)
            {
                p.DeteriorationScore =

                p.NewsScore * 2

                +

                (p.ActiveAlerts * 3)

                +

                (p.OxygenSaturation < 94 ? 5 : 0)

                +

                (
                p.HeartRate > 110
                ||
                p.HeartRate < 50
                ? 4
                : 0
                )

                +

                (
                p.Temperature > 38.5
                ? 3
                : 0
                )

                +

                (
                p.LengthOfStayDays > 7
                ? 2
                : 0
                );


                p.RiskLevel =
                p.DeteriorationScore >= 30
                ? "Critical"
                :
                p.DeteriorationScore >= 20
                ? "High"
                :
                p.DeteriorationScore >= 10
                ? "Medium"
                :
                "Low";


                if (p.OxygenSaturation < 94)
                    p.Findings.Add("Low O2");

                if (p.Temperature > 38.5)
                    p.Findings.Add("High Temperature");

                if (p.HeartRate > 110)
                    p.Findings.Add("High Heart Rate");


                if (p.NewsScore >= 7)
                    p.Recommendations.Add(
                    "Immediate doctor review");

                if (p.OxygenSaturation < 94)
                    p.Recommendations.Add(
                    "Oxygen assessment");
            }

            if (riskBand != null)
            {
                patients =
                patients
                .Where(p =>
                    p.RiskLevel == riskBand)
                .ToList();
            }

            return patients;
        }
        public async Task<VitalTrendDto> VitalTrend(int patientId,int hours = 24)
        {
            var startTime =
                DateTime.UtcNow.AddHours(-hours);

            var patient = await _context.Patients
                .Where(p => p.Id == patientId)

                .Select(p => new VitalTrendDto
                {
                    PatientName = p.FullName,

                    Readings =
                    p.VitalSigns

                    .Where(v =>
                        v.RecordedAt >= startTime)

                    .OrderBy(v => v.RecordedAt)

                    .Select(v =>
                        new VitalReadingDto
                        {
                            RecordedAt =
                            v.RecordedAt,

                            OxygenSaturation =
                            v.OxygenLevel,

                            HeartRate =
                            v.HeartRate,

                            Temperature =
                            v.Temperature,

                            SystolicBP =
                            v.SystolicPressure,

                            RespiratoryRate =
                            v.RespirationRate
                        })

                    .ToList()
                })

                .FirstOrDefaultAsync();

            return patient;
        }
        public async Task<List<AlertFeedDto>> AlertFeed(int limit = 20)
        {
            var alerts = await _context.Alerts

                .OrderByDescending(a => a.CreatedAt)

                .Take(limit)

                .Select(a => new AlertFeedDto
                {
                    PatientName = a.Patient.FullName,

                    Message = a.Message,

                    Severity = a.Severity,
                   
                    NewsScore=a.Patient.NewsScore,

                    IsRead = a.IsRead,

                    MinutesAgo =
                        EF.Functions.DateDiffMinute(a.CreatedAt, DateTime.UtcNow),

                    RepeatedCount =
                        _context.Alerts.Count(x =>
                            x.PatientId == a.PatientId
                            &&
                            x.Message == a.Message),

                    Priority = ""
                })

                .ToListAsync();

            foreach (var a in alerts)
            {
                int PrNm = CalculatePriority(a);
                a.Priority =
                    PrNm>=16
                    ? "URGENT"

                    : PrNm >= 8
                    ? "HIGH PRIORITY"

                    : "STANDARD";
            }

            return alerts;
        }
        private int CalculatePriority(AlertFeedDto alert)
        {
            int priority = 0;
            if (alert.Severity == AlertSeverity.Critical) priority += 16;
            if (alert.NewsScore > 7) priority += 3;
            if (alert.MinutesAgo < 60) priority += 2;
            if (alert.RepeatedCount > 3) priority += 2;

            return priority;
        }
        public async Task<List<EscalationDto>> Escalations()
        {
            var patients = await _context.Patients
                .Select(p => new
                {
                    PatientName = p.FullName,

                    NewsScore = p.NewsScore,

                    OxygenSaturation =
                        p.VitalSigns
                        .OrderByDescending(o => o.RecordedAt)
                        .Select(o => o.OxygenLevel)
                        .FirstOrDefault(),

                    HeartRate =
                        p.VitalSigns
                        .OrderByDescending(o => o.RecordedAt)
                        .Select(o => o.HeartRate)
                        .FirstOrDefault(),

                    DelayedTasks =
                        p.Tasks.Count(t =>
                            (
                                t.Status == PatientTaskStatus.Pending
                                ||
                                t.Status == PatientTaskStatus.InProgress
                            )
                            &&
                            t.DueAt < DateTime.UtcNow.AddHours(-2))
                })

                .ToListAsync();

            var result = new List<EscalationDto>();


            foreach (var p in patients)
            {
                int level = 0;

                List<string> reasons = new();


                if (p.NewsScore > 9)
                {
                    level = 3;
                    reasons.Add("NEWS > 9");
                }

                if (p.OxygenSaturation < 90)
                {
                    level = 3;
                    reasons.Add("O2 < 90%");
                }

                if (p.HeartRate > 130)
                {
                    level = 3;
                    reasons.Add("HR > 130");
                }

                if (level < 3)
                {
                    if (p.NewsScore > 7)
                    {
                        level = 2;
                        reasons.Add("NEWS > 7");
                    }

                    if (p.DelayedTasks > 2)
                    {
                        level = 2;
                        reasons.Add("More than 2 delayed tasks");
                    }
                }

                if (level == 0)
                {
                    level = 1;
                    reasons.Add("Monitoring Alert");
                }


                result.Add(new EscalationDto
                {
                    PatientName = p.PatientName,

                    EscalationLevel = level,

                    LevelName =
                        level == 3
                        ? "CRITICAL ESCALATION"
                        : level == 2
                        ? "URGENT REVIEW"
                        : "MONITORING ALERT",

                    ActionRequired =
                        level == 3
                        ? "Immediate intervention"
                        : level == 2
                        ? "Doctor review"
                        : "Continuous monitoring",

                    Reasons = reasons
                });
            }

            return result;
        }
        public async Task<BedMapDto> BedMap(string? department = null)
        {
            var beds = await _context.Beds

                .Where(b =>
                    department == null
                    ||
                    b.Room.Department.Name == department)

                .Select(b => new BedInfoDto
                {
                    DepartmentName =
                        b.Room.Department.Name,

                    RoomNumber =b.Room.RoomNumber,

                    BedNumber =
                        b.Id,

                    PatientName =
                        b.Patient != null
                        ?
                        b.Patient.FullName
                        :
                        null,

                    FlowStatus =
                        b.Patient != null
                        ?
                        b.Patient.FlowStatus
                        :
                        null,

                    NewsScore =
                        b.Patient != null
                        ?
                        b.Patient.NewsScore
                        :
                        null,

                    LengthOfStayDays =
                        b.Patient != null
                        ?
                        EF.Functions.DateDiffDay(
                            b.Patient.AdmittedAt,
                            DateTime.UtcNow
                        )
                        :
                        null,

                    BedStatus =
                        !b.IsOccupied
                        ?
                        "Free"

                        :

                        b.Patient.FlowStatus ==
                        PatientFlowStatus.Stable

                        ?

                        "Pending Discharge"

                        :

                        "Occupied"
                })

                .ToListAsync();


            var result = new BedMapDto
            {
                TotalBeds =
                    beds.Count(),

                OccupiedBeds =
                    beds.Count(b =>
                        b.BedStatus != "Free"),

                AvailableBeds =
                    beds.Count(b =>
                        b.BedStatus == "Free"),

                Beds = beds
            };

            return result;
        }
        public async Task<List<PeakHourDto>> PeakHours()
        {
            var startDate =
                DateTime.UtcNow.AddDays(-30);

            var data = await _context.Patients

                .Where(p =>
                    p.AdmittedAt >= startDate)

                .GroupBy(p =>
                    p.AdmittedAt.Hour)

                .Select(g => new PeakHourDto
                {
                    Hour = g.Key,

                    AdmissionCount =
                        g.Count(),

                    AverageNewsScore =
                        g.Average(
                            p => (double?)p.NewsScore
                        ) ?? 0
                })

                .OrderBy(x => x.Hour)

                .ToListAsync();

            return data;
        }

        public async Task<List<BedShortageRiskDto>>BedShortageRisk()
        {
            var depts = await _context.Departments

            .Select(d => new BedShortageRiskDto
            {
                DepartmentName =
                    d.Name,

                TotalBeds =
                    d.Rooms
                    .SelectMany(r => r.Beds)
                    .Count(),

                AvailableBeds =
                    d.Rooms
                    .SelectMany(r => r.Beds)
                    .Count(b => !b.IsOccupied),

                OccupancyRate = 0,

                AverageDailyAdmissions =
                    d.Rooms
                    .SelectMany(r => r.Beds)
                    .Where(b =>
                        b.Patient != null &&
                        b.Patient.AdmittedAt
                        >= DateTime.UtcNow.AddDays(-7))

                    .Count() / 7.0,

                ExpectedDischarges =
                    d.Rooms
                    .SelectMany(r => r.Beds)
                    .Count(b =>
                        b.Patient != null
                        &&
                        b.Patient.NewsScore <= 2),

                RiskLevel = ""
            })

            .ToListAsync();


            foreach (var d in depts)
            {
                d.OccupancyRate =
                    d.TotalBeds == 0
                    ? 0
                    :
                    (
                        (
                            (double)
                            (d.TotalBeds
                            -
                            d.AvailableBeds)

                            /

                            d.TotalBeds
                        )
                    ) * 100;


                d.RiskLevel =

                    d.OccupancyRate >= 90
                    &&
                    d.ExpectedDischarges < 2

                    ?

                    "HIGH RISK"

                    :

                    d.OccupancyRate >= 75

                    ?

                    "MODERATE"

                    :

                    "STABLE";
            }

            return depts;
        }

    }
}
