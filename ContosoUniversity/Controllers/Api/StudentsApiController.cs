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
    [Route("api/students")]
    [ApiController]
    public class StudentsApiController : ControllerBase
    {
        private readonly SchoolContext _context;
        private readonly NotificationService _notificationService;

        public StudentsApiController(SchoolContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // GET: api/students
        [HttpGet]
        public async Task<ActionResult<PaginatedResponse<StudentDto>>> GetStudents(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string sortOrder = "",
            [FromQuery] string searchString = "")
        {
            try
            {
                // Validate pagination parameters
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 100) pageSize = 100; // Max page size

                var studentsQuery = _context.Students.AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(searchString))
                {
                    studentsQuery = studentsQuery.Where(s => 
                        s.LastName.Contains(searchString) || 
                        s.FirstMidName.Contains(searchString));
                }

                // Apply sorting
                studentsQuery = sortOrder switch
                {
                    "name_desc" => studentsQuery.OrderByDescending(s => s.LastName),
                    "Date" => studentsQuery.OrderBy(s => s.EnrollmentDate),
                    "date_desc" => studentsQuery.OrderByDescending(s => s.EnrollmentDate),
                    _ => studentsQuery.OrderBy(s => s.LastName)
                };

                // Get total count before pagination
                var totalCount = await studentsQuery.CountAsync();

                // Apply pagination
                var students = await studentsQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new StudentDto
                    {
                        ID = s.ID,
                        LastName = s.LastName,
                        FirstMidName = s.FirstMidName,
                        FullName = s.LastName + ", " + s.FirstMidName,
                        EnrollmentDate = s.EnrollmentDate,
                        Enrollments = new List<EnrollmentDto>()
                    })
                    .ToListAsync();

                var response = new PaginatedResponse<StudentDto>(students, totalCount, page, pageSize);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse(
                    "An error occurred while retrieving students.",
                    new List<string> { ex.Message }
                ));
            }
        }

        // GET: api/students/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<StudentDto>>> GetStudent(int id)
        {
            try
            {
                var student = await _context.Students
                    .Include(s => s.Enrollments)
                        .ThenInclude(e => e.Course)
                    .Where(s => s.ID == id)
                    .Select(s => new StudentDto
                    {
                        ID = s.ID,
                        LastName = s.LastName,
                        FirstMidName = s.FirstMidName,
                        FullName = s.LastName + ", " + s.FirstMidName,
                        EnrollmentDate = s.EnrollmentDate,
                        Enrollments = s.Enrollments.Select(e => new EnrollmentDto
                        {
                            EnrollmentID = e.EnrollmentID,
                            CourseID = e.CourseID,
                            CourseTitle = e.Course.Title,
                            StudentID = e.StudentID,
                            Grade = e.Grade
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (student == null)
                {
                    return NotFound(ApiResponse<StudentDto>.ErrorResponse(
                        $"Student with ID {id} not found.",
                        new List<string> { "Student not found" }
                    ));
                }

                return Ok(ApiResponse<StudentDto>.SuccessResponse(student));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<StudentDto>.ErrorResponse(
                    "An error occurred while retrieving the student.",
                    new List<string> { ex.Message }
                ));
            }
        }

        // POST: api/students
        [HttpPost]
        public async Task<ActionResult<ApiResponse<StudentDto>>> CreateStudent([FromBody] StudentCreateDto studentDto)
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
                    
                    return BadRequest(ApiResponse<StudentDto>.ErrorResponse(
                        "Validation failed.",
                        errors
                    ));
                }

                // Additional validation for enrollment date
                if (studentDto.EnrollmentDate == DateTime.MinValue || studentDto.EnrollmentDate == default(DateTime))
                {
                    return BadRequest(ApiResponse<StudentDto>.ErrorResponse(
                        "Validation failed.",
                        new List<string> { "Please enter a valid enrollment date." }
                    ));
                }

                if (studentDto.EnrollmentDate < new DateTime(1753, 1, 1) || studentDto.EnrollmentDate > new DateTime(9999, 12, 31))
                {
                    return BadRequest(ApiResponse<StudentDto>.ErrorResponse(
                        "Validation failed.",
                        new List<string> { "Enrollment date must be between 1753 and 9999." }
                    ));
                }

                // Create student entity
                var student = new Student
                {
                    LastName = studentDto.LastName,
                    FirstMidName = studentDto.FirstMidName,
                    EnrollmentDate = studentDto.EnrollmentDate
                };

                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                // Send notification
                var studentName = $"{student.FirstMidName} {student.LastName}";
                _notificationService.SendNotification("Student", student.ID.ToString(), studentName, EntityOperation.CREATE, "System");

                // Create response DTO
                var responseDto = new StudentDto
                {
                    ID = student.ID,
                    LastName = student.LastName,
                    FirstMidName = student.FirstMidName,
                    FullName = student.LastName + ", " + student.FirstMidName,
                    EnrollmentDate = student.EnrollmentDate,
                    Enrollments = new List<EnrollmentDto>()
                };

                return CreatedAtAction(
                    nameof(GetStudent),
                    new { id = student.ID },
                    ApiResponse<StudentDto>.SuccessResponse(responseDto, "Student created successfully.")
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<StudentDto>.ErrorResponse(
                    "An error occurred while creating the student.",
                    new List<string> { ex.Message }
                ));
            }
        }

        // PUT: api/students/5
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<StudentDto>>> UpdateStudent(int id, [FromBody] StudentUpdateDto studentDto)
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
                    
                    return BadRequest(ApiResponse<StudentDto>.ErrorResponse(
                        "Validation failed.",
                        errors
                    ));
                }

                // Additional validation for enrollment date
                if (studentDto.EnrollmentDate == DateTime.MinValue || studentDto.EnrollmentDate == default(DateTime))
                {
                    return BadRequest(ApiResponse<StudentDto>.ErrorResponse(
                        "Validation failed.",
                        new List<string> { "Please enter a valid enrollment date." }
                    ));
                }

                if (studentDto.EnrollmentDate < new DateTime(1753, 1, 1) || studentDto.EnrollmentDate > new DateTime(9999, 12, 31))
                {
                    return BadRequest(ApiResponse<StudentDto>.ErrorResponse(
                        "Validation failed.",
                        new List<string> { "Enrollment date must be between 1753 and 9999." }
                    ));
                }

                // Find existing student
                var student = await _context.Students.FindAsync(id);
                if (student == null)
                {
                    return NotFound(ApiResponse<StudentDto>.ErrorResponse(
                        $"Student with ID {id} not found.",
                        new List<string> { "Student not found" }
                    ));
                }

                // Update student properties
                student.LastName = studentDto.LastName;
                student.FirstMidName = studentDto.FirstMidName;
                student.EnrollmentDate = studentDto.EnrollmentDate;

                _context.Entry(student).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                // Send notification
                var studentName = $"{student.FirstMidName} {student.LastName}";
                _notificationService.SendNotification("Student", student.ID.ToString(), studentName, EntityOperation.UPDATE, "System");

                // Create response DTO
                var responseDto = new StudentDto
                {
                    ID = student.ID,
                    LastName = student.LastName,
                    FirstMidName = student.FirstMidName,
                    FullName = student.LastName + ", " + student.FirstMidName,
                    EnrollmentDate = student.EnrollmentDate,
                    Enrollments = new List<EnrollmentDto>()
                };

                return Ok(ApiResponse<StudentDto>.SuccessResponse(responseDto, "Student updated successfully."));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await StudentExists(id))
                {
                    return NotFound(ApiResponse<StudentDto>.ErrorResponse(
                        $"Student with ID {id} not found.",
                        new List<string> { "Student not found" }
                    ));
                }
                else
                {
                    return StatusCode(500, ApiResponse<StudentDto>.ErrorResponse(
                        "A concurrency error occurred while updating the student.",
                        new List<string> { "Concurrency conflict" }
                    ));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<StudentDto>.ErrorResponse(
                    "An error occurred while updating the student.",
                    new List<string> { ex.Message }
                ));
            }
        }

        // DELETE: api/students/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteStudent(int id)
        {
            try
            {
                var student = await _context.Students.FindAsync(id);
                if (student == null)
                {
                    return NotFound(ApiResponse<bool>.ErrorResponse(
                        $"Student with ID {id} not found.",
                        new List<string> { "Student not found" }
                    ));
                }

                var studentName = $"{student.FirstMidName} {student.LastName}";

                _context.Students.Remove(student);
                await _context.SaveChangesAsync();

                // Send notification
                _notificationService.SendNotification("Student", id.ToString(), studentName, EntityOperation.DELETE, "System");

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Student deleted successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<bool>.ErrorResponse(
                    "An error occurred while deleting the student.",
                    new List<string> { ex.Message }
                ));
            }
        }

        private async Task<bool> StudentExists(int id)
        {
            return await _context.Students.AnyAsync(e => e.ID == id);
        }
    }
}
