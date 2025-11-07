using ContosoUniversity.Models;

namespace ContosoUniversity.DTOs
{
    public class EnrollmentDto
    {
        public int EnrollmentID { get; set; }
        public int CourseID { get; set; }
        public string CourseTitle { get; set; }
        public int StudentID { get; set; }
        public string StudentName { get; set; }
        public Grade? Grade { get; set; }
    }
}
