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
    [Route("api/instructors")]
    [ApiController]
    public class InstructorsApiController : ControllerBase
    {
        private readonly SchoolContext _context;
        private readonly NotificationService _notificationService;

        public InstructorsApiController(SchoolContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // GET: api/instructors
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<InstructorDto>>>> GetInstructors(
            [FromQuery] int? instructorId,
            [FromQuery] int? courseId)
        {
            try
            {
                var instructorsQuery = _context.Instructors
                    .Include(i => i.OfficeAssignment)
                    .Include(i => i.CourseAssignments)
                        .ThenInclude(ca => ca.Course)
                            .ThenInclude(c => c.Department)
                    .AsQueryable();

                // Apply filtering by instructorId if provided
                if (instructorId.HasValue)
                {
                    instructorsQuery = instructorsQuery.Where(i => i.ID == instructorId.Value);
                }

                var instructors = await instructorsQuery
                    .OrderBy(i => i.LastName)
                    .Select(i => new InstructorDto
                    {
                        ID = i.ID,
                        LastName = i.LastName,
                        FirstMidName = i.FirstMidName,
                        FullName = i.LastName + ", " + i.FirstMidName,
                        HireDate = i.HireDate,
                        OfficeAssignment = i.OfficeAssignment != null ? new OfficeAssignmentDto
                        {
                            InstructorID = i.OfficeAssignment.InstructorID,
                            Location = i.OfficeAssignment.Location
                        } : null,
                        CourseAssignments = i.CourseAssignments.Select(ca => new CourseAssignmentDto
                        {
                            InstructorID = ca.InstructorID,
                            CourseID = ca.CourseID,
                            CourseTitle = ca.Course.Title
                        }).ToList()
                    })
                    .ToListAsync();

                // Apply filtering by courseId if provided (filter in memory after loading)
                if (courseId.HasValue)
                {
                    instructors = instructors
                        .Where(i => i.CourseAssignments.Any(ca => ca.CourseID == courseId.Value))
                        .ToList();
                }

                return Ok(ApiResponse<List<InstructorDto>>.SuccessResponse(instructors));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<InstructorDto>>.ErrorResponse(
                    "An error occurred while retrieving instructors.",
                    new List<string> { ex.Message }
                ));
            }
        }

        // GET: api/instructors/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<InstructorDto>>> GetInstructor(int id)
        {
            try
            {
                var instructor = await _context.Instructors
                    .Include(i => i.OfficeAssignment)
                    .Include(i => i.CourseAssignments)
                        .ThenInclude(ca => ca.Course)
                            .ThenInclude(c => c.Department)
                    .Where(i => i.ID == id)
                    .Select(i => new InstructorDto
                    {
                        ID = i.ID,
                        LastName = i.LastName,
                        FirstMidName = i.FirstMidName,
                        FullName = i.LastName + ", " + i.FirstMidName,
                        HireDate = i.HireDate,
                        OfficeAssignment = i.OfficeAssignment != null ? new OfficeAssignmentDto
                        {
                            InstructorID = i.OfficeAssignment.InstructorID,
                            Location = i.OfficeAssignment.Location
                        } : null,
                        CourseAssignments = i.CourseAssignments.Select(ca => new CourseAssignmentDto
                        {
                            InstructorID = ca.InstructorID,
                            CourseID = ca.CourseID,
                            CourseTitle = ca.Course.Title
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (instructor == null)
                {
                    return NotFound(ApiResponse<InstructorDto>.ErrorResponse(
                        $"Instructor with ID {id} not found.",
                        new List<string> { "Instructor not found" }
                    ));
                }

                return Ok(ApiResponse<InstructorDto>.SuccessResponse(instructor));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<InstructorDto>.ErrorResponse(
                    "An error occurred while retrieving the instructor.",
                    new List<string> { ex.Message }
                ));
            }
        }

        // POST: api/instructors
        [HttpPost]
        public async Task<ActionResult<ApiResponse<InstructorDto>>> CreateInstructor([FromBody] InstructorCreateDto instructorDto)
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
                    
                    return BadRequest(ApiResponse<InstructorDto>.ErrorResponse(
                        "Validation failed.",
                        errors
                    ));
                }

                // Additional validation for hire date
                if (instructorDto.HireDate == DateTime.MinValue || instructorDto.HireDate == default(DateTime))
                {
                    return BadRequest(ApiResponse<InstructorDto>.ErrorResponse(
                        "Validation failed.",
                        new List<string> { "Please enter a valid hire date." }
                    ));
                }

                if (instructorDto.HireDate < new DateTime(1753, 1, 1) || instructorDto.HireDate > new DateTime(9999, 12, 31))
                {
                    return BadRequest(ApiResponse<InstructorDto>.ErrorResponse(
                        "Validation failed.",
                        new List<string> { "Hire date must be between 1753 and 9999." }
                    ));
                }

                // Create instructor entity
                var instructor = new Instructor
                {
                    LastName = instructorDto.LastName,
                    FirstMidName = instructorDto.FirstMidName,
                    HireDate = instructorDto.HireDate,
                    CourseAssignments = new List<CourseAssignment>()
                };

                // Add office assignment if provided
                if (!string.IsNullOrWhiteSpace(instructorDto.OfficeLocation))
                {
                    instructor.OfficeAssignment = new OfficeAssignment
                    {
                        Location = instructorDto.OfficeLocation
                    };
                }

                _context.Instructors.Add(instructor);
                await _context.SaveChangesAsync();

                // Add course assignments if provided
                if (instructorDto.CourseIDs != null && instructorDto.CourseIDs.Any())
                {
                    foreach (var courseId in instructorDto.CourseIDs)
                    {
                        // Validate that course exists
                        if (!await _context.Courses.AnyAsync(c => c.CourseID == courseId))
                        {
                            return BadRequest(ApiResponse<InstructorDto>.ErrorResponse(
                                "Validation failed.",
                                new List<string> { $"Course with ID {courseId} does not exist." }
                            ));
                        }

                        var courseAssignment = new CourseAssignment
                        {
                            InstructorID = instructor.ID,
                            CourseID = courseId
                        };
                        _context.CourseAssignments.Add(courseAssignment);
                    }
                    await _context.SaveChangesAsync();
                }

                // Send notification
                var instructorName = $"{instructor.FirstMidName} {instructor.LastName}";
                _notificationService.SendNotification("Instructor", instructor.ID.ToString(), instructorName, EntityOperation.CREATE, "System");

                // Load related data for response
                await _context.Entry(instructor)
                    .Collection(i => i.CourseAssignments)
                    .Query()
                    .Include(ca => ca.Course)
                    .LoadAsync();

                if (instructor.OfficeAssignment != null)
                {
                    await _context.Entry(instructor).Reference(i => i.OfficeAssignment).LoadAsync();
                }

                // Create response DTO
                var responseDto = new InstructorDto
                {
                    ID = instructor.ID,
                    LastName = instructor.LastName,
                    FirstMidName = instructor.FirstMidName,
                    FullName = instructor.LastName + ", " + instructor.FirstMidName,
                    HireDate = instructor.HireDate,
                    OfficeAssignment = instructor.OfficeAssignment != null ? new OfficeAssignmentDto
                    {
                        InstructorID = instructor.OfficeAssignment.InstructorID,
                        Location = instructor.OfficeAssignment.Location
                    } : null,
                    CourseAssignments = instructor.CourseAssignments.Select(ca => new CourseAssignmentDto
                    {
                        InstructorID = ca.InstructorID,
                        CourseID = ca.CourseID,
                        CourseTitle = ca.Course.Title
                    }).ToList()
                };

                return CreatedAtAction(
                    nameof(GetInstructor),
                    new { id = instructor.ID },
                    ApiResponse<InstructorDto>.SuccessResponse(responseDto, "Instructor created successfully.")
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<InstructorDto>.ErrorResponse(
                    "An error occurred while creating the instructor.",
                    new List<string> { ex.Message }
                ));
            }
        }

        // PUT: api/instructors/5
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<InstructorDto>>> UpdateInstructor(int id, [FromBody] InstructorUpdateDto instructorDto)
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
                    
                    return BadRequest(ApiResponse<InstructorDto>.ErrorResponse(
                        "Validation failed.",
                        errors
                    ));
                }

                // Additional validation for hire date
                if (instructorDto.HireDate == DateTime.MinValue || instructorDto.HireDate == default(DateTime))
                {
                    return BadRequest(ApiResponse<InstructorDto>.ErrorResponse(
                        "Validation failed.",
                        new List<string> { "Please enter a valid hire date." }
                    ));
                }

                if (instructorDto.HireDate < new DateTime(1753, 1, 1) || instructorDto.HireDate > new DateTime(9999, 12, 31))
                {
                    return BadRequest(ApiResponse<InstructorDto>.ErrorResponse(
                        "Validation failed.",
                        new List<string> { "Hire date must be between 1753 and 9999." }
                    ));
                }

                // Find existing instructor
                var instructor = await _context.Instructors
                    .Include(i => i.OfficeAssignment)
                    .Include(i => i.CourseAssignments)
                    .Where(i => i.ID == id)
                    .FirstOrDefaultAsync();

                if (instructor == null)
                {
                    return NotFound(ApiResponse<InstructorDto>.ErrorResponse(
                        $"Instructor with ID {id} not found.",
                        new List<string> { "Instructor not found" }
                    ));
                }

                // Update instructor properties
                instructor.LastName = instructorDto.LastName;
                instructor.FirstMidName = instructorDto.FirstMidName;
                instructor.HireDate = instructorDto.HireDate;

                // Update office assignment
                if (string.IsNullOrWhiteSpace(instructorDto.OfficeLocation))
                {
                    // Remove office assignment if location is empty
                    if (instructor.OfficeAssignment != null)
                    {
                        _context.OfficeAssignments.Remove(instructor.OfficeAssignment);
                        instructor.OfficeAssignment = null;
                    }
                }
                else
                {
                    // Add or update office assignment
                    if (instructor.OfficeAssignment == null)
                    {
                        instructor.OfficeAssignment = new OfficeAssignment
                        {
                            InstructorID = instructor.ID,
                            Location = instructorDto.OfficeLocation
                        };
                    }
                    else
                    {
                        instructor.OfficeAssignment.Location = instructorDto.OfficeLocation;
                    }
                }

                // Update course assignments
                await UpdateInstructorCourses(instructorDto.CourseIDs, instructor);

                _context.Entry(instructor).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                // Send notification
                var instructorName = $"{instructor.FirstMidName} {instructor.LastName}";
                _notificationService.SendNotification("Instructor", instructor.ID.ToString(), instructorName, EntityOperation.UPDATE, "System");

                // Reload related data for response
                await _context.Entry(instructor)
                    .Collection(i => i.CourseAssignments)
                    .Query()
                    .Include(ca => ca.Course)
                    .LoadAsync();

                // Create response DTO
                var responseDto = new InstructorDto
                {
                    ID = instructor.ID,
                    LastName = instructor.LastName,
                    FirstMidName = instructor.FirstMidName,
                    FullName = instructor.LastName + ", " + instructor.FirstMidName,
                    HireDate = instructor.HireDate,
                    OfficeAssignment = instructor.OfficeAssignment != null ? new OfficeAssignmentDto
                    {
                        InstructorID = instructor.OfficeAssignment.InstructorID,
                        Location = instructor.OfficeAssignment.Location
                    } : null,
                    CourseAssignments = instructor.CourseAssignments.Select(ca => new CourseAssignmentDto
                    {
                        InstructorID = ca.InstructorID,
                        CourseID = ca.CourseID,
                        CourseTitle = ca.Course.Title
                    }).ToList()
                };

                return Ok(ApiResponse<InstructorDto>.SuccessResponse(responseDto, "Instructor updated successfully."));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await InstructorExists(id))
                {
                    return NotFound(ApiResponse<InstructorDto>.ErrorResponse(
                        $"Instructor with ID {id} not found.",
                        new List<string> { "Instructor not found" }
                    ));
                }
                else
                {
                    return StatusCode(500, ApiResponse<InstructorDto>.ErrorResponse(
                        "A concurrency error occurred while updating the instructor.",
                        new List<string> { "Concurrency conflict" }
                    ));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<InstructorDto>.ErrorResponse(
                    "An error occurred while updating the instructor.",
                    new List<string> { ex.Message }
                ));
            }
        }

        // DELETE: api/instructors/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteInstructor(int id)
        {
            try
            {
                var instructor = await _context.Instructors
                    .Include(i => i.OfficeAssignment)
                    .Where(i => i.ID == id)
                    .FirstOrDefaultAsync();

                if (instructor == null)
                {
                    return NotFound(ApiResponse<bool>.ErrorResponse(
                        $"Instructor with ID {id} not found.",
                        new List<string> { "Instructor not found" }
                    ));
                }

                var instructorName = $"{instructor.FirstMidName} {instructor.LastName}";

                // Handle department administrator cleanup
                var department = await _context.Departments
                    .Where(d => d.InstructorID == id)
                    .FirstOrDefaultAsync();

                if (department != null)
                {
                    department.InstructorID = null;
                }

                _context.Instructors.Remove(instructor);
                await _context.SaveChangesAsync();

                // Send notification
                _notificationService.SendNotification("Instructor", id.ToString(), instructorName, EntityOperation.DELETE, "System");

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Instructor deleted successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<bool>.ErrorResponse(
                    "An error occurred while deleting the instructor.",
                    new List<string> { ex.Message }
                ));
            }
        }

        // Helper methods

        private async Task<bool> InstructorExists(int id)
        {
            return await _context.Instructors.AnyAsync(e => e.ID == id);
        }

        private async Task UpdateInstructorCourses(List<int> selectedCourseIds, Instructor instructor)
        {
            if (selectedCourseIds == null)
            {
                selectedCourseIds = new List<int>();
            }

            var selectedCoursesHS = new HashSet<int>(selectedCourseIds);
            var instructorCourses = new HashSet<int>(instructor.CourseAssignments.Select(c => c.CourseID));

            // Get all courses to iterate through
            var allCourses = await _context.Courses.ToListAsync();

            foreach (var course in allCourses)
            {
                if (selectedCoursesHS.Contains(course.CourseID))
                {
                    // Add course assignment if not already assigned
                    if (!instructorCourses.Contains(course.CourseID))
                    {
                        var courseAssignment = new CourseAssignment
                        {
                            InstructorID = instructor.ID,
                            CourseID = course.CourseID
                        };
                        instructor.CourseAssignments.Add(courseAssignment);
                    }
                }
                else
                {
                    // Remove course assignment if it exists
                    if (instructorCourses.Contains(course.CourseID))
                    {
                        var courseToRemove = instructor.CourseAssignments
                            .FirstOrDefault(ca => ca.CourseID == course.CourseID);
                        if (courseToRemove != null)
                        {
                            _context.CourseAssignments.Remove(courseToRemove);
                        }
                    }
                }
            }
        }
    }
}
