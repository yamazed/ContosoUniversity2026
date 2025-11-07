using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Data;
using ContosoUniversity.Models;
using ContosoUniversity.DTOs;
using ContosoUniversity.Services;

namespace ContosoUniversity.Controllers.Api
{
    [Route("api/departments")]
    [ApiController]
    public class DepartmentsApiController : ControllerBase
    {
        private readonly SchoolContext _context;
        private readonly NotificationService _notificationService;

        public DepartmentsApiController(SchoolContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // GET: api/departments
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<DepartmentDto>>>> GetDepartments()
        {
            try
            {
                var departments = await _context.Departments
                    .Include(d => d.Administrator)
                    .Select(d => new DepartmentDto
                    {
                        DepartmentID = d.DepartmentID,
                        Name = d.Name,
                        Budget = d.Budget,
                        StartDate = d.StartDate,
                        InstructorID = d.InstructorID,
                        AdministratorName = d.Administrator != null 
                            ? d.Administrator.FirstMidName + " " + d.Administrator.LastName 
                            : null,
                        RowVersion = d.RowVersion
                    })
                    .ToListAsync();

                return Ok(ApiResponse<List<DepartmentDto>>.SuccessResponse(departments));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<DepartmentDto>>.ErrorResponse(
                    "An error occurred while retrieving departments.",
                    new List<string> { ex.Message }
                ));
            }
        }

        // GET: api/departments/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<DepartmentDto>>> GetDepartment(int id)
        {
            try
            {
                var department = await _context.Departments
                    .Include(d => d.Administrator)
                    .Where(d => d.DepartmentID == id)
                    .Select(d => new DepartmentDto
                    {
                        DepartmentID = d.DepartmentID,
                        Name = d.Name,
                        Budget = d.Budget,
                        StartDate = d.StartDate,
                        InstructorID = d.InstructorID,
                        AdministratorName = d.Administrator != null 
                            ? d.Administrator.FirstMidName + " " + d.Administrator.LastName 
                            : null,
                        RowVersion = d.RowVersion
                    })
                    .FirstOrDefaultAsync();

                if (department == null)
                {
                    return NotFound(ApiResponse<DepartmentDto>.ErrorResponse(
                        $"Department with ID {id} not found.",
                        new List<string> { "Department not found" }
                    ));
                }

                return Ok(ApiResponse<DepartmentDto>.SuccessResponse(department));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<DepartmentDto>.ErrorResponse(
                    "An error occurred while retrieving the department.",
                    new List<string> { ex.Message }
                ));
            }
        }

        // POST: api/departments
        [HttpPost]
        public async Task<ActionResult<ApiResponse<DepartmentDto>>> CreateDepartment([FromBody] DepartmentCreateDto departmentDto)
        {
            try
            {
                // Validate model state
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    
                    return BadRequest(ApiResponse<DepartmentDto>.ErrorResponse(
                        "Validation failed.",
                        errors
                    ));
                }

                // Validate instructor exists if provided
                if (departmentDto.InstructorID.HasValue)
                {
                    if (!await _context.Instructors.AnyAsync(i => i.ID == departmentDto.InstructorID.Value))
                    {
                        return BadRequest(ApiResponse<DepartmentDto>.ErrorResponse(
                            "Validation failed.",
                            new List<string> { "Invalid instructor ID." }
                        ));
                    }
                }

                // Validate budget
                if (departmentDto.Budget < 0)
                {
                    return BadRequest(ApiResponse<DepartmentDto>.ErrorResponse(
                        "Validation failed.",
                        new List<string> { "Budget must be a positive value." }
                    ));
                }

                // Validate start date
                if (departmentDto.StartDate == DateTime.MinValue || departmentDto.StartDate == default(DateTime))
                {
                    return BadRequest(ApiResponse<DepartmentDto>.ErrorResponse(
                        "Validation failed.",
                        new List<string> { "Please enter a valid start date." }
                    ));
                }

                // Create department entity
                var department = new Department
                {
                    Name = departmentDto.Name,
                    Budget = departmentDto.Budget,
                    StartDate = departmentDto.StartDate,
                    InstructorID = departmentDto.InstructorID
                };

                _context.Departments.Add(department);
                await _context.SaveChangesAsync();

                // Send notification
                _notificationService.SendNotification("Department", department.DepartmentID.ToString(), department.Name, EntityOperation.CREATE, "System");

                // Load administrator for response
                if (department.InstructorID.HasValue)
                {
                    await _context.Entry(department).Reference(d => d.Administrator).LoadAsync();
                }

                // Create response DTO
                var responseDto = new DepartmentDto
                {
                    DepartmentID = department.DepartmentID,
                    Name = department.Name,
                    Budget = department.Budget,
                    StartDate = department.StartDate,
                    InstructorID = department.InstructorID,
                    AdministratorName = department.Administrator != null 
                        ? department.Administrator.FirstMidName + " " + department.Administrator.LastName 
                        : null,
                    RowVersion = department.RowVersion
                };

                return CreatedAtAction(
                    nameof(GetDepartment),
                    new { id = department.DepartmentID },
                    ApiResponse<DepartmentDto>.SuccessResponse(responseDto, "Department created successfully.")
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<DepartmentDto>.ErrorResponse(
                    "An error occurred while creating the department.",
                    new List<string> { ex.Message }
                ));
            }
        }

        // PUT: api/departments/5
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<DepartmentDto>>> UpdateDepartment(int id, [FromBody] DepartmentUpdateDto departmentDto)
        {
            try
            {
                // Validate model state
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    
                    return BadRequest(ApiResponse<DepartmentDto>.ErrorResponse(
                        "Validation failed.",
                        errors
                    ));
                }

                // Validate instructor exists if provided
                if (departmentDto.InstructorID.HasValue)
                {
                    if (!await _context.Instructors.AnyAsync(i => i.ID == departmentDto.InstructorID.Value))
                    {
                        return BadRequest(ApiResponse<DepartmentDto>.ErrorResponse(
                            "Validation failed.",
                            new List<string> { "Invalid instructor ID." }
                        ));
                    }
                }

                // Validate budget
                if (departmentDto.Budget < 0)
                {
                    return BadRequest(ApiResponse<DepartmentDto>.ErrorResponse(
                        "Validation failed.",
                        new List<string> { "Budget must be a positive value." }
                    ));
                }

                // Validate start date
                if (departmentDto.StartDate == DateTime.MinValue || departmentDto.StartDate == default(DateTime))
                {
                    return BadRequest(ApiResponse<DepartmentDto>.ErrorResponse(
                        "Validation failed.",
                        new List<string> { "Please enter a valid start date." }
                    ));
                }

                // Find existing department
                var department = await _context.Departments.FindAsync(id);
                if (department == null)
                {
                    return NotFound(ApiResponse<DepartmentDto>.ErrorResponse(
                        $"Department with ID {id} not found.",
                        new List<string> { "Department not found" }
                    ));
                }

                // Update department properties
                department.Name = departmentDto.Name;
                department.Budget = departmentDto.Budget;
                department.StartDate = departmentDto.StartDate;
                department.InstructorID = departmentDto.InstructorID;
                department.RowVersion = departmentDto.RowVersion;

                _context.Entry(department).State = EntityState.Modified;
                _context.Entry(department).Property(d => d.RowVersion).OriginalValue = departmentDto.RowVersion;

                try
                {
                    await _context.SaveChangesAsync();

                    // Send notification
                    _notificationService.SendNotification("Department", department.DepartmentID.ToString(), department.Name, EntityOperation.UPDATE, "System");

                    // Load administrator for response
                    if (department.InstructorID.HasValue)
                    {
                        await _context.Entry(department).Reference(d => d.Administrator).LoadAsync();
                    }

                    // Create response DTO
                    var responseDto = new DepartmentDto
                    {
                        DepartmentID = department.DepartmentID,
                        Name = department.Name,
                        Budget = department.Budget,
                        StartDate = department.StartDate,
                        InstructorID = department.InstructorID,
                        AdministratorName = department.Administrator != null 
                            ? department.Administrator.FirstMidName + " " + department.Administrator.LastName 
                            : null,
                        RowVersion = department.RowVersion
                    };

                    return Ok(ApiResponse<DepartmentDto>.SuccessResponse(responseDto, "Department updated successfully."));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    var entry = ex.Entries.Single();
                    var databaseEntry = entry.GetDatabaseValues();
                    
                    if (databaseEntry == null)
                    {
                        return Conflict(ApiResponse<DepartmentDto>.ErrorResponse(
                            "Unable to save changes. The department was deleted by another user.",
                            new List<string> { "Department was deleted by another user." }
                        ));
                    }
                    else
                    {
                        var databaseValues = (Department)databaseEntry.ToObject();
                        
                        // Build detailed conflict information
                        var conflictErrors = new List<string>
                        {
                            "The record you attempted to edit was modified by another user after you got the original value."
                        };

                        if (databaseValues.Name != departmentDto.Name)
                            conflictErrors.Add($"Name - Current value: {databaseValues.Name}");
                        if (databaseValues.Budget != departmentDto.Budget)
                            conflictErrors.Add($"Budget - Current value: {databaseValues.Budget:c}");
                        if (databaseValues.StartDate != departmentDto.StartDate)
                            conflictErrors.Add($"StartDate - Current value: {databaseValues.StartDate:d}");
                        if (databaseValues.InstructorID != departmentDto.InstructorID)
                        {
                            var instructor = await _context.Instructors.FindAsync(databaseValues.InstructorID);
                            var instructorName = instructor != null 
                                ? $"{instructor.FirstMidName} {instructor.LastName}" 
                                : "None";
                            conflictErrors.Add($"Administrator - Current value: {instructorName}");
                        }

                        // Return current database values in the response
                        var currentDto = new DepartmentDto
                        {
                            DepartmentID = databaseValues.DepartmentID,
                            Name = databaseValues.Name,
                            Budget = databaseValues.Budget,
                            StartDate = databaseValues.StartDate,
                            InstructorID = databaseValues.InstructorID,
                            RowVersion = databaseValues.RowVersion
                        };

                        // Load administrator name for current values
                        if (databaseValues.InstructorID.HasValue)
                        {
                            var admin = await _context.Instructors.FindAsync(databaseValues.InstructorID);
                            currentDto.AdministratorName = admin != null 
                                ? $"{admin.FirstMidName} {admin.LastName}" 
                                : null;
                        }

                        var response = ApiResponse<DepartmentDto>.ErrorResponse(
                            "Concurrency conflict occurred.",
                            conflictErrors
                        );
                        response.Data = currentDto; // Include current database values

                        return Conflict(response);
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<DepartmentDto>.ErrorResponse(
                    "An error occurred while updating the department.",
                    new List<string> { ex.Message }
                ));
            }
        }

        // DELETE: api/departments/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteDepartment(int id)
        {
            try
            {
                var department = await _context.Departments.FindAsync(id);
                if (department == null)
                {
                    return NotFound(ApiResponse<bool>.ErrorResponse(
                        $"Department with ID {id} not found.",
                        new List<string> { "Department not found" }
                    ));
                }

                var departmentName = department.Name;

                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();

                // Send notification
                _notificationService.SendNotification("Department", id.ToString(), departmentName, EntityOperation.DELETE, "System");

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Department deleted successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<bool>.ErrorResponse(
                    "An error occurred while deleting the department.",
                    new List<string> { ex.Message }
                ));
            }
        }
    }
}
