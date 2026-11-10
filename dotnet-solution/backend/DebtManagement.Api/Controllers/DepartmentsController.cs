using DebtManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DebtManagement.Api.Controllers;

[ApiController]
[Route("api/departments")]
[Authorize]
public class DepartmentsController(IDepartmentRepository deptRepo) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var depts = await deptRepo.GetAllAsync();
        return Ok(depts.Select(d => new
        {
            id = d.Id, name = d.Name, code = d.Code,
            description = d.Description, sort_order = d.SortOrder
        }));
    }
}

