using ISHMS.Core.Constants;
using ISHMS.Core.Constants.Enums;
using ISHMS.Core.Enums;
using ISHMS.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public class HospitalSeeder
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly Random _rng = new(20260526);

    public HospitalSeeder(
        AppDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task SeedAsync()
    {
        await SeedRolesAsync();
        var staff = await SeedUsersAsync();

        if (await _context.Patients.AnyAsync())
            return;

        await EnsureHospitalLayoutAsync();

        var beds = await _context.Beds
            .Include(b => b.Room)
                .ThenInclude(r => r.Department)
            .OrderBy(b => b.Room.Department.Name)
            .ThenBy(b => b.Room.RoomNumber)
            .ThenBy(b => b.Id)
            .ToListAsync();

        foreach (var bed in beds)
        {
            bed.IsOccupied = false;
            bed.PatientId = null;
        }

        var scenarios = BuildPatientScenarios(beds);
        await _context.Patients.AddRangeAsync(scenarios.Select(s => s.Patient));
        await _context.SaveChangesAsync();

        foreach (var scenario in scenarios.Where(s => s.AssignedBed != null))
        {
            scenario.AssignedBed!.IsOccupied = true;
            scenario.AssignedBed.PatientId = scenario.Patient.Id;
        }

        await _context.SaveChangesAsync();

        var vitals = scenarios
            .SelectMany(s => s.Vitals.Select(v => new VitalSign
            {
                PatientId = s.Patient.Id,
                HeartRate = v.HeartRate,
                OxygenLevel = v.OxygenLevel,
                Temperature = v.Temperature,
                SystolicPressure = v.SystolicPressure,
                DiastolicPressure = v.DiastolicPressure,
                RespirationRate = v.RespirationRate,
                RecordedAt = v.RecordedAt
            }))
            .OrderBy(v => v.RecordedAt)
            .ToList();

        await _context.VitalSigns.AddRangeAsync(vitals);
        await _context.SaveChangesAsync();

        var alerts = BuildAlerts(scenarios, staff);
        await _context.Alerts.AddRangeAsync(alerts);
        await _context.SaveChangesAsync();

        var tasks = BuildTasks(scenarios, staff);
        await _context.PatientTasks.AddRangeAsync(tasks);
        await _context.SaveChangesAsync();

        var reports = BuildMedicalReports(scenarios, staff.Doctors);
        await _context.MedicalReports.AddRangeAsync(reports);
        await _context.SaveChangesAsync();
    }

    private async Task SeedRolesAsync()
    {
        foreach (var role in new[] { AppRoles.Admin, AppRoles.Doctor, AppRoles.Nurse, AppRoles.Receptionist })
        {
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    private async Task<StaffSet> SeedUsersAsync()
    {
        var users = new[]
        {
            new SeedUser("System Admin", "admin@ishms.com", "Admin123", AppRoles.Admin),
            new SeedUser("Dr. Laila Mansour", "dr.laila@ishms.com", "Doctor123", AppRoles.Doctor),
            new SeedUser("Dr. Omar Haddad", "dr.omar@ishms.com", "Doctor123", AppRoles.Doctor),
            new SeedUser("Dr. Nadia Karim", "dr.nadia@ishms.com", "Doctor123", AppRoles.Doctor),
            new SeedUser("Dr. Youssef Salem", "dr.youssef@ishms.com", "Doctor123", AppRoles.Doctor),
            new SeedUser("Dr. Mariam Fathy", "dr.mariam@ishms.com", "Doctor123", AppRoles.Doctor),
            new SeedUser("Dr. Karim Nasser", "dr.karim@ishms.com", "Doctor123", AppRoles.Doctor),
            new SeedUser("Nurse Hanan Ali", "nurse.hanan@ishms.com", "Nurse123", AppRoles.Nurse),
            new SeedUser("Nurse Mostafa Zaki", "nurse.mostafa@ishms.com", "Nurse123", AppRoles.Nurse),
            new SeedUser("Nurse Farida Samir", "nurse.farida@ishms.com", "Nurse123", AppRoles.Nurse),
            new SeedUser("Nurse Ahmed Tarek", "nurse.ahmed@ishms.com", "Nurse123", AppRoles.Nurse),
            new SeedUser("Nurse Salma Adel", "nurse.salma@ishms.com", "Nurse123", AppRoles.Nurse),
            new SeedUser("Nurse Mina George", "nurse.mina@ishms.com", "Nurse123", AppRoles.Nurse),
            new SeedUser("Nurse Reem Hassan", "nurse.reem@ishms.com", "Nurse123", AppRoles.Nurse),
            new SeedUser("Nurse Yara Ibrahim", "nurse.yara@ishms.com", "Nurse123", AppRoles.Nurse),
            new SeedUser("Receptionist Dina Farouk", "reception.dina@ishms.com", "Reception123", AppRoles.Receptionist),
            new SeedUser("Receptionist Hossam Amin", "reception.hossam@ishms.com", "Reception123", AppRoles.Receptionist),
            new SeedUser("Receptionist Nour Selim", "reception.nour@ishms.com", "Reception123", AppRoles.Receptionist)
        };

        var result = new List<(ApplicationUser User, string Role)>();

        foreach (var seed in users)
        {
            var user = await _userManager.FindByEmailAsync(seed.Email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    FullName = seed.FullName,
                    Email = seed.Email,
                    UserName = seed.Email,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-30)
                };

                var created = await _userManager.CreateAsync(user, seed.Password);
                if (!created.Succeeded)
                    throw new InvalidOperationException(string.Join("; ", created.Errors.Select(e => e.Description)));
            }

            if (!await _userManager.IsInRoleAsync(user, seed.Role))
                await _userManager.AddToRoleAsync(user, seed.Role);

            result.Add((user, seed.Role));
        }

        return new StaffSet(
            result.Where(x => x.Role == AppRoles.Admin).Select(x => x.User).ToList(),
            result.Where(x => x.Role == AppRoles.Doctor).Select(x => x.User).ToList(),
            result.Where(x => x.Role == AppRoles.Nurse).Select(x => x.User).ToList(),
            result.Where(x => x.Role == AppRoles.Receptionist).Select(x => x.User).ToList());
    }

    private async Task EnsureHospitalLayoutAsync()
    {
        var departmentNames = new[] { "Cardiology", "Neurology", "Emergency", "ICU" };

        foreach (var name in departmentNames)
        {
            if (!await _context.Departments.AnyAsync(d => d.Name == name))
                await _context.Departments.AddAsync(new Department { Name = name });
        }

        await _context.SaveChangesAsync();

        var departments = await _context.Departments
            .Include(d => d.Rooms)
                .ThenInclude(r => r.Beds)
            .Where(d => departmentNames.Contains(d.Name))
            .ToListAsync();

        foreach (var department in departments)
        {
            if (!department.Rooms.Any())
            {
                for (var roomNumber = 1; roomNumber <= 10; roomNumber++)
                {
                    var room = new Room
                    {
                        DepartmentId = department.Id,
                        RoomNumber = $"{department.Name}-{roomNumber}"
                    };

                    for (var bedNumber = 1; bedNumber <= 5; bedNumber++)
                        room.Beds.Add(new Bed { IsOccupied = false });

                    await _context.Rooms.AddAsync(room);
                }

                continue;
            }

            foreach (var room in department.Rooms.Where(r => !r.Beds.Any()))
            {
                for (var bedNumber = 1; bedNumber <= 5; bedNumber++)
                    await _context.Beds.AddAsync(new Bed { RoomId = room.Id, IsOccupied = false });
            }
        }

        await _context.SaveChangesAsync();
    }

    private List<PatientScenario> BuildPatientScenarios(List<Bed> beds)
    {
        var scenarios = new List<PatientScenario>();
        var availableBeds = beds.ToList();

        AddScenarios(scenarios, availableBeds, RiskBand.Low, PatientFlowStatus.ObservationalStable, 12, false);
        AddScenarios(scenarios, availableBeds, RiskBand.Low, PatientFlowStatus.Stable, 8, false);
        AddScenarios(scenarios, availableBeds, RiskBand.Low, PatientFlowStatus.Discharged, 6, false);
        AddScenarios(scenarios, availableBeds, RiskBand.Medium, PatientFlowStatus.UnderObservation, 18, false);
        AddScenarios(scenarios, availableBeds, RiskBand.Medium, PatientFlowStatus.UnderTreatment, 8, false);
        AddScenarios(scenarios, availableBeds, RiskBand.Critical, PatientFlowStatus.WaitingDoctor, 10, true);
        AddScenarios(scenarios, availableBeds, RiskBand.Critical, PatientFlowStatus.UnderTreatment, 3, false);

        return scenarios;
    }

    private void AddScenarios(
        List<PatientScenario> scenarios,
        List<Bed> availableBeds,
        RiskBand riskBand,
        PatientFlowStatus flowStatus,
        int count,
        bool allowDeterioration)
    {
        for (var i = 0; i < count; i++)
        {
            var patientCase = PickCase(riskBand);
            var deteriorated = allowDeterioration && i % 3 == 0;
            var vitals = BuildVitalSeries(riskBand, patientCase, deteriorated, flowStatus);
            var latest = vitals[^1];
            var newsScore = CalculateNews(latest);
            var age = _rng.Next(19, 88);
            var admittedAt = BuildAdmissionTime(flowStatus);
            var assignedBed = flowStatus == PatientFlowStatus.Discharged
                ? null
                : TakeBed(availableBeds, patientCase.DepartmentName);

            var patient = new Patient
            {
                FullName = BuildPatientName(),
                Age = age,
                DateOfBirth = DateTime.Today.AddYears(-age).AddDays(-_rng.Next(0, 365)),
                AdmittedAt = admittedAt,
                CurrentStatus = NewsToPatientStatus(newsScore),
                NewsScore = newsScore,
                FlowStatus = flowStatus,
                Background = BuildBackground(patientCase, riskBand, deteriorated),
                PreviousMedications = patientCase.PreviousMedications,
                CurrentTreatment = BuildCurrentTreatment(patientCase, riskBand, flowStatus),
                BedId = assignedBed?.Id
            };

            scenarios.Add(new PatientScenario(patient, patientCase, riskBand, flowStatus, deteriorated, vitals, assignedBed));
        }
    }

    private DateTime BuildAdmissionTime(PatientFlowStatus flowStatus)
    {
        var preferredHours = new[] { 6, 8, 9, 10, 14, 17, 19, 22 };
        var daysBack = flowStatus == PatientFlowStatus.Discharged
            ? _rng.Next(2, 12)
            : _rng.Next(0, 8);

        var admittedAt = DateTime.UtcNow.Date
            .AddDays(-daysBack)
            .AddHours(preferredHours[_rng.Next(preferredHours.Length)])
            .AddMinutes(_rng.Next(0, 50));

        return admittedAt > DateTime.UtcNow.AddHours(-4)
            ? admittedAt.AddDays(-1)
            : admittedAt;
    }

    private Bed? TakeBed(List<Bed> availableBeds, string departmentName)
    {
        var candidates = availableBeds
            .Where(b => b.Room.Department.Name == departmentName)
            .ToList();

        if (!candidates.Any())
            candidates = availableBeds.ToList();

        if (!candidates.Any())
            return null;

        var bed = candidates[_rng.Next(candidates.Count)];
        availableBeds.Remove(bed);
        return bed;
    }

    private List<VitalReading> BuildVitalSeries(
        RiskBand finalRisk,
        ClinicalCase patientCase,
        bool deteriorated,
        PatientFlowStatus flowStatus)
    {
        var count = _rng.Next(8, 16);
        var end = flowStatus == PatientFlowStatus.Discharged
            ? DateTime.UtcNow.AddHours(-_rng.Next(8, 48)).AddMinutes(-_rng.Next(0, 60))
            : DateTime.UtcNow.AddMinutes(-_rng.Next(5, 90));

        var start = end.AddHours(-_rng.Next(40, 72));
        var intervalMinutes = Math.Max(45, (end - start).TotalMinutes / Math.Max(1, count - 1));
        var vitals = new List<VitalReading>();

        for (var i = 0; i < count; i++)
        {
            var stageRisk = finalRisk;

            if (deteriorated)
            {
                if (i < count / 2)
                    stageRisk = RiskBand.Low;
                else if (i < count - 2)
                    stageRisk = RiskBand.Medium;
            }
            else if (finalRisk == RiskBand.Medium && i < 2)
            {
                stageRisk = RiskBand.Low;
            }
            else if (finalRisk == RiskBand.Critical && i < 2)
            {
                stageRisk = RiskBand.Medium;
            }

            var recordedAt = start.AddMinutes(intervalMinutes * i).AddMinutes(_rng.Next(-8, 9));
            if (i == 0)
                recordedAt = start;
            if (i == count - 1)
                recordedAt = end;

            vitals.Add(GenerateVitalForBand(stageRisk, patientCase, recordedAt));
        }

        return vitals.OrderBy(v => v.RecordedAt).ToList();
    }

    private VitalReading GenerateVitalForBand(RiskBand riskBand, ClinicalCase patientCase, DateTime recordedAt)
    {
        for (var attempt = 0; attempt < 30; attempt++)
        {
            var vital = GenerateVital(riskBand, patientCase, recordedAt);
            if (MatchesRiskBand(CalculateNews(vital), riskBand))
                return vital;
        }

        return riskBand switch
        {
            RiskBand.Low => new VitalReading(82, 98, 36.8, 122, 76, 16, recordedAt),
            RiskBand.Medium => new VitalReading(104, 93, 37.8, 116, 74, 22, recordedAt),
            _ => new VitalReading(132, 90, 39.2, 88, 60, 28, recordedAt)
        };
    }

    private static bool MatchesRiskBand(int newsScore, RiskBand riskBand) =>
        riskBand switch
        {
            RiskBand.Low => newsScore <= 3,
            RiskBand.Medium => newsScore is >= 4 and <= 6,
            _ => newsScore >= 7
        };

    private VitalReading GenerateVital(RiskBand riskBand, ClinicalCase patientCase, DateTime recordedAt)
    {
        var vital = riskBand switch
        {
            RiskBand.Low => new VitalReading(
                HeartRate: Ri(68, 91),
                OxygenLevel: Ri(96, 100),
                Temperature: Rd(36.3, 37.4),
                SystolicPressure: Ri(108, 134),
                DiastolicPressure: Ri(66, 86),
                RespirationRate: Ri(12, 19),
                RecordedAt: recordedAt),

            RiskBand.Medium => new VitalReading(
                HeartRate: Ri(96, 119),
                OxygenLevel: Ri(92, 95),
                Temperature: Rd(37.2, 38.8),
                SystolicPressure: Ri(101, 128),
                DiastolicPressure: Ri(64, 86),
                RespirationRate: Ri(21, 25),
                RecordedAt: recordedAt),

            _ => new VitalReading(
                HeartRate: Ri(116, 142),
                OxygenLevel: Ri(87, 92),
                Temperature: Rd(38.7, 39.8),
                SystolicPressure: Ri(82, 101),
                DiastolicPressure: Ri(50, 76),
                RespirationRate: Ri(25, 33),
                RecordedAt: recordedAt)
        };

        return patientCase.Code switch
        {
            "COPD" or "ASTHMA" => vital with
            {
                OxygenLevel = ClampD(vital.OxygenLevel - (riskBand == RiskBand.Low ? 0 : 2), 82, 100),
                RespirationRate = Clamp(vital.RespirationRate + 2, 10, 36)
            },
            "SEPSIS" => vital with
            {
                Temperature = riskBand == RiskBand.Low ? vital.Temperature : ClampD(vital.Temperature + 0.3, 36.0, 40.2),
                SystolicPressure = riskBand == RiskBand.Critical ? Clamp(vital.SystolicPressure - 6, 75, 240) : vital.SystolicPressure
            },
            "ACS" => vital with
            {
                HeartRate = Clamp(vital.HeartRate + 6, 45, 170),
                SystolicPressure = riskBand == RiskBand.Critical ? Clamp(vital.SystolicPressure + 18, 75, 220) : vital.SystolicPressure
            },
            "STROKE" => vital with
            {
                SystolicPressure = Clamp(vital.SystolicPressure + 18, 90, 220),
                HeartRate = Clamp(vital.HeartRate + 3, 45, 170)
            },
            _ => vital
        };
    }

    private List<Alert> BuildAlerts(List<PatientScenario> scenarios, StaffSet staff)
    {
        var alerts = new List<Alert>();

        foreach (var scenario in scenarios)
        {
            var patient = scenario.Patient;
            var latest = scenario.Vitals[^1];
            var baseTime = latest.RecordedAt.AddMinutes(-_rng.Next(5, 180));

            if (scenario.RiskBand == RiskBand.Critical)
            {
                var repeatedMessage = $"URGENT: NEWS {patient.NewsScore} for {patient.FullName} - immediate doctor review required.";
                var alertCount = _rng.Next(3, 6);

                for (var i = 0; i < alertCount; i++)
                {
                    var message = i < 2
                        ? repeatedMessage
                        : CriticalAlertMessage(patient, latest, scenario.Case);

                    var createdAt = i == alertCount - 1
                        ? DateTime.UtcNow.AddMinutes(-_rng.Next(4, 45))
                        : baseTime.AddMinutes(i * _rng.Next(18, 50));

                    alerts.Add(CreateAlert(
                        patient.Id,
                        AppRoles.Doctor,
                        NextUserId(staff.Doctors),
                        message,
                        AlertSeverity.Critical,
                        createdAt,
                        isRead: i < alertCount - 2 && _rng.Next(100) < 35));
                }

                alerts.Add(CreateAlert(
                    patient.Id,
                    AppRoles.Nurse,
                    NextUserId(staff.Nurses),
                    $"Prepare escalation bundle for {patient.FullName}: latest O2 {latest.OxygenLevel}%, RR {latest.RespirationRate}.",
                    AlertSeverity.Warning,
                    DateTime.UtcNow.AddMinutes(-_rng.Next(10, 90)),
                    isRead: false));
            }
            else if (scenario.RiskBand == RiskBand.Medium)
            {
                var count = _rng.Next(1, 4);
                for (var i = 0; i < count; i++)
                {
                    alerts.Add(CreateAlert(
                        patient.Id,
                        AppRoles.Nurse,
                        NextUserId(staff.Nurses),
                        MediumAlertMessage(patient, latest, scenario.Case),
                        AlertSeverity.Warning,
                        baseTime.AddMinutes(i * _rng.Next(25, 75)),
                        isRead: _rng.Next(100) < 45));
                }
            }
            else
            {
                if (patient.FlowStatus == PatientFlowStatus.ObservationalStable && _rng.Next(100) < 55)
                {
                    alerts.Add(CreateAlert(
                        patient.Id,
                        AppRoles.Doctor,
                        NextUserId(staff.Doctors),
                        $"{patient.FullName} is observationally stable with NEWS {patient.NewsScore}. Review when available.",
                        AlertSeverity.Info,
                        baseTime,
                        isRead: _rng.Next(100) < 60));
                }

                if (patient.FlowStatus == PatientFlowStatus.Stable)
                {
                    alerts.Add(CreateAlert(
                        patient.Id,
                        AppRoles.Receptionist,
                        NextUserId(staff.Receptionists),
                        $"{patient.FullName} is stable and ready for discharge workflow.",
                        AlertSeverity.Info,
                        DateTime.UtcNow.AddMinutes(-_rng.Next(30, 420)),
                        isRead: _rng.Next(100) < 40));
                }
            }
        }

        return alerts;
    }

    private List<PatientTask> BuildTasks(List<PatientScenario> scenarios, StaffSet staff)
    {
        var tasks = new List<PatientTask>();

        foreach (var scenario in scenarios)
        {
            var patient = scenario.Patient;
            var latest = scenario.Vitals[^1];

            switch (patient.FlowStatus)
            {
                case PatientFlowStatus.UnderObservation:
                    tasks.Add(CreateTask(patient.Id, AppRoles.Nurse, NextUserId(staff.Nurses),
                        "Repeat full vital signs",
                        $"NEWS {patient.NewsScore}; repeat observations and document oxygen response for {scenario.Case.Diagnosis}.",
                        PickOpenStatus(), latest.RecordedAt.AddMinutes(15), overdue: _rng.Next(100) < 30));
                    tasks.Add(CreateTask(patient.Id, AppRoles.Nurse, NextUserId(staff.Nurses),
                        "Maintain observation chart",
                        "Continue NEWS monitoring while patient remains under observation.",
                        PatientTaskStatus.Completed, latest.RecordedAt.AddMinutes(-90), overdue: false));
                    break;

                case PatientFlowStatus.ObservationalStable:
                    tasks.Add(CreateTask(patient.Id, AppRoles.Nurse, NextUserId(staff.Nurses),
                        "Routine vital signs check",
                        $"NEWS {patient.NewsScore}; continue routine monitoring and update ISBAR before handover.",
                        _rng.Next(100) < 70 ? PatientTaskStatus.Completed : PatientTaskStatus.Pending,
                        latest.RecordedAt.AddMinutes(-120), overdue: _rng.Next(100) < 15));
                    tasks.Add(CreateTask(patient.Id, AppRoles.Doctor, NextUserId(staff.Doctors),
                        "Review observational stability",
                        "Doctor review needed before final stable or treatment decision.",
                        _rng.Next(100) < 45 ? PatientTaskStatus.Pending : PatientTaskStatus.Completed,
                        latest.RecordedAt.AddMinutes(-60), overdue: _rng.Next(100) < 10));
                    break;

                case PatientFlowStatus.WaitingDoctor:
                    tasks.Add(CreateTask(patient.Id, AppRoles.Nurse, NextUserId(staff.Nurses),
                        "Prepare patient for doctor",
                        $"Critical NEWS {patient.NewsScore}; latest O2 {latest.OxygenLevel}% and RR {latest.RespirationRate}.",
                        PatientTaskStatus.InProgress, latest.RecordedAt.AddMinutes(5), overdue: _rng.Next(100) < 45));
                    tasks.Add(CreateTask(patient.Id, AppRoles.Doctor, NextUserId(staff.Doctors),
                        "Immediate doctor assessment",
                        $"Doctor must decide Stable or UnderTreatment for {scenario.Case.Diagnosis}.",
                        PatientTaskStatus.Pending, latest.RecordedAt.AddMinutes(10), overdue: _rng.Next(100) < 55));
                    tasks.Add(CreateTask(patient.Id, AppRoles.Nurse, NextUserId(staff.Nurses),
                        "Start escalation preparation",
                        "Attach monitor, prepare IV access, and keep escalation trolley nearby.",
                        PatientTaskStatus.Completed, latest.RecordedAt.AddMinutes(-45), overdue: false));
                    break;

                case PatientFlowStatus.UnderTreatment:
                    tasks.Add(CreateTask(patient.Id, AppRoles.Nurse, NextUserId(staff.Nurses),
                        "Administer prescribed treatment",
                        scenario.Patient.CurrentTreatment ?? "Administer active treatment plan.",
                        PickMixedStatus(), latest.RecordedAt.AddMinutes(-30), overdue: _rng.Next(100) < 25));
                    tasks.Add(CreateTask(patient.Id, AppRoles.Doctor, NextUserId(staff.Doctors),
                        "Reassess treatment response",
                        $"Review NEWS trend for {scenario.Case.Diagnosis} and decide if patient can move to Stable.",
                        PickOpenStatus(), latest.RecordedAt.AddMinutes(20), overdue: _rng.Next(100) < 25));
                    break;

                case PatientFlowStatus.Stable:
                    tasks.Add(CreateTask(patient.Id, AppRoles.Nurse, NextUserId(staff.Nurses),
                        "Final vital signs check",
                        "Confirm NEWS remains low before discharge paperwork.",
                        PatientTaskStatus.Completed, latest.RecordedAt.AddMinutes(-90), overdue: false));
                    tasks.Add(CreateTask(patient.Id, AppRoles.Receptionist, NextUserId(staff.Receptionists),
                        "Complete discharge paperwork",
                        "Prepare discharge file and verify patient contact details.",
                        PickOpenStatus(), latest.RecordedAt.AddMinutes(30), overdue: _rng.Next(100) < 35));
                    break;

                case PatientFlowStatus.Discharged:
                    tasks.Add(CreateTask(patient.Id, AppRoles.Nurse, NextUserId(staff.Nurses),
                        "Complete final nursing notes",
                        "Final observations documented before bed release.",
                        PatientTaskStatus.Completed, latest.RecordedAt.AddMinutes(-120), overdue: false));
                    tasks.Add(CreateTask(patient.Id, AppRoles.Receptionist, NextUserId(staff.Receptionists),
                        "Archive discharge file",
                        "Discharge workflow completed and bed released.",
                        PatientTaskStatus.Completed, latest.RecordedAt.AddMinutes(-60), overdue: false));
                    break;
            }
        }

        return tasks;
    }

    private List<MedicalReport> BuildMedicalReports(List<PatientScenario> scenarios, List<ApplicationUser> doctors)
    {
        var reports = new List<MedicalReport>();
        if (!doctors.Any())
            return reports;

        foreach (var scenario in scenarios)
        {
            var patient = scenario.Patient;
            var latest = scenario.Vitals[^1];
            var doctor = doctors[_rng.Next(doctors.Count)];
            var firstReportTime = scenario.Vitals[Math.Min(2, scenario.Vitals.Count - 1)].RecordedAt.AddMinutes(20);

            reports.Add(new MedicalReport
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                Diagnosis = scenario.Case.Diagnosis,
                TreatmentPlan = BuildReportPlan(scenario, latest),
                ReportType = MedicalReportType.TreatmentPlan,
                CreatedAt = firstReportTime
            });

            if (patient.FlowStatus == PatientFlowStatus.UnderTreatment ||
                patient.FlowStatus == PatientFlowStatus.WaitingDoctor ||
                scenario.RiskBand == RiskBand.Critical)
            {
                reports.Add(new MedicalReport
                {
                    PatientId = patient.Id,
                    DoctorId = doctors[_rng.Next(doctors.Count)].Id,
                    Diagnosis = scenario.Case.Diagnosis,
                    TreatmentPlan = BuildProgressPlan(scenario, latest),
                    ReportType = MedicalReportType.TreatmentPlan,
                    CreatedAt = latest.RecordedAt.AddMinutes(-_rng.Next(30, 180))
                });
            }

            if (patient.FlowStatus == PatientFlowStatus.Stable ||
                patient.FlowStatus == PatientFlowStatus.Discharged)
            {
                reports.Add(new MedicalReport
                {
                    PatientId = patient.Id,
                    DoctorId = doctors[_rng.Next(doctors.Count)].Id,
                    Diagnosis = scenario.Case.Diagnosis,
                    TreatmentPlan = $"NEWS {patient.NewsScore}; symptoms controlled. Discharge education, medication reconciliation, and follow-up arranged.",
                    ReportType = MedicalReportType.DischargeReport,
                    CreatedAt = latest.RecordedAt.AddMinutes(_rng.Next(20, 120))
                });
            }
        }

        return reports
            .OrderBy(r => r.CreatedAt)
            .ToList();
    }

    private PatientTask CreateTask(
        int patientId,
        string role,
        string? userId,
        string title,
        string description,
        PatientTaskStatus status,
        DateTime createdAt,
        bool overdue)
    {
        var dueAt = overdue
            ? DateTime.UtcNow.AddMinutes(-_rng.Next(20, 240))
            : createdAt.AddMinutes(_rng.Next(45, 240));

        if (dueAt <= createdAt)
            createdAt = dueAt.AddMinutes(-_rng.Next(40, 180));

        DateTime? completedAt = null;
        if (status == PatientTaskStatus.Completed)
        {
            var completionWindow = Math.Max(10, (int)(dueAt - createdAt).TotalMinutes - 5);
            completedAt = createdAt.AddMinutes(_rng.Next(8, completionWindow + 1));
            if (completedAt > dueAt)
                completedAt = dueAt.AddMinutes(-1);
        }

        return new PatientTask
        {
            PatientId = patientId,
            AssignedToRole = role,
            AssignedToUserId = userId,
            Title = title,
            Description = description,
            Status = status,
            CreatedAt = createdAt,
            DueAt = dueAt,
            CompletedAt = completedAt
        };
    }

    private Alert CreateAlert(
        int patientId,
        string role,
        string? userId,
        string message,
        AlertSeverity severity,
        DateTime createdAt,
        bool isRead)
    {
        return new Alert
        {
            PatientId = patientId,
            TargetRole = role,
            TargetUserId = userId,
            Message = message,
            Severity = severity,
            CreatedAt = createdAt,
            IsRead = isRead
        };
    }

    private string BuildBackground(ClinicalCase patientCase, RiskBand riskBand, bool deteriorated)
    {
        var acuity = riskBand switch
        {
            RiskBand.Low => "low-risk observation",
            RiskBand.Medium => "moderate NEWS under close observation",
            _ => "critical deterioration requiring escalation"
        };

        var course = deteriorated
            ? " The patient was previously stable but deteriorated during the current observation window."
            : string.Empty;

        return $"{patientCase.Background} Current admission is for {acuity}.{course}";
    }

    private string BuildCurrentTreatment(ClinicalCase patientCase, RiskBand riskBand, PatientFlowStatus flowStatus)
    {
        if (flowStatus == PatientFlowStatus.ObservationalStable)
            return "Routine monitoring, oral hydration, and repeat NEWS before doctor review.";

        if (flowStatus == PatientFlowStatus.Stable || flowStatus == PatientFlowStatus.Discharged)
            return "Symptoms controlled; continue home medication plan and follow-up instructions.";

        return riskBand switch
        {
            RiskBand.Low => "Routine monitoring and comfort measures.",
            RiskBand.Medium => patientCase.MediumTreatment,
            _ => patientCase.CriticalTreatment
        };
    }

    private string BuildReportPlan(PatientScenario scenario, VitalReading latest)
    {
        return scenario.RiskBand switch
        {
            RiskBand.Low =>
                $"NEWS {scenario.Patient.NewsScore}; vitals stable (O2 {latest.OxygenLevel}%, HR {latest.HeartRate}). Continue observation and review for Stable decision.",
            RiskBand.Medium =>
                $"NEWS {scenario.Patient.NewsScore}; continue close observation, repeat vitals, and maintain {scenario.Case.MediumTreatment}.",
            _ =>
                $"NEWS {scenario.Patient.NewsScore}; urgent senior review required. Prepare for treatment escalation for {scenario.Case.Diagnosis}."
        };
    }

    private string BuildProgressPlan(PatientScenario scenario, VitalReading latest)
    {
        var prefix = scenario.Deteriorated
            ? "Deterioration noted after prior stable observations."
            : "Ongoing high-risk review.";

        return $"{prefix} Latest vitals: O2 {latest.OxygenLevel}%, HR {latest.HeartRate}, BP {latest.SystolicPressure}/{latest.DiastolicPressure}, RR {latest.RespirationRate}. {scenario.Case.CriticalTreatment}";
    }

    private string CriticalAlertMessage(Patient patient, VitalReading latest, ClinicalCase patientCase)
    {
        return _rng.Next(4) switch
        {
            0 => $"O2 saturation {latest.OxygenLevel}% in {patientCase.Diagnosis}; escalation required.",
            1 => $"Respiratory rate {latest.RespirationRate}/min with NEWS {patient.NewsScore}; doctor review overdue.",
            2 => $"Heart rate {latest.HeartRate}/min with unstable observations; prepare immediate assessment.",
            _ => $"Blood pressure {latest.SystolicPressure}/{latest.DiastolicPressure} and NEWS {patient.NewsScore}; urgent clinical decision needed."
        };
    }

    private string MediumAlertMessage(Patient patient, VitalReading latest, ClinicalCase patientCase)
    {
        return _rng.Next(3) switch
        {
            0 => $"NEWS {patient.NewsScore} for {patientCase.Diagnosis}; continue close observation.",
            1 => $"O2 {latest.OxygenLevel}% and RR {latest.RespirationRate}; repeat vital signs within the hour.",
            _ => $"Moderate deterioration risk for {patient.FullName}; update ISBAR if trend worsens."
        };
    }

    private ClinicalCase PickCase(RiskBand riskBand)
    {
        var cases = ClinicalCases
            .Where(c => c.DefaultRisk == riskBand || _rng.Next(100) < 35)
            .ToList();

        return cases[_rng.Next(cases.Count)];
    }

    private PatientTaskStatus PickOpenStatus() =>
        _rng.Next(100) < 55 ? PatientTaskStatus.Pending : PatientTaskStatus.InProgress;

    private PatientTaskStatus PickMixedStatus()
    {
        var roll = _rng.Next(100);
        if (roll < 35) return PatientTaskStatus.Completed;
        if (roll < 70) return PatientTaskStatus.InProgress;
        return PatientTaskStatus.Pending;
    }

    private string BuildPatientName()
    {
        var firstNames = new[]
        {
            "Amina", "Omar", "Youssef", "Mariam", "Hassan", "Farida", "Nour", "Karim",
            "Salma", "Tarek", "Dina", "Mostafa", "Rania", "Ahmed", "Laila", "Mona",
            "Ibrahim", "Nada", "Hossam", "Aya", "Khaled", "Reem", "Samir", "Yara"
        };

        var lastNames = new[]
        {
            "Hassan", "Mansour", "El-Sayed", "Fathy", "Nasser", "Farouk", "Ibrahim",
            "Salem", "Zaki", "Karim", "Adel", "Amin", "Mahmoud", "Tawfik"
        };

        return $"{firstNames[_rng.Next(firstNames.Length)]} {lastNames[_rng.Next(lastNames.Length)]}";
    }

    private string? NextUserId(List<ApplicationUser> users)
    {
        if (!users.Any())
            return null;

        var user = users[_assignmentCursor % users.Count];
        _assignmentCursor++;
        return user.Id;
    }

    private int CalculateNews(VitalReading vital)
    {
        var score = 0;

        if (vital.OxygenLevel <= 91) score += 3;
        else if (vital.OxygenLevel <= 93) score += 2;
        else if (vital.OxygenLevel <= 95) score += 1;

        if (vital.HeartRate >= 131) score += 3;
        else if (vital.HeartRate >= 111) score += 2;
        else if (vital.HeartRate >= 91) score += 1;

        if (vital.Temperature >= 39) score += 2;
        else if (vital.Temperature <= 35) score += 3;

        if (vital.SystolicPressure <= 90) score += 3;
        else if (vital.SystolicPressure <= 100) score += 2;

        if (vital.RespirationRate >= 25) score += 3;
        else if (vital.RespirationRate >= 21) score += 2;

        return score;
    }

    private static PatientStatus NewsToPatientStatus(int newsScore) =>
        newsScore >= 7 ? PatientStatus.Critical :
        newsScore >= 4 ? PatientStatus.Unstable :
        PatientStatus.Stable;

    private int Ri(int minInclusive, int maxExclusive) => _rng.Next(minInclusive, maxExclusive);

    private double Rd(double minInclusive, double maxExclusive) =>
        Math.Round(minInclusive + _rng.NextDouble() * (maxExclusive - minInclusive), 1);

    private static int Clamp(int value, int min, int max) =>
        Math.Max(min, Math.Min(max, value));

    private static double ClampD(double value, double min, double max) =>
        Math.Round(Math.Max(min, Math.Min(max, value)), 1);

    private int _assignmentCursor;

    private static readonly ClinicalCase[] ClinicalCases =
    {
        new(
            Code: "PNEUMONIA",
            Diagnosis: "Community-acquired pneumonia",
            DepartmentName: "Emergency",
            Background: "Cough, fever, pleuritic chest discomfort, and reduced oral intake.",
            PreviousMedications: "Paracetamol 1g, Salbutamol inhaler",
            MediumTreatment: "IV ceftriaxone, oral azithromycin, fluids, and oxygen as needed.",
            CriticalTreatment: "High-flow oxygen, broad-spectrum IV antibiotics, blood cultures, and senior review.",
            DefaultRisk: RiskBand.Medium),
        new(
            Code: "COPD",
            Diagnosis: "COPD exacerbation",
            DepartmentName: "Emergency",
            Background: "Known COPD with increasing dyspnea, wheeze, and productive cough.",
            PreviousMedications: "Tiotropium inhaler, Salbutamol inhaler, Prednisolone 5mg",
            MediumTreatment: "Nebulized bronchodilators, controlled oxygen, steroids, and sputum culture.",
            CriticalTreatment: "Controlled oxygen target 88-92%, nebulizers, IV steroids, ABG, and NIV assessment.",
            DefaultRisk: RiskBand.Medium),
        new(
            Code: "ACS",
            Diagnosis: "Acute coronary syndrome",
            DepartmentName: "Cardiology",
            Background: "Central chest pain with diaphoresis and cardiovascular risk factors.",
            PreviousMedications: "Aspirin 100mg, Atorvastatin 40mg, Bisoprolol 2.5mg",
            MediumTreatment: "ECG monitoring, serial troponin, aspirin, statin, and cardiology review.",
            CriticalTreatment: "Cardiac monitor, dual antiplatelet therapy, anticoagulation, and urgent cardiology decision.",
            DefaultRisk: RiskBand.Critical),
        new(
            Code: "SEPSIS",
            Diagnosis: "Sepsis secondary to urinary infection",
            DepartmentName: "ICU",
            Background: "Fever, rigors, confusion, dysuria, and poor urine output.",
            PreviousMedications: "Metformin 500mg, Lisinopril 10mg",
            MediumTreatment: "Sepsis screening, IV fluids, urine culture, and IV antibiotics.",
            CriticalTreatment: "Sepsis six bundle, fluid resuscitation, lactate monitoring, vasopressor readiness, and ICU review.",
            DefaultRisk: RiskBand.Critical),
        new(
            Code: "STROKE",
            Diagnosis: "Acute ischemic stroke observation",
            DepartmentName: "Neurology",
            Background: "Sudden unilateral weakness with slurred speech, now under neurological observation.",
            PreviousMedications: "Amlodipine 5mg, Clopidogrel 75mg",
            MediumTreatment: "Neurological observations, swallow screen, CT review, and BP monitoring.",
            CriticalTreatment: "Urgent neurological review, airway monitoring, CT angiography pathway, and escalation plan.",
            DefaultRisk: RiskBand.Medium),
        new(
            Code: "HF",
            Diagnosis: "Acute decompensated heart failure",
            DepartmentName: "Cardiology",
            Background: "Orthopnea, ankle swelling, basal crackles, and reduced exercise tolerance.",
            PreviousMedications: "Furosemide 40mg, Ramipril 5mg, Spironolactone 25mg",
            MediumTreatment: "IV diuretics, fluid balance chart, oxygen, and renal function monitoring.",
            CriticalTreatment: "High-flow oxygen, IV diuretics, cardiac monitoring, and urgent cardiology review.",
            DefaultRisk: RiskBand.Medium),
        new(
            Code: "DKA",
            Diagnosis: "Diabetic ketoacidosis",
            DepartmentName: "ICU",
            Background: "Vomiting, dehydration, abdominal pain, and high capillary glucose.",
            PreviousMedications: "Insulin glargine 24 units, Metformin 500mg",
            MediumTreatment: "IV fluids, fixed-rate insulin infusion, potassium replacement, and hourly glucose checks.",
            CriticalTreatment: "DKA protocol, continuous monitoring, electrolyte correction, and ICU-level review.",
            DefaultRisk: RiskBand.Critical),
        new(
            Code: "POSTOP",
            Diagnosis: "Post-operative recovery observation",
            DepartmentName: "Emergency",
            Background: "Post-operative monitoring after abdominal surgery with controlled pain.",
            PreviousMedications: "Omeprazole 20mg, Paracetamol 1g",
            MediumTreatment: "Analgesia, wound review, mobilization support, and routine observations.",
            CriticalTreatment: "Senior surgical review, IV fluids, sepsis screen, and urgent imaging if indicated.",
            DefaultRisk: RiskBand.Low),
        new(
            Code: "ASTHMA",
            Diagnosis: "Asthma exacerbation",
            DepartmentName: "Emergency",
            Background: "Wheeze, cough, and shortness of breath after viral illness.",
            PreviousMedications: "Budesonide inhaler, Salbutamol inhaler",
            MediumTreatment: "Nebulized salbutamol, oral steroids, oxygen if needed, and peak-flow monitoring.",
            CriticalTreatment: "Back-to-back nebulizers, IV magnesium consideration, oxygen, and senior respiratory review.",
            DefaultRisk: RiskBand.Medium),
        new(
            Code: "GI",
            Diagnosis: "Upper gastrointestinal bleeding",
            DepartmentName: "ICU",
            Background: "Melena, dizziness, epigastric pain, and anticoagulant exposure.",
            PreviousMedications: "Warfarin 5mg, Omeprazole 20mg",
            MediumTreatment: "IV proton pump inhibitor, group and save, fluid resuscitation, and hemoglobin monitoring.",
            CriticalTreatment: "Major bleed protocol readiness, IV access, transfusion planning, and urgent endoscopy referral.",
            DefaultRisk: RiskBand.Critical)
    };

    private sealed record SeedUser(string FullName, string Email, string Password, string Role);

    private sealed record StaffSet(
        List<ApplicationUser> Admins,
        List<ApplicationUser> Doctors,
        List<ApplicationUser> Nurses,
        List<ApplicationUser> Receptionists);

    private sealed record ClinicalCase(
        string Code,
        string Diagnosis,
        string DepartmentName,
        string Background,
        string PreviousMedications,
        string MediumTreatment,
        string CriticalTreatment,
        RiskBand DefaultRisk);

    private sealed record PatientScenario(
        Patient Patient,
        ClinicalCase Case,
        RiskBand RiskBand,
        PatientFlowStatus IntendedFlowStatus,
        bool Deteriorated,
        List<VitalReading> Vitals,
        Bed? AssignedBed);

    private sealed record VitalReading(
        int HeartRate,
        double OxygenLevel,
        double Temperature,
        int SystolicPressure,
        int DiastolicPressure,
        int RespirationRate,
        DateTime RecordedAt);

    private enum RiskBand
    {
        Low,
        Medium,
        Critical
    }
}
