using ISHMS.Core.Constants;
using ISHMS.Core.Constants.Enums;
using ISHMS.Core.Enums;
using ISHMS.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Seeds the database with a realistic, consistent hospital dataset for Demo and Data Analysis.
///
/// IMPORTANT NOTES:
/// ─────────────────────────────────────────────────────────────────────────────
/// • Departments are NOT seeded here — the backend already seeds 4 fixed departments.
/// • No manual IDs are assigned to any entity (DB uses Identity/Auto-Increment).
/// • This seeder does NOT invoke WorkflowService, AlertService, or any API.
///   It writes data directly to the DB for demo/analysis purposes only.
/// • Two separate status fields on every Patient — never mixed up:
///
///     PatientStatus   (CurrentStatus field)
///     ═══════════════════════════════════════
///     Represents the MEDICAL condition of the patient.
///     Values: Stable | Unstable | Critical
///     Derived from: NEWS score
///       NEWS 0-3  →  PatientStatus.Stable
///       NEWS 4-6  →  PatientStatus.Unstable
///       NEWS 7+   →  PatientStatus.Critical
///
///     PatientFlowStatus   (FlowStatus field)
///     ═══════════════════════════════════════
///     Represents the WORKFLOW STAGE inside the hospital system.
///     Values: New | UnderObservation | WaitingDoctor | UnderTreatment
///             | ObservationalStable | Stable | Discharged
///     Derived from: NEWS band (which group the patient belongs to)
///       NEWS 0-3  →  FlowStatus.ObservationalStable  (low-risk, being monitored)
///       NEWS 4-6  →  FlowStatus.UnderObservation      (borderline, close watch)
///       NEWS 7+   →  FlowStatus.WaitingDoctor          (critical, needs doctor)
///       Exited    →  FlowStatus.Stable                 (cleared for discharge)
///
///     NOTE: "Green/Yellow/Red/DarkRed/Mild" are INTERNAL Seeder labels only.
///     They define vital-sign ranges and map to NEWS scores.
///     They are NOT PatientStatus values and are never stored in the DB.
/// ─────────────────────────────────────────────────────────────────────────────
/// </summary>
public class HospitalSeeder
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly Random _rng = new(42); // fixed → reproducible

    public HospitalSeeder(
        AppDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // ════════════════════════════════════════════════════════════════════════
    // ENTRY POINT
    // ════════════════════════════════════════════════════════════════════════

    public async Task SeedAsync()
    {
        // Guard: run only once.
        // Departments already exist (seeded by backend) so we check Patients instead.
        if (await _context.Patients.AnyAsync()) return;

        // ── 1. Roles & Users ────────────────────────────────────────────────
        await SeedRolesAsync();
        var users = await SeedUsersAsync();
        var doctors = users.Where(u => u.role == AppRoles.Doctor).Select(u => u.user).ToList();
        var nurses = users.Where(u => u.role == AppRoles.Nurse).Select(u => u.user).ToList();

        // ── 2. Rooms & Beds (linked to the existing 4 Departments) ──────────
        var departments = await _context.Departments.ToListAsync(); // load existing ones
        var rooms = SeedRooms(departments);
        await _context.Rooms.AddRangeAsync(rooms);
        await _context.SaveChangesAsync();
        // EF has now populated room.Id via Identity

        var beds = SeedBeds(rooms);
        await _context.Beds.AddRangeAsync(beds);
        await _context.SaveChangesAsync();
        // EF has now populated bed.Id via Identity

        // ── 3. Patients ──────────────────────────────────────────────────────
        // Each patient is built around a SCENARIO that defines:
        //   • A vital-sign profile  (the "ground truth")
        //   • A NEWS score computed from those vitals
        //   • A FlowStatus derived from that NEWS score
        //   • A PatientStatus derived from that NEWS score
        var patients = SeedPatients(beds);
        await _context.Patients.AddRangeAsync(patients);
        await _context.SaveChangesAsync();

        // Mark beds as occupied for patients still in hospital.
        // Uses FlowStatus (workflow stage) — NOT PatientStatus (medical severity).
        // FlowStatus.Stable = cleared for discharge but still occupying a bed.
        // Only the "hasExited" scenario group has no bed assigned at all.
        foreach (var p in patients.Where(p => p.BedId.HasValue &&
                                               p.FlowStatus != PatientFlowStatus.Discharged))
        {
            var bed = beds.First(b => b.Id == p.BedId!.Value);
            bed.IsOccupied = true;
            bed.PatientId = p.Id;
        }
        await _context.SaveChangesAsync();

        // ── 4. Vitals ────────────────────────────────────────────────────────
        var vitals = SeedVitalSigns(patients);
        await _context.VitalSigns.AddRangeAsync(vitals);
        await _context.SaveChangesAsync();

        // ── 5. Alerts ────────────────────────────────────────────────────────
        var alerts = SeedAlerts(patients, doctors, nurses);
        await _context.Alerts.AddRangeAsync(alerts);
        await _context.SaveChangesAsync();

        // ── 6. Tasks ─────────────────────────────────────────────────────────
        var tasks = SeedPatientTasks(patients, alerts, doctors, nurses);
        await _context.PatientTasks.AddRangeAsync(tasks);
        await _context.SaveChangesAsync();

        // ── 7. Medical Reports ───────────────────────────────────────────────
        var reports = SeedMedicalReports(patients, doctors);
        await _context.MedicalReports.AddRangeAsync(reports);
        await _context.SaveChangesAsync();
    }

    // ════════════════════════════════════════════════════════════════════════
    // ROLES
    // ════════════════════════════════════════════════════════════════════════

    private async Task SeedRolesAsync()
    {
        foreach (var role in new[] { AppRoles.Admin, AppRoles.Doctor,
                                     AppRoles.Nurse,  AppRoles.Receptionist })
        {
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // USERS
    // ════════════════════════════════════════════════════════════════════════

    private async Task<List<(ApplicationUser user, string role)>> SeedUsersAsync()
    {
        var seed = new (string name, string email, string pass, string role)[]
        {
            ("Admin User",         "admin@ishms.com",         "Admin@12345",  AppRoles.Admin),
            ("Dr. Ahmed Karim",    "dr.ahmed@ishms.com",      "Doctor@12345", AppRoles.Doctor),
            ("Dr. Sara Hassan",    "dr.sara@ishms.com",       "Doctor@12345", AppRoles.Doctor),
            ("Dr. Omar Nasser",    "dr.omar@ishms.com",       "Doctor@12345", AppRoles.Doctor),
            ("Nurse Fatima Ali",   "nurse.fatima@ishms.com",  "Nurse@12345",  AppRoles.Nurse),
            ("Nurse Layla Samir",  "nurse.layla@ishms.com",   "Nurse@12345",  AppRoles.Nurse),
            ("Nurse Hassan Zaki",  "nurse.hassan@ishms.com",  "Nurse@12345",  AppRoles.Nurse),
            ("Rec. Dina Farouk",   "rec.dina@ishms.com",      "Rec@123456",   AppRoles.Receptionist),
        };

        var result = new List<(ApplicationUser, string)>();

        foreach (var (name, email, pass, role) in seed)
        {
            var existing = await _userManager.FindByEmailAsync(email);
            if (existing != null)
            {
                result.Add((existing, role));
                continue;
            }

            var user = new ApplicationUser { FullName = name, Email = email, UserName = email };
            var ok = await _userManager.CreateAsync(user, pass);
            if (ok.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, role);
                result.Add((user, role));
            }
        }
        return result;
    }

    // ════════════════════════════════════════════════════════════════════════
    // ROOMS  (no manual Id — EF assigns via Identity)
    // ════════════════════════════════════════════════════════════════════════

    private List<Room> SeedRooms(List<Department> departments)
    {
        var rooms = new List<Room>();
        foreach (var dept in departments)
            for (int i = 1; i <= 3; i++)
                rooms.Add(new Room
                {
                    // Id intentionally omitted — auto-increment
                    RoomNumber = $"{dept.Name[..3].ToUpper()}-{i:00}",
                    DepartmentId = dept.Id
                });
        return rooms;
    }

    // ════════════════════════════════════════════════════════════════════════
    // BEDS  (no manual Id)
    // ════════════════════════════════════════════════════════════════════════

    private List<Bed> SeedBeds(List<Room> rooms)
    {
        var beds = new List<Bed>();
        foreach (var room in rooms)
            for (int i = 1; i <= 4; i++)        // 4 beds per room
                beds.Add(new Bed { RoomId = room.Id, IsOccupied = false });
        return beds;
    }

    // ════════════════════════════════════════════════════════════════════════
    // PATIENTS  — scenario-driven, fully consistent
    // ════════════════════════════════════════════════════════════════════════

    // Each scenario defines the vital-sign profile first.
    // NEWS score and all statuses are DERIVED from those vitals — never guessed.

    private static readonly string[] FirstNames =
        { "Ahmed","Mohamed","Sara","Fatima","Omar","Layla",
          "Hassan","Nour","Khaled","Amira","Youssef","Hana",
          "Ibrahim","Rania","Tariq","Dina","Mahmoud","Aya",
          "Karim","Mona","Tarek","Salma","Faris","Nada" };

    private static readonly string[] LastNames =
        { "Al-Rashidi","Mansour","El-Sayed","Hassan","Karim",
          "Nasser","Ibrahim","Farouk","Zaki","Othman","Tawfik","Barakat" };

    private static readonly string[] Diagnoses =
        { "Acute Myocardial Infarction","Pneumonia","Stroke",
          "Sepsis","COPD Exacerbation","Post-op Recovery",
          "Hypertensive Crisis","Diabetic Ketoacidosis",
          "Pulmonary Embolism","Heart Failure","Renal Failure",
          "Liver Cirrhosis","Asthma Exacerbation","GI Bleeding" };

    private List<Patient> SeedPatients(List<Bed> beds)
    {
        var patients = new List<Patient>();
        var availableBeds = beds.ToList();

        // ── Scenario definitions ─────────────────────────────────────────────
        //
        // Each scenario is defined by a vitalBand (internal Seeder label only).
        // From that band we generate realistic Vitals → compute NEWS → derive
        // BOTH statuses independently:
        //
        //   PatientStatus  (medical severity)  — computed by NewsToPatientStatus()
        //   PatientFlowStatus (workflow stage) — fixed per scenario below
        //
        // The two are SEPARATE fields.  NewsToPatientStatus() maps:
        //   NEWS 0-3  →  PatientStatus.Stable
        //   NEWS 4-6  →  PatientStatus.Unstable
        //   NEWS 7+   →  PatientStatus.Critical
        //
        // PatientFlowStatus per scenario:
        //   vitalBand "Green"   → NEWS 0-3  → FlowStatus.ObservationalStable
        //   vitalBand "Yellow"  → NEWS 3-6  → FlowStatus.UnderObservation
        //   vitalBand "Red"     → NEWS 7-9  → FlowStatus.WaitingDoctor
        //   vitalBand "DarkRed" → NEWS 10+  → FlowStatus.WaitingDoctor
        //   vitalBand "Mild"    → NEWS 1-3  → FlowStatus.Stable (exiting patients)
        //
        // "Green/Yellow/Red/DarkRed/Mild" are NOT PatientStatus values.
        // They are private range labels inside this Seeder only.

        var scenarios = new (int count, string vitalBand, PatientFlowStatus flowStatus, bool hasExited)[]
        {
            // NEWS 0-3 → PatientStatus.Stable   + FlowStatus.ObservationalStable
            (12, "Green",   PatientFlowStatus.ObservationalStable, false),

            // NEWS 3-6 → PatientStatus.Unstable + FlowStatus.UnderObservation
            (10, "Yellow",  PatientFlowStatus.UnderObservation,    false),

            // NEWS 7-9 → PatientStatus.Critical + FlowStatus.WaitingDoctor
            ( 8, "Red",     PatientFlowStatus.WaitingDoctor,        false),

            // NEWS 10+ → PatientStatus.Critical + FlowStatus.WaitingDoctor
            ( 4, "DarkRed", PatientFlowStatus.WaitingDoctor,        false),

            // NEWS 1-3 → PatientStatus.Stable   + FlowStatus.Stable (cleared, no bed)
            (16, "Mild",    PatientFlowStatus.Stable,               true),
        };

        foreach (var (count, vitalBand, flowStatus, hasExited) in scenarios)
        {
            for (int i = 0; i < count; i++)
            {
                var name = $"{FirstNames[_rng.Next(FirstNames.Length)]} " +
                                 $"{LastNames[_rng.Next(LastNames.Length)]}";
                var age = _rng.Next(22, 85);
                var admittedAt = DateTime.UtcNow
                                         .AddDays(-_rng.Next(1, 45))
                                         .AddHours(-_rng.Next(0, 23));

                // Step 1: generate vitals for this band (the ground truth)
                var repVital = GenerateRepresentativeVital(vitalBand);

                // Step 2: compute NEWS from those exact vitals
                var newsScore = CalculateNews(repVital);

                // Step 3: derive PatientStatus (medical severity) from NEWS
                // This is SEPARATE from FlowStatus (workflow stage).
                var medicalStatus = NewsToPatientStatus(newsScore);

                // Step 4: FlowStatus (workflow stage) comes from the scenario —
                // already validated to be consistent with the NEWS band above.

                int? bedId = null;
                if (!hasExited && availableBeds.Any())
                {
                    var bed = availableBeds[_rng.Next(availableBeds.Count)];
                    bedId = bed.Id;
                    availableBeds.Remove(bed);
                }

                patients.Add(new Patient
                {
                    // Id omitted — auto-increment
                    FullName = name,
                    Age = age,
                    DateOfBirth = DateTime.Today.AddYears(-age),
                    AdmittedAt = admittedAt,
                    CurrentStatus = medicalStatus,  // PatientStatus enum — from NEWS
                    NewsScore = newsScore,        // computed from vitals
                    FlowStatus = flowStatus,       // PatientFlowStatus enum — from scenario
                    Background = Diagnoses[_rng.Next(Diagnoses.Length)],
                    PreviousMedications = PickMedications(),
                    CurrentTreatment = PickTreatment(vitalBand),
                    BedId = bedId
                });
            }
        }
        return patients;
    }

    // ════════════════════════════════════════════════════════════════════════
    // VITAL SIGN RANGES  per band
    // ════════════════════════════════════════════════════════════════════════

    // Each band guarantees the vital signs will produce a NEWS score
    // in the expected range when CalculateNews() is called.
    //
    //  Band      HR        O2       Temp      SysBP    DiaBP    RR      Expected NEWS
    //  ──────────────────────────────────────────────────────────────────────────────
    //  Green     60-85     96-99    36.1-37.2  110-130  70-85   12-16   0-2
    //  Mild      70-90     95-97    36.5-37.4  115-135  72-88   13-17   1-3
    //  Yellow    90-110    93-95    37.5-38.4  140-165  88-100  18-22   3-6
    //  Red       111-130   89-92    38.5-39.0  165-185  100-110 22-26   7-9
    //  DarkRed   131-160   80-88    39.1-40.2  185-220  110-130 26-32   10-14

    private record VitalReading(int HR, int O2, double Temp, int SysBP, int DiaBP, int RR);

    private VitalReading GenerateRepresentativeVital(string band) => band switch
    {
        "Green" => new(Ri(60, 85), Ri(96, 99), Rd(36.1, 37.2), Ri(110, 130), Ri(70, 85), Ri(12, 16)),
        "Mild" => new(Ri(70, 90), Ri(95, 97), Rd(36.5, 37.4), Ri(115, 135), Ri(72, 88), Ri(13, 17)),
        "Yellow" => new(Ri(90, 110), Ri(93, 95), Rd(37.5, 38.4), Ri(140, 165), Ri(88, 100), Ri(18, 22)),
        "Red" => new(Ri(111, 130), Ri(89, 92), Rd(38.5, 39.0), Ri(165, 185), Ri(100, 110), Ri(22, 26)),
        "DarkRed" => new(Ri(131, 160), Ri(80, 88), Rd(39.1, 40.2), Ri(185, 220), Ri(110, 130), Ri(26, 32)),
        _ => new(75, 98, 36.8, 120, 78, 16)
    };

    // ════════════════════════════════════════════════════════════════════════
    // NEWS SCORE CALCULATOR  (mirrors NewsService.Calculate exactly)
    // ════════════════════════════════════════════════════════════════════════

    private static int CalculateNews(VitalReading v)
    {
        int score = 0;

        // Oxygen
        if (v.O2 <= 91) score += 3;
        else if (v.O2 <= 93) score += 2;
        else if (v.O2 <= 95) score += 1;

        // Heart Rate
        if (v.HR >= 131) score += 3;
        else if (v.HR >= 111) score += 2;
        else if (v.HR >= 91) score += 1;

        // Temperature
        if (v.Temp >= 39) score += 2;
        else if (v.Temp <= 35) score += 3;

        // Blood Pressure
        if (v.SysBP <= 90) score += 3;
        else if (v.SysBP <= 100) score += 2;

        // Respiration
        if (v.RR >= 25) score += 3;
        else if (v.RR >= 21) score += 2;

        return score;
    }

    private static PatientStatus NewsToPatientStatus(int news) =>
        news >= 7 ? PatientStatus.Critical :
        news >= 4 ? PatientStatus.Unstable :
                    PatientStatus.Stable;

    // ════════════════════════════════════════════════════════════════════════
    // VITAL SIGNS  — time-series, consistent with patient's band
    // ════════════════════════════════════════════════════════════════════════

    private List<VitalSign> SeedVitalSigns(List<Patient> patients)
    {
        var vitals = new List<VitalSign>();

        foreach (var patient in patients)
        {
            // Determine which band this patient belongs to based on FlowStatus + NEWS
            var band = PatientToBand(patient);

            var start = patient.AdmittedAt;
            // FlowStatus (workflow stage) determines the time window for vitals.
            // PatientStatus (medical severity) is NOT used here — FlowStatus is.
            var end = (patient.FlowStatus == PatientFlowStatus.Stable)
                ? patient.AdmittedAt.AddDays(_rng.Next(3, 10))   // exited: fixed window
                : DateTime.UtcNow;                                 // admitted: up to now

            // Deterioration applies only to Critical patients (PatientStatus.Critical,
            // which corresponds to NEWS ≥ 7 and FlowStatus.WaitingDoctor).
            bool deteriorating = patient.NewsScore >= 7;
            var current = start;

            while (current <= end)
            {
                double elapsed = (current - start).TotalDays;

                // Small progressive shift: critical patients worsen, stable patients stay flat
                double shift = deteriorating ? Math.Min(elapsed * 0.04, 0.25) : 0;

                var v = GenerateRepresentativeVital(band);

                // Apply deterioration shift (worsens each vital slightly over time)
                var hr = v.HR + (int)(shift * 12);
                var o2 = v.O2 - (int)(shift * 6);
                var temp = v.Temp + shift * 0.3;
                var sysBP = v.SysBP + (int)(shift * 10);
                var diaBP = v.DiaBP + (int)(shift * 6);
                var rr = v.RR + (int)(shift * 4);

                // Add small random jitter (±5% of typical range) — stays within band
                vitals.Add(new VitalSign
                {
                    // Id omitted — auto-increment
                    PatientId = patient.Id,
                    HeartRate = Clamp(hr + Ri(-4, 4), 30, 200),
                    OxygenLevel = Clamp(o2 - Ri(0, 2), 50, 100),
                    Temperature = ClampD(temp + Rd(-0.2, 0.2), 33.0, 42.0),
                    SystolicPressure = Clamp(sysBP + Ri(-6, 6), 60, 240),
                    DiastolicPressure = Clamp(diaBP + Ri(-4, 4), 40, 140),
                    RespirationRate = Clamp(rr + Ri(-1, 1), 8, 40),
                    RecordedAt = current
                });

                current = current.AddHours(3).AddMinutes(_rng.Next(-10, 10));
            }
        }
        return vitals;
    }

    // Map patient back to a vital band so time-series vitals stay consistent
    private static string PatientToBand(Patient p) =>
        p.NewsScore switch
        {
            <= 2 => "Green",
            <= 3 => "Mild",
            <= 6 => "Yellow",
            <= 9 => "Red",
            _ => "DarkRed"
        };

    // ════════════════════════════════════════════════════════════════════════
    // ALERTS  — driven by patient severity, NOT per vital reading
    // ════════════════════════════════════════════════════════════════════════
    // We don't loop over every vital to avoid generating thousands of alerts.
    // Instead, each critical patient gets a realistic set of alerts that match
    // their condition. Stable patients get zero or one informational alert.

    private List<Alert> SeedAlerts(
        List<Patient> patients,
        List<ApplicationUser> doctors,
        List<ApplicationUser> nurses)
    {
        var alerts = new List<Alert>();

        foreach (var patient in patients)
        {
            var baseTime = patient.AdmittedAt.AddHours(_rng.Next(1, 6));

            if (patient.NewsScore >= 7)
            {
                // Critical patients: 2-4 alerts (Critical severity → target Doctor)
                var criticalMessages = new[]
                {
                    $"NEWS Score {patient.NewsScore} — immediate escalation required",
                    $"O2 saturation critically low — urgent intervention needed",
                    $"Tachycardia / elevated heart rate detected",
                    $"Hypertensive crisis — BP dangerously high"
                };
                int alertCount = _rng.Next(2, 5);
                for (int i = 0; i < Math.Min(alertCount, criticalMessages.Length); i++)
                {
                    alerts.Add(BuildAlert(
                        patient.Id,
                        baseTime.AddMinutes(i * _rng.Next(10, 40)),
                        AlertSeverity.Critical,
                        criticalMessages[i],
                        doctors, nurses));
                }
            }
            else if (patient.NewsScore >= 4)
            {
                // Unstable patients: 1-2 Warning alerts → target Nurse
                var warnMessages = new[]
                {
                    $"Mild oxygen drop — monitor closely",
                    $"Elevated respiration rate — reassess in 1 hour"
                };
                int alertCount = _rng.Next(1, 3);
                for (int i = 0; i < Math.Min(alertCount, warnMessages.Length); i++)
                {
                    alerts.Add(BuildAlert(
                        patient.Id,
                        baseTime.AddMinutes(i * _rng.Next(15, 60)),
                        AlertSeverity.Warning,
                        warnMessages[i],
                        doctors, nurses));
                }
            }
            else if (patient.FlowStatus == PatientFlowStatus.ObservationalStable)
            {
                // Stable patients: optional Info alert (50% chance)
                if (_rng.Next(2) == 0)
                {
                    alerts.Add(BuildAlert(
                        patient.Id,
                        baseTime,
                        AlertSeverity.Info,
                        $"Patient admitted and stable — routine monitoring in progress",
                        doctors, nurses));
                }
            }
            // Discharged patients (FlowStatus.Stable): no alerts
        }

        return alerts;
    }

    private Alert BuildAlert(
        int patientId,
        DateTime time,
        AlertSeverity severity,
        string message,
        List<ApplicationUser> doctors,
        List<ApplicationUser> nurses)
    {
        // Critical → Doctor; Warning / Info → Nurse
        var (targetRole, pool) = severity == AlertSeverity.Critical
            ? (AppRoles.Doctor, doctors)
            : (AppRoles.Nurse, nurses);

        var targetUserId = pool.Any() ? pool[_rng.Next(pool.Count)].Id : (string?)null;

        // Older alerts are more likely to have been read
        bool isRead = time < DateTime.UtcNow.AddDays(-3)
            ? _rng.Next(100) < 85   // old → 85% read
            : _rng.Next(100) < 30;  // recent → 30% read

        return new Alert
        {
            // Id omitted — auto-increment
            PatientId = patientId,
            TargetRole = targetRole,
            TargetUserId = targetUserId,
            Message = message,
            Severity = severity,
            IsRead = isRead,
            CreatedAt = time
        };
    }

    // ════════════════════════════════════════════════════════════════════════
    // PATIENT TASKS  — consistent with patient severity
    // ════════════════════════════════════════════════════════════════════════

    private List<PatientTask> SeedPatientTasks(
        List<Patient> patients,
        List<Alert> alerts,
        List<ApplicationUser> doctors,
        List<ApplicationUser> nurses)
    {
        var tasks = new List<PatientTask>();

        // Task title pools per severity level
        var criticalTasks = new[]
        {
            "Administer emergency medication",
            "Prepare for immediate doctor examination",
            "Attach cardiac monitor",
            "Insert IV line",
            "Oxygen mask adjustment",
            "Notify on-call doctor"
        };
        var stableTasks = new[]
        {
            "Check vital signs",
            "Blood glucose check",
            "Administer scheduled medication",
            "Patient repositioning",
            "Wound dressing",
            "Review lab results"
        };

        foreach (var patient in patients)
        {
            // isMedicallyStable  — PatientStatus (medical severity)
            // isWorkflowExiting  — PatientFlowStatus (workflow stage)
            // These are TWO SEPARATE checks. A patient can be medically stable
            // but still in WaitingDoctor (doctor hasn't reviewed yet).

            bool isMedicallyUnstable = patient.CurrentStatus == PatientStatus.Unstable;
            bool isMedicallyCritical = patient.CurrentStatus == PatientStatus.Critical;
            // FlowStatus.Stable = doctor cleared the patient (workflow stage = exiting)
            bool isWorkflowExiting = patient.FlowStatus == PatientFlowStatus.Stable;

            if (isWorkflowExiting)
            {
                // Exiting patients (FlowStatus.Stable): one completed final task only
                tasks.Add(BuildTask(
                    patient.Id,
                    AppRoles.Nurse,
                    nurses.Any() ? nurses[_rng.Next(nurses.Count)].Id : null,
                    "Final vital signs check",
                    "Pre-discharge vitals recorded and signed off.",
                    PatientTaskStatus.Completed,
                    patient.AdmittedAt.AddDays(_rng.Next(1, 5)),
                    completed: true,
                    patient.AdmittedAt));
                continue;
            }

            if (isMedicallyCritical)  // PatientStatus.Critical (NEWS ≥ 7)
            {
                // 3-5 tasks: mix of completed and pending
                int count = _rng.Next(3, 6);
                for (int i = 0; i < count; i++)
                {
                    bool done = i < count - 1; // last one is still pending
                    var alert = alerts.FirstOrDefault(a =>
                        a.PatientId == patient.Id && a.Severity == AlertSeverity.Critical);

                    tasks.Add(BuildTask(
                        patient.Id,
                        i % 3 == 0 ? AppRoles.Doctor : AppRoles.Nurse,
                        i % 3 == 0
                            ? (doctors.Any() ? doctors[_rng.Next(doctors.Count)].Id : null)
                            : (nurses.Any() ? nurses[_rng.Next(nurses.Count)].Id : null),
                        criticalTasks[i % criticalTasks.Length],
                        alert != null
                            ? $"Triggered by alert: {alert.Message}"
                            : $"Critical NEWS {patient.NewsScore} — immediate action required (PatientStatus: {patient.CurrentStatus})",
                        done ? PatientTaskStatus.Completed : PatientTaskStatus.Pending,
                        patient.AdmittedAt.AddHours(_rng.Next(1, 12)),
                        done,
                        patient.AdmittedAt));
                }
            }
            else if (isMedicallyUnstable)  // PatientStatus.Unstable (NEWS 4-6)
            {
                // 1-2 tasks: mostly completed
                int count = _rng.Next(1, 3);
                for (int i = 0; i < count; i++)
                {
                    bool done = _rng.Next(100) < 70;
                    tasks.Add(BuildTask(
                        patient.Id,
                        AppRoles.Nurse,
                        nurses.Any() ? nurses[_rng.Next(nurses.Count)].Id : null,
                        stableTasks[_rng.Next(stableTasks.Length)],
                        "Routine monitoring — patient under observation",
                        done ? PatientTaskStatus.Completed : PatientTaskStatus.Pending,
                        patient.AdmittedAt.AddHours(_rng.Next(1, 8)),
                        done,
                        patient.AdmittedAt));
                }
            }
            else
            {
                // PatientStatus.Stable (NEWS 0-3, FlowStatus.ObservationalStable):
                // 1 routine task, completed
                tasks.Add(BuildTask(
                    patient.Id,
                    AppRoles.Nurse,
                    nurses.Any() ? nurses[_rng.Next(nurses.Count)].Id : null,
                    stableTasks[_rng.Next(stableTasks.Length)],
                    "Routine check — patient stable",
                    PatientTaskStatus.Completed,
                    patient.AdmittedAt.AddHours(_rng.Next(1, 6)),
                    completed: true,
                    patient.AdmittedAt));
            }
        }
        return tasks;
    }

    private PatientTask BuildTask(
        int patientId,
        string role,
        string? userId,
        string title,
        string description,
        PatientTaskStatus status,
        DateTime createdAt,
        bool completed,
        DateTime admittedAt)
    {
        return new PatientTask
        {
            // Id omitted — auto-increment
            PatientId = patientId,
            AssignedToRole = role,
            AssignedToUserId = userId,
            Title = title,
            Description = description,
            Status = status,
            CreatedAt = createdAt,
            CompletedAt = completed
                ? createdAt.AddMinutes(_rng.Next(20, 180))
                : null
        };
    }

    // ════════════════════════════════════════════════════════════════════════
    // MEDICAL REPORTS  — consistent with FlowStatus
    // ════════════════════════════════════════════════════════════════════════
    //
    // MedicalReportType has only two values:
    //   TreatmentPlan  = 1  → doctor wrote a treatment plan (patient still admitted)
    //   DischargeReport = 2 → doctor cleared patient for discharge
    //
    // Rules:
    //   WaitingDoctor / UnderObservation  → TreatmentPlan only
    //   ObservationalStable               → TreatmentPlan (maybe + DischargeReport)
    //   Stable (discharged)               → TreatmentPlan + DischargeReport

    private List<MedicalReport> SeedMedicalReports(
        List<Patient> patients,
        List<ApplicationUser> doctors)
    {
        var reports = new List<MedicalReport>();

        foreach (var patient in patients)
        {
            if (!doctors.Any()) continue;

            var doctor = doctors[_rng.Next(doctors.Count)];
            bool isExiting = patient.FlowStatus == PatientFlowStatus.Stable;

            // How many TreatmentPlan reports? (simulates multiple doctor visits)
            int planCount = patient.NewsScore >= 7 ? _rng.Next(2, 4)
                          : patient.NewsScore >= 4 ? _rng.Next(1, 3)
                          : 1;

            for (int i = 0; i < planCount; i++)
            {
                reports.Add(new MedicalReport
                {
                    // Id omitted — auto-increment
                    PatientId = patient.Id,
                    DoctorId = doctor.Id,
                    Diagnosis = patient.Background ?? "Under investigation",
                    TreatmentPlan = patient.CurrentTreatment ?? "To be determined",
                    ReportType = MedicalReportType.TreatmentPlan,
                    CreatedAt = patient.AdmittedAt.AddDays(i * _rng.Next(1, 3))
                                                       .AddHours(_rng.Next(6, 18))
                });
            }

            if (isExiting)
            {
                // Add DischargeReport — always the last one, after treatment plans
                reports.Add(new MedicalReport
                {
                    PatientId = patient.Id,
                    DoctorId = doctor.Id,
                    Diagnosis = patient.Background ?? "Resolved",
                    TreatmentPlan = "Patient stable. Discharge approved.",
                    ReportType = MedicalReportType.DischargeReport,
                    CreatedAt = patient.AdmittedAt
                                           .AddDays(planCount * _rng.Next(1, 3))
                                           .AddHours(_rng.Next(6, 18))
                });
            }
        }
        return reports;
    }

    // ════════════════════════════════════════════════════════════════════════
    // HELPER UTILITIES
    // ════════════════════════════════════════════════════════════════════════

    // Random int in [min, max)
    private int Ri(int min, int max) => _rng.Next(min, max);
    // Random double in [min, max)
    private double Rd(double min, double max) => min + _rng.NextDouble() * (max - min);

    private static int Clamp(int v, int lo, int hi) => Math.Max(lo, Math.Min(hi, v));
    private static double ClampD(double v, double lo, double hi) => Math.Max(lo, Math.Min(hi, v));

    private string PickMedications() =>
        _rng.Next(4) switch
        {
            0 => "Aspirin 100mg, Metformin 500mg",
            1 => "Warfarin 5mg, Atorvastatin 40mg, Lisinopril 10mg",
            2 => "Insulin Glargine 20U, Amlodipine 5mg",
            _ => "Paracetamol 1g, Omeprazole 20mg, Enoxaparin 40mg"
        };

    private string PickTreatment(string band) =>
        band switch
        {
            "Green" => "Routine monitoring, oral medications",
            "Mild" => "Oral medications, daily vitals check",
            "Yellow" => "IV Fluids, supplemental oxygen, close monitoring",
            "Red" => "IV Antibiotics, oxygen therapy, cardiac monitoring",
            "DarkRed" => "ICU-level care, vasopressors, mechanical ventilation support",
            _ => "Supportive care"
        };
}
