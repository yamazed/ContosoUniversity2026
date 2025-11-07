export interface Department {
  departmentID: number;
  name: string;
  budget: number;
  startDate: string;
  instructorID?: number;
  administratorName?: string;
  rowVersion?: string;
}

export interface DepartmentCreate {
  name: string;
  budget: number;
  startDate: string;
  instructorID?: number;
}

export interface DepartmentUpdate {
  departmentID: number;
  name: string;
  budget: number;
  startDate: string;
  instructorID?: number;
  rowVersion?: string;
}
