import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { TASKS_API_ORIGIN } from './api-origins';
import { CreateTaskRequest } from './create-task-request';
import { TaskSummary } from './task-summary';
import { TasksClient } from './tasks-client';
import { UpdateTaskRequest } from './update-task-request';

@Injectable()
export class TasksApi implements TasksClient {
  private readonly http = inject(HttpClient);
  private readonly endpoint = `${TASKS_API_ORIGIN}/api/tasks`;

  create(request: CreateTaskRequest) {
    return this.http.post<TaskSummary>(this.endpoint, request);
  }

  update(id: string, request: UpdateTaskRequest) {
    return this.http.put<TaskSummary>(`${this.endpoint}/${encodeURIComponent(id)}`, request);
  }

  delete(id: string) {
    return this.http.delete<void>(`${this.endpoint}/${encodeURIComponent(id)}`);
  }

  list() {
    return this.http.get<TaskSummary[]>(this.endpoint);
  }
}
