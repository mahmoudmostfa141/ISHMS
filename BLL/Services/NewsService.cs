using ISHMS.Core.Enums;

namespace ISHMS.BLL.Services;

public class NewsService
{
    public (int score, PatientStatus status, PriorityLevel priority) Calculate(
        int heartRate,
        int oxygen,
        double temp,
        int systolic,
        int respiration)
    {
        int score = 0;

        // 🔴 Oxygen
        if (oxygen <= 91) score += 3;
        else if (oxygen <= 93) score += 2;
        else if (oxygen <= 95) score += 1;

        // ❤️ Heart Rate
        if (heartRate >= 131) score += 3;
        else if (heartRate >= 111) score += 2;
        else if (heartRate >= 91) score += 1;

        // 🌡 Temperature
        if (temp >= 39) score += 2;
        else if (temp <= 35) score += 3;

        // 🩸 Blood Pressure
        if (systolic <= 90) score += 3;
        else if (systolic <= 100) score += 2;

        // 🌬 Respiration
        if (respiration >= 25) score += 3;
        else if (respiration >= 21) score += 2;

        // 🔥 تحديد الحالة
        PatientStatus status =
            score >= 7 ? PatientStatus.Critical :
            score >= 4 ? PatientStatus.Unstable :
            PatientStatus.Stable;

        // ⚡ تحديد الأولوية
        PriorityLevel priority =
            score >= 7 ? PriorityLevel.Emergency :
            score >= 4 ? PriorityLevel.High :
            score >= 2 ? PriorityLevel.Medium :
            PriorityLevel.Low;

        return (score, status, priority);
    }
}