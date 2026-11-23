import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { TASKS_API_ORIGIN } from './api-origins';
import { CreateTaskRequest } from './create-task-request';
import { TaskSummary } from './task-summary';
import { TasksClient } from './tasks-client';

@Injectable()
export class TasksApi implements TasksClient {
  private readonly http = inject(HttpClient);
  private readonly endpoint = `${TASKS_API_ORIGIN}/api/tasks`;

  create(request: CreateTaskRequest) {
    return this.http.post<TaskSummary>(this.endpoint, request);
  }

  list() {
    return this.http.get<TaskSummary[]>(this.endpoint);
  }
}
