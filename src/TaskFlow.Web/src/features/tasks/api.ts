import { apiClient } from '@/shared/apiClient';
import type { CreateTaskRequest, TaskDto, UpdateTaskRequest } from './types';

export async function listTasks(): Promise<TaskDto[]> {
  const { data } = await apiClient.get<TaskDto[]>('/tasks');
  return data;
}

export async function createTask(payload: CreateTaskRequest): Promise<TaskDto> {
  const { data } = await apiClient.post<TaskDto>('/tasks', payload);
  return data;
}

export async function updateTask(id: string, payload: UpdateTaskRequest): Promise<TaskDto> {
  const { data } = await apiClient.put<TaskDto>(`/tasks/${id}`, payload);
  return data;
}

export async function deleteTask(id: string): Promise<void> {
  await apiClient.delete(`/tasks/${id}`);
}
