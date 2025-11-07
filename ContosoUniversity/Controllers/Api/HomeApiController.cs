using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Data;
using ContosoUniversity.DTOs;
using ContosoUniversity.Models.SchoolViewModels;

namespace ContosoUniversity.Controllers.Api
{
    [Route("api/home")]
    [ApiController]
    public class HomeApiController : ControllerBase
    {
        private readonly SchoolContext _context;

        public HomeApiController(SchoolContext context)
        {
            _context = context;
        }

        // GET: api/home/enrollment-stats
        [HttpGet("enrollment-stats")]
        public async Task<ActionResult<ApiResponse<List<EnrollmentDateGroup>>>> GetEnrollmentStatistics()
        {
            try
            {
                var data = await _context.Students
                    .GroupBy(s => s.EnrollmentDate)
                    .Select(dateGroup => new EnrollmentDateGroup
                    {
                        EnrollmentDate = dateGroup.Key,
                        StudentCount = dateGroup.Count()
                    })
                    .OrderBy(x => x.EnrollmentDate)
                    .ToListAsync();

                return Ok(ApiResponse<List<EnrollmentDateGroup>>.SuccessResponse(data));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<EnrollmentDateGroup>>.ErrorResponse(
                    "An error occurred while retrieving enrollment statistics.",
                    new List<string> { ex.Message }
                ));
            }
        }
    }
}
