export interface OfficeAssignment {
  instructorID: number;
  location: string;
}

export interface CourseAssignment {
  instructorID: number;
  courseID: number;
  courseTitle?: string;
}

export interface Instructor {
  id: number;
  lastName: string;
  firstMidName: string;
  fullName: string;
  hireDate: string;
  officeAssignment?: OfficeAssignment;
  courseAssignments: CourseAssignment[];
}

export interface InstructorCreate {
  lastName: string;
  firstMidName: string;
  hireDate: string;
  officeAssignment?: {
    location: string;
  };
  courseIDs?: number[];
}

export interface InstructorUpdate {
  id: number;
  lastName: string;
  firstMidName: string;
  hireDate: string;
  officeAssignment?: {
    location: string;
  };
  courseIDs?: number[];
}
