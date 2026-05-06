import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { DEFAULT_TASK_FORM, taskFormSchema, type TaskFormValues } from './schema';
import { TASK_STATUS_OPTIONS, type TaskDto } from './types';

interface TaskFormProps {
  initial?: TaskDto | null;
  showStatus?: boolean;
  submitLabel: string;
  onSubmit: (values: TaskFormValues) => Promise<void> | void;
  onCancel?: () => void;
}

function toLocalInputValue(iso: string): string {
  const d = new Date(iso);
  const pad = (n: number) => n.toString().padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

export function TaskForm({ initial, showStatus = false, submitLabel, onSubmit, onCancel }: TaskFormProps): JSX.Element {
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<TaskFormValues>({
    resolver: zodResolver(taskFormSchema),
    defaultValues: initial
      ? {
          title: initial.title,
          description: initial.description ?? '',
          dueDate: toLocalInputValue(initial.dueDateUtc),
          status: initial.status,
        }
      : DEFAULT_TASK_FORM,
  });

  useEffect(() => {
    if (initial) {
      reset({
        title: initial.title,
        description: initial.description ?? '',
        dueDate: toLocalInputValue(initial.dueDateUtc),
        status: initial.status,
      });
    }
  }, [initial, reset]);

  const submit = handleSubmit(async (values) => {
    await onSubmit(values);
    if (!initial) reset(DEFAULT_TASK_FORM);
  });

  return (
    <form onSubmit={submit} className="space-y-4" noValidate>
      <div>
        <label htmlFor="title" className="label">Title</label>
        <input id="title" className="input" {...register('title')} />
        {errors.title && <p className="mt-1 text-xs text-red-600">{errors.title.message}</p>}
      </div>

      <div>
        <label htmlFor="description" className="label">Description</label>
        <textarea id="description" rows={3} className="input" {...register('description')} />
        {errors.description && <p className="mt-1 text-xs text-red-600">{errors.description.message}</p>}
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div>
          <label htmlFor="dueDate" className="label">Due date</label>
          <input id="dueDate" type="datetime-local" className="input" {...register('dueDate')} />
          {errors.dueDate && <p className="mt-1 text-xs text-red-600">{errors.dueDate.message}</p>}
        </div>

        {showStatus && (
          <div>
            <label htmlFor="status" className="label">Status</label>
            <select id="status" className="input" {...register('status')}>
              {TASK_STATUS_OPTIONS.map((o) => (
                <option key={o.value} value={o.value}>{o.label}</option>
              ))}
            </select>
          </div>
        )}
      </div>

      <div className="flex justify-end gap-2">
        {onCancel && (
          <button type="button" className="btn-secondary" onClick={onCancel} disabled={isSubmitting}>
            Cancel
          </button>
        )}
        <button type="submit" className="btn-primary" disabled={isSubmitting}>
          {isSubmitting ? 'Saving…' : submitLabel}
        </button>
      </div>
    </form>
  );
}
