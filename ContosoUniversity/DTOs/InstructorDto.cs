using System;
using System.Collections.Generic;

namespace ContosoUniversity.DTOs
{
    public class InstructorDto
    {
        public int ID { get; set; }
        public string LastName { get; set; }
        public string FirstMidName { get; set; }
        public string FullName { get; set; }
        public DateTime HireDate { get; set; }
        public OfficeAssignmentDto OfficeAssignment { get; set; }
        public List<CourseAssignmentDto> CourseAssignments { get; set; }

        public InstructorDto()
        {
            CourseAssignments = new List<CourseAssignmentDto>();
        }
    }

    public class InstructorCreateDto
    {
        public string LastName { get; set; }
        public string FirstMidName { get; set; }
        public DateTime HireDate { get; set; }
        public string OfficeLocation { get; set; }
        public List<int> CourseIDs { get; set; }

        public InstructorCreateDto()
        {
            CourseIDs = new List<int>();
        }
    }

    public class InstructorUpdateDto
    {
        public string LastName { get; set; }
        public string FirstMidName { get; set; }
        public DateTime HireDate { get; set; }
        public string OfficeLocation { get; set; }
        public List<int> CourseIDs { get; set; }

        public InstructorUpdateDto()
        {
            CourseIDs = new List<int>();
        }
    }
}
