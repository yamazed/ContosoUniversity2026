export interface Enrollment {
  enrollmentID: number;
  courseID: number;
  studentID: number;
  grade?: string;
  courseTitle?: string;
}

export interface Student {
  id: number;
  lastName: string;
  firstMidName: string;
  enrollmentDate: string;
  enrollments?: Enrollment[];
}

export interface StudentCreate {
  lastName: string;
  firstMidName: string;
  enrollmentDate: string;
}

export interface StudentUpdate {
  id: number;
  lastName: string;
  firstMidName: string;
  enrollmentDate: string;
}
