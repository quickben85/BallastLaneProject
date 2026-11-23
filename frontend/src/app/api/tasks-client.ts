import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateTaskRequest } from './create-task-request';
import { TaskSummary } from './task-summary';

export interface TasksClient {
  create(request: CreateTaskRequest): Observable<TaskSummary>;
  list(): Observable<TaskSummary[]>;
}

export const TASKS_API = new InjectionToken<TasksClient>('Tasks API');
