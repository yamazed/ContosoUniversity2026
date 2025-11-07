export interface Course {
  courseID: number;
  title: string;
  credits: number;
  departmentID: number;
  departmentName?: string;
  teachingMaterialImagePath?: string;
}

export interface CourseCreate {
  courseID: number;
  title: string;
  credits: number;
  departmentID: number;
  teachingMaterialImage?: File;
}

export interface CourseUpdate {
  courseID: number;
  title: string;
  credits: number;
  departmentID: number;
  teachingMaterialImage?: File;
}
