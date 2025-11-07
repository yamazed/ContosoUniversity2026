using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ContosoUniversity.DTOs;

namespace ContosoUniversity.Filters
{
    public class ValidateModelStateFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .SelectMany(x => x.Value.Errors.Select(e => 
                        $"{x.Key}: {(string.IsNullOrEmpty(e.ErrorMessage) ? e.Exception?.Message : e.ErrorMessage)}"))
                    .ToList();

                var errorResponse = ApiResponse<object>.ErrorResponse(
                    "Validation failed",
                    errors
                );

                context.Result = new BadRequestObjectResult(errorResponse);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // No action needed after execution
        }
    }
}
