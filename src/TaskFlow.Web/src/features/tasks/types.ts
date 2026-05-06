export const TaskStatus = {
  Pending: 0,
  InProgress: 1,
  Done: 2,
  Cancelled: 3,
} as const;

export type TaskStatus = (typeof TaskStatus)[keyof typeof TaskStatus];

export const TASK_STATUS_OPTIONS: ReadonlyArray<{ value: TaskStatus; label: string }> = [
  { value: TaskStatus.Pending, label: 'Pending' },
  { value: TaskStatus.InProgress, label: 'In progress' },
  { value: TaskStatus.Done, label: 'Done' },
  { value: TaskStatus.Cancelled, label: 'Cancelled' },
];

export function taskStatusLabel(status: TaskStatus): string {
  return TASK_STATUS_OPTIONS.find((o) => o.value === status)?.label ?? 'Unknown';
}

export interface TaskDto {
  id: string;
  title: string;
  description: string | null;
  status: TaskStatus;
  dueDateUtc: string;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateTaskRequest {
  title: string;
  description: string | null;
  dueDateUtc: string;
}

export interface UpdateTaskRequest extends CreateTaskRequest {
  status: TaskStatus;
}
