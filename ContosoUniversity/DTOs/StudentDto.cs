using System;
using System.Collections.Generic;

namespace ContosoUniversity.DTOs
{
    public class StudentDto
    {
        public int ID { get; set; }
        public string LastName { get; set; }
        public string FirstMidName { get; set; }
        public string FullName { get; set; }
        public DateTime EnrollmentDate { get; set; }
        public List<EnrollmentDto> Enrollments { get; set; }

        public StudentDto()
        {
            Enrollments = new List<EnrollmentDto>();
        }
    }

    public class StudentCreateDto
    {
        public string LastName { get; set; }
        public string FirstMidName { get; set; }
        public DateTime EnrollmentDate { get; set; }
    }

    public class StudentUpdateDto
    {
        public string LastName { get; set; }
        public string FirstMidName { get; set; }
        public DateTime EnrollmentDate { get; set; }
    }
}
