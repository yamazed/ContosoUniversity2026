using System;

namespace ContosoUniversity.DTOs
{
    public class DepartmentDto
    {
        public int DepartmentID { get; set; }
        public string Name { get; set; }
        public decimal Budget { get; set; }
        public DateTime StartDate { get; set; }
        public int? InstructorID { get; set; }
        public string AdministratorName { get; set; }
        public byte[] RowVersion { get; set; }
    }

    public class DepartmentCreateDto
    {
        public string Name { get; set; }
        public decimal Budget { get; set; }
        public DateTime StartDate { get; set; }
        public int? InstructorID { get; set; }
    }

    public class DepartmentUpdateDto
    {
        public string Name { get; set; }
        public decimal Budget { get; set; }
        public DateTime StartDate { get; set; }
        public int? InstructorID { get; set; }
        public byte[] RowVersion { get; set; }
    }
}
