export interface TaskSummary {
  id: string;
  title: string;
  description: string | null;
  status: 'open' | 'completed';
  dueDate: string | null;
}
