using ISHMS.BLL.Services;
using ISHMS.Core.DTOs;
using ISHMS.Core.DTOs.Patient;
using ISHMS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISHMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PatientController : ControllerBase
{
    private readonly IPatientService _service;

    public PatientController(IPatientService service)
    {
        _service = service;
    }

    // ✅ Receptionist — إنشاء المريض وتحديد السرير
    [HttpPost]
    [Authorize(Roles = "Receptionist,Admin")]
    public async Task<IActionResult> Create(CreatePatientDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _service.Create(dto);
        return Ok(result);
    }

    // ✅ All — عرض كل المرضى
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAll();
        return Ok(result);
    }

    // ✅ All — عرض مريض بالـ Id
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetById(id);

        if (result == null)
            return NotFound("Patient not found");

        return Ok(result);
    }

    // ✅ Receptionist — حذف مريض
    [HttpDelete("{id}")]
    [Authorize(Roles = "Receptionist,Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.Delete(id);
        return Ok("Deleted Successfully");
    }

    // ✅ Nurse — إدخال Background والأدوية القديمة
    [HttpPut("{id}/nurse")]
    [Authorize(Roles = "Nurse,Admin")]
    public async Task<IActionResult> UpdateNurseInfo(int id, UpdateNurseDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await _service.UpdateNurseInfo(id, dto);
        return Ok("Nurse info updated successfully");
    }

    // ✅ Nurse — إضافة العلامات الحيوية
    [HttpPost("vital")]
    [Authorize(Roles = "Nurse,Admin")]
    public async Task<IActionResult> AddVital(CreateVitalDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await _service.AddVital(dto);
        return Ok("Vital signs added successfully");
    }

    // ✅ Doctor — إدخال العلاج الحالي
    [HttpPut("{id}/doctor")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> UpdateDoctorInfo(int id, UpdateDoctorDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await _service.UpdateDoctorInfo(id, dto);
        return Ok("Doctor info updated successfully");
    }
    // POST api/patient/discharge/{patientId}
    [HttpPost("discharge/{patientId}")]
    public async Task<IActionResult> Discharge(int patientId)
    {
        await _service.DischargeAsync(patientId);
        return Ok("Patient discharged successfully");
    }

    
    // Doctor فحص تفاعل الأدوية السابقة مع العلاج الجديد
    
    [HttpGet("{id}/medication/check")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> CheckDrugInteraction(int id)
    {
        var result = await _service.CheckDrugInteraction(id);
        return Ok(result);
    }

    [HttpPut("vital/{id}")]
    [Authorize(Roles = "Nurse")]
    public async Task<IActionResult> UpdateVital(int id, [FromBody] UpdateVitalSignDto dto)
    {
        var result = await _service.UpdateVitalSignAsync(id, dto);
        if (!result)
            return NotFound("Vital sign not found");

        return Ok("Vital signs updated successfully");
    }


    [HttpGet("{id}/isbar")]
    [Authorize(Roles = "Nurse,Doctor")]
    public async Task<IActionResult> GenerateIsbar(int id)
    {
        var result = await _service.GenerateIsbarAsync(id);
        return Ok(result);
    }
}