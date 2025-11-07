using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Data;
using ContosoUniversity.Models;
using ContosoUniversity.DTOs;
using ContosoUniversity.Services;

namespace ContosoUniversity.Controllers.Api
{
    [Route("api/courses")]
    [ApiController]
    public class CoursesApiController : ControllerBase
    {
        private readonly SchoolContext _context;
        private readonly NotificationService _notificationService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CoursesApiController(
            SchoolContext context, 
            NotificationService notificationService,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _notificationService = notificationService;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: api/courses
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<CourseDto>>>> GetCourses()
        {
            try
            {
                var courses = await _context.Courses
                    .Include(c => c.Department)
                    .Select(c => new CourseDto
                    {
                        CourseID = c.CourseID,
                        Title = c.Title,
                        Credits = c.Credits,
                        DepartmentID = c.DepartmentID,
                        DepartmentName = c.Department.Name,
                        TeachingMaterialImagePath = c.TeachingMaterialImagePath
                    })
                    .ToListAsync();

                return Ok(ApiResponse<List<CourseDto>>.SuccessResponse(courses));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<CourseDto>>.ErrorResponse(
                    "An error occurred while retrieving courses.",
                    new List<string> { ex.Message }
                ));
            }
        }

        // GET: api/courses/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<CourseDto>>> GetCourse(int id)
        {
            try
            {
                var course = await _context.Courses
                    .Include(c => c.Department)
                    .Where(c => c.CourseID == id)
                    .Select(c => new CourseDto
                    {
                        CourseID = c.CourseID,
                        Title = c.Title,
                        Credits = c.Credits,
                        DepartmentID = c.DepartmentID,
                        DepartmentName = c.Department.Name,
                        TeachingMaterialImagePath = c.TeachingMaterialImagePath
                    })
                    .FirstOrDefaultAsync();

                if (course == null)
                {
                    return NotFound(ApiResponse<CourseDto>.ErrorResponse(
                        $"Course with ID {id} not found.",
                        new List<string> { "Course not found" }
                    ));
                }

                return Ok(ApiResponse<CourseDto>.SuccessResponse(course));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<CourseDto>.ErrorResponse(
                    "An error occurred while retrieving the course.",
                    new List<string> { ex.Message }
                ));
            }
        }

        // POST: api/courses
        [HttpPost]
        public async Task<ActionResult<ApiResponse<CourseDto>>> CreateCourse(
            [FromForm] CourseCreateDto courseDto,
            [FromForm] IFormFile teachingMaterialImage)
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
                    
                    return BadRequest(ApiResponse<CourseDto>.ErrorResponse(
                        "Validation failed.",
                        errors
                    ));
                }

                // Validate course ID doesn't already exist
                if (await _context.Courses.AnyAsync(c => c.CourseID == courseDto.CourseID))
                {
                    return BadRequest(ApiResponse<CourseDto>.ErrorResponse(
                        "Validation failed.",
                        new List<string> { $"Course with ID {courseDto.CourseID} already exists." }
                    ));
                }

                // Validate department exists
                if (!await _context.Departments.AnyAsync(d => d.DepartmentID == courseDto.DepartmentID))
                {
                    return BadRequest(ApiResponse<CourseDto>.ErrorResponse(
                        "Validation failed.",
                        new List<string> { "Invalid department ID." }
                    ));
                }

                // Create course entity
                var course = new Course
                {
                    CourseID = courseDto.CourseID,
                    Title = courseDto.Title,
                    Credits = courseDto.Credits,
                    DepartmentID = courseDto.DepartmentID
                };

                // Handle file upload if provided
                if (teachingMaterialImage != null && teachingMaterialImage.Length > 0)
                {
                    var fileValidation = ValidateFile(teachingMaterialImage);
                    if (!fileValidation.IsValid)
                    {
                        return BadRequest(ApiResponse<CourseDto>.ErrorResponse(
                            "File validation failed.",
                            fileValidation.Errors
                        ));
                    }

                    try
                    {
                        var filePath = await SaveFileAsync(teachingMaterialImage, course.CourseID);
                        course.TeachingMaterialImagePath = filePath;
                    }
                    catch (Exception ex)
                    {
                        return StatusCode(500, ApiResponse<CourseDto>.ErrorResponse(
                            "Error uploading file.",
                            new List<string> { ex.Message }
                        ));
                    }
                }

                _context.Courses.Add(course);
                await _context.SaveChangesAsync();

                // Send notification
                _notificationService.SendNotification("Course", course.CourseID.ToString(), course.Title, EntityOperation.CREATE, "System");

                // Load department for response
                await _context.Entry(course).Reference(c => c.Department).LoadAsync();

                // Create response DTO
                var responseDto = new CourseDto
                {
                    CourseID = course.CourseID,
                    Title = course.Title,
                    Credits = course.Credits,
                    DepartmentID = course.DepartmentID,
                    DepartmentName = course.Department?.Name,
                    TeachingMaterialImagePath = course.TeachingMaterialImagePath
                };

                return CreatedAtAction(
                    nameof(GetCourse),
                    new { id = course.CourseID },
                    ApiResponse<CourseDto>.SuccessResponse(responseDto, "Course created successfully.")
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<CourseDto>.ErrorResponse(
                    "An error occurred while creating the course.",
                    new List<string> { ex.Message }
                ));
            }
        }

        // PUT: api/courses/5
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<CourseDto>>> UpdateCourse(
            int id,
            [FromForm] CourseUpdateDto courseDto,
            [FromForm] IFormFile teachingMaterialImage)
        {
            try
            {
                if (id != courseDto.CourseID)
                {
                    return BadRequest(ApiResponse<CourseDto>.ErrorResponse(
                        "Course ID mismatch.",
                        new List<string> { "The course ID in the URL does not match the course ID in the request body." }
                    ));
                }

                // Validate model state
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    
                    return BadRequest(ApiResponse<CourseDto>.ErrorResponse(
                        "Validation failed.",
                        errors
                    ));
                }

                // Find existing course
                var course = await _context.Courses.FindAsync(id);
                if (course == null)
                {
                    return NotFound(ApiResponse<CourseDto>.ErrorResponse(
                        $"Course with ID {id} not found.",
                        new List<string> { "Course not found" }
                    ));
                }

                // Validate department exists
                if (!await _context.Departments.AnyAsync(d => d.DepartmentID == courseDto.DepartmentID))
                {
                    return BadRequest(ApiResponse<CourseDto>.ErrorResponse(
                        "Validation failed.",
                        new List<string> { "Invalid department ID." }
                    ));
                }

                // Handle file upload if provided
                if (teachingMaterialImage != null && teachingMaterialImage.Length > 0)
                {
                    var fileValidation = ValidateFile(teachingMaterialImage);
                    if (!fileValidation.IsValid)
                    {
                        return BadRequest(ApiResponse<CourseDto>.ErrorResponse(
                            "File validation failed.",
                            fileValidation.Errors
                        ));
                    }

                    try
                    {
                        // Delete old file if exists
                        if (!string.IsNullOrEmpty(course.TeachingMaterialImagePath))
                        {
                            DeleteFile(course.TeachingMaterialImagePath);
                        }

                        // Save new file
                        var filePath = await SaveFileAsync(teachingMaterialImage, course.CourseID);
                        course.TeachingMaterialImagePath = filePath;
                    }
                    catch (Exception ex)
                    {
                        return StatusCode(500, ApiResponse<CourseDto>.ErrorResponse(
                            "Error uploading file.",
                            new List<string> { ex.Message }
                        ));
                    }
                }

                // Update course properties
                course.Title = courseDto.Title;
                course.Credits = courseDto.Credits;
                course.DepartmentID = courseDto.DepartmentID;

                _context.Entry(course).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                // Send notification
                _notificationService.SendNotification("Course", course.CourseID.ToString(), course.Title, EntityOperation.UPDATE, "System");

                // Load department for response
                await _context.Entry(course).Reference(c => c.Department).LoadAsync();

                // Create response DTO
                var responseDto = new CourseDto
                {
                    CourseID = course.CourseID,
                    Title = course.Title,
                    Credits = course.Credits,
                    DepartmentID = course.DepartmentID,
                    DepartmentName = course.Department?.Name,
                    TeachingMaterialImagePath = course.TeachingMaterialImagePath
                };

                return Ok(ApiResponse<CourseDto>.SuccessResponse(responseDto, "Course updated successfully."));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await CourseExists(id))
                {
                    return NotFound(ApiResponse<CourseDto>.ErrorResponse(
                        $"Course with ID {id} not found.",
                        new List<string> { "Course not found" }
                    ));
                }
                else
                {
                    return StatusCode(500, ApiResponse<CourseDto>.ErrorResponse(
                        "A concurrency error occurred while updating the course.",
                        new List<string> { "Concurrency conflict" }
                    ));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<CourseDto>.ErrorResponse(
                    "An error occurred while updating the course.",
                    new List<string> { ex.Message }
                ));
            }
        }

        // DELETE: api/courses/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteCourse(int id)
        {
            try
            {
                var course = await _context.Courses.FindAsync(id);
                if (course == null)
                {
                    return NotFound(ApiResponse<bool>.ErrorResponse(
                        $"Course with ID {id} not found.",
                        new List<string> { "Course not found" }
                    ));
                }

                var courseTitle = course.Title;

                // Delete associated image file if it exists
                if (!string.IsNullOrEmpty(course.TeachingMaterialImagePath))
                {
                    DeleteFile(course.TeachingMaterialImagePath);
                }

                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();

                // Send notification
                _notificationService.SendNotification("Course", id.ToString(), courseTitle, EntityOperation.DELETE, "System");

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Course deleted successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<bool>.ErrorResponse(
                    "An error occurred while deleting the course.",
                    new List<string> { ex.Message }
                ));
            }
        }

        // Helper methods

        private async Task<bool> CourseExists(int id)
        {
            return await _context.Courses.AnyAsync(e => e.CourseID == id);
        }

        private (bool IsValid, List<string> Errors) ValidateFile(IFormFile file)
        {
            var errors = new List<string>();

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
            {
                errors.Add("Please upload a valid image file (jpg, jpeg, png, gif, bmp).");
            }

            // Validate file size (max 5MB)
            if (file.Length > 5 * 1024 * 1024)
            {
                errors.Add("File size must be less than 5MB.");
            }

            return (errors.Count == 0, errors);
        }

        private async Task<string> SaveFileAsync(IFormFile file, int courseId)
        {
            // Create uploads directory if it doesn't exist
            var uploadsPath = Path.Combine(_webHostEnvironment.WebRootPath, "Uploads", "TeachingMaterials");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            // Generate unique filename
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            var fileName = $"course_{courseId}_{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsPath, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"~/Uploads/TeachingMaterials/{fileName}";
        }

        private void DeleteFile(string filePath)
        {
            try
            {
                var physicalPath = Path.Combine(
                    _webHostEnvironment.WebRootPath,
                    filePath.TrimStart('~', '/').Replace('/', Path.DirectorySeparatorChar)
                );

                if (System.IO.File.Exists(physicalPath))
                {
                    System.IO.File.Delete(physicalPath);
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - file deletion failure shouldn't prevent course deletion
                System.Diagnostics.Debug.WriteLine($"Error deleting file: {ex.Message}");
            }
        }
    }
}
