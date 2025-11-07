using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ContosoUniversity.DTOs;

namespace ContosoUniversity.Middleware
{
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

        public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Log the exception with full details
            _logger.LogError(exception, 
                "An unhandled exception occurred. Path: {Path}, Method: {Method}, User: {User}",
                context.Request.Path,
                context.Request.Method,
                context.User?.Identity?.Name ?? "Anonymous");

            // Determine status code and error message based on exception type
            var (statusCode, message, errors) = MapExceptionToResponse(exception);

            // Set response properties
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            // Create error response
            var errorResponse = ApiResponse<object>.ErrorResponse(message, errors);

            // Serialize and write response
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(errorResponse, jsonOptions);
            await context.Response.WriteAsync(json);
        }

        private (HttpStatusCode statusCode, string message, System.Collections.Generic.List<string> errors) MapExceptionToResponse(Exception exception)
        {
            var errors = new System.Collections.Generic.List<string>();

            switch (exception)
            {
                case DbUpdateConcurrencyException concurrencyEx:
                    // Concurrency conflict
                    errors.Add("The record you attempted to edit was modified by another user after you got the original value.");
                    errors.Add("Please refresh and try again.");
                    return (HttpStatusCode.Conflict, "Concurrency conflict detected", errors);

                case DbUpdateException dbUpdateEx:
                    // Database update errors
                    _logger.LogError(dbUpdateEx, "Database update error occurred");
                    
                    // Check for common database constraint violations
                    var innerMessage = dbUpdateEx.InnerException?.Message ?? dbUpdateEx.Message;
                    
                    if (innerMessage.Contains("duplicate key") || innerMessage.Contains("UNIQUE constraint"))
                    {
                        errors.Add("A record with this value already exists.");
                        return (HttpStatusCode.Conflict, "Duplicate record", errors);
                    }
                    
                    if (innerMessage.Contains("FOREIGN KEY constraint") || innerMessage.Contains("foreign key"))
                    {
                        errors.Add("This operation would violate a foreign key constraint.");
                        errors.Add("Please ensure all related records exist.");
                        return (HttpStatusCode.BadRequest, "Foreign key constraint violation", errors);
                    }

                    errors.Add("An error occurred while updating the database.");
                    return (HttpStatusCode.InternalServerError, "Database update failed", errors);

                case UnauthorizedAccessException unauthorizedEx:
                    // Unauthorized access
                    errors.Add("You do not have permission to perform this action.");
                    return (HttpStatusCode.Forbidden, "Access denied", errors);

                case ArgumentNullException argNullEx:
                    // Null argument
                    errors.Add($"Required parameter '{argNullEx.ParamName}' is missing.");
                    return (HttpStatusCode.BadRequest, "Invalid request", errors);

                case ArgumentException argEx:
                    // Invalid argument
                    errors.Add(argEx.Message);
                    return (HttpStatusCode.BadRequest, "Invalid argument", errors);

                case InvalidOperationException invalidOpEx:
                    // Invalid operation
                    errors.Add(invalidOpEx.Message);
                    return (HttpStatusCode.BadRequest, "Invalid operation", errors);

                case KeyNotFoundException notFoundEx:
                    // Resource not found
                    errors.Add(notFoundEx.Message);
                    return (HttpStatusCode.NotFound, "Resource not found", errors);

                case NotImplementedException notImplementedEx:
                    // Not implemented
                    errors.Add("This feature is not yet implemented.");
                    return (HttpStatusCode.NotImplemented, "Not implemented", errors);

                case TimeoutException timeoutEx:
                    // Timeout
                    errors.Add("The operation timed out. Please try again.");
                    return (HttpStatusCode.RequestTimeout, "Request timeout", errors);

                default:
                    // Generic server error
                    errors.Add("An unexpected error occurred. Please try again later.");
                    
                    // In development, include more details
                    #if DEBUG
                    errors.Add($"Exception type: {exception.GetType().Name}");
                    errors.Add($"Message: {exception.Message}");
                    #endif
                    
                    return (HttpStatusCode.InternalServerError, "Internal server error", errors);
            }
        }
    }
}
