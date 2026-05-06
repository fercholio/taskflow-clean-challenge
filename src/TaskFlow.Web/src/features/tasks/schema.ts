import { z } from 'zod';
import { TaskStatus } from './types';

export const taskFormSchema = z.object({
  title: z.string().min(1, 'Title is required').max(200, 'Title too long'),
  description: z.string().max(2000, 'Description too long').optional().or(z.literal('')),
  dueDate: z.string().min(1, 'Due date is required'),
  status: z.coerce.number().int().min(0).max(3),
});

export type TaskFormValues = z.infer<typeof taskFormSchema>;

export const DEFAULT_TASK_FORM: TaskFormValues = {
  title: '',
  description: '',
  dueDate: new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString().slice(0, 16),
  status: TaskStatus.Pending,
};
