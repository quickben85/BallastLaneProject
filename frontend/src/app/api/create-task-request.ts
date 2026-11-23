export interface CreateTaskRequest {
  title: string;
  description?: string;
  dueDate?: string;
  timeZone?: string;
}
