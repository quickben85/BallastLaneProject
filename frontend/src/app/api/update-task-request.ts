import { TaskSummary } from './task-summary';

export interface UpdateTaskRequest {
  title: string;
  description: string | null;
  dueDate: string | null;
  timeZone?: string;
  status: TaskSummary['status'];
}
