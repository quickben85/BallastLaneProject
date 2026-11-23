import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateTaskRequest } from './create-task-request';
import { TaskSummary } from './task-summary';
import { UpdateTaskRequest } from './update-task-request';

export interface TasksClient {
  create(request: CreateTaskRequest): Observable<TaskSummary>;
  update(id: string, request: UpdateTaskRequest): Observable<TaskSummary>;
  list(): Observable<TaskSummary[]>;
}

export const TASKS_API = new InjectionToken<TasksClient>('Tasks API');
