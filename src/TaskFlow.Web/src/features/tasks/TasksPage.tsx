import { useState } from 'react';
import { extractErrorMessage } from '@/shared/apiClient';
import { TaskForm } from './TaskForm';
import { useCreateTask, useDeleteTask, useTasks, useUpdateTask } from './hooks';
import { taskStatusLabel, TaskStatus, type TaskDto } from './types';
import type { TaskFormValues } from './schema';

function toIsoUtc(localDateTime: string): string {
  return new Date(localDateTime).toISOString();
}

function statusBadgeClass(status: TaskStatus): string {
  switch (status) {
    case TaskStatus.Pending:
      return 'bg-slate-100 text-slate-700';
    case TaskStatus.InProgress:
      return 'bg-amber-100 text-amber-800';
    case TaskStatus.Done:
      return 'bg-emerald-100 text-emerald-800';
    case TaskStatus.Cancelled:
      return 'bg-red-100 text-red-800';
    default:
      return 'bg-slate-100 text-slate-700';
  }
}

export function TasksPage(): JSX.Element {
  const tasksQuery = useTasks();
  const createMutation = useCreateTask();
  const updateMutation = useUpdateTask();
  const deleteMutation = useDeleteTask();
  const [editing, setEditing] = useState<TaskDto | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handleCreate = async (values: TaskFormValues): Promise<void> => {
    setError(null);
    try {
      await createMutation.mutateAsync({
        title: values.title.trim(),
        description: values.description?.trim() || null,
        dueDateUtc: toIsoUtc(values.dueDate),
      });
    } catch (err) {
      setError(extractErrorMessage(err, 'Failed to create task'));
    }
  };

  const handleUpdate = async (values: TaskFormValues): Promise<void> => {
    if (!editing) return;
    setError(null);
    try {
      await updateMutation.mutateAsync({
        id: editing.id,
        payload: {
          title: values.title.trim(),
          description: values.description?.trim() || null,
          dueDateUtc: toIsoUtc(values.dueDate),
          status: values.status as TaskStatus,
        },
      });
      setEditing(null);
    } catch (err) {
      setError(extractErrorMessage(err, 'Failed to update task'));
    }
  };

  const handleDelete = async (task: TaskDto): Promise<void> => {
    if (!confirm(`Delete task "${task.title}"?`)) return;
    setError(null);
    try {
      await deleteMutation.mutateAsync(task.id);
    } catch (err) {
      setError(extractErrorMessage(err, 'Failed to delete task'));
    }
  };

  return (
    <div className="space-y-6">
      <section className="card">
        <h2 className="text-lg font-semibold text-slate-900 mb-4">
          {editing ? 'Edit task' : 'New task'}
        </h2>
        <TaskForm
          key={editing?.id ?? 'new'}
          initial={editing}
          showStatus={editing !== null}
          submitLabel={editing ? 'Save changes' : 'Add task'}
          onSubmit={editing ? handleUpdate : handleCreate}
          onCancel={editing ? () => setEditing(null) : undefined}
        />
      </section>

      {error && (
        <div className="rounded-md bg-red-50 border border-red-200 px-3 py-2 text-sm text-red-700">
          {error}
        </div>
      )}

      <section className="card">
        <h2 className="text-lg font-semibold text-slate-900 mb-4">Your tasks</h2>

        {tasksQuery.isLoading && <p className="text-sm text-slate-500">Loading tasks…</p>}
        {tasksQuery.isError && (
          <p className="text-sm text-red-600">{extractErrorMessage(tasksQuery.error, 'Failed to load tasks')}</p>
        )}
        {tasksQuery.data && tasksQuery.data.length === 0 && (
          <p className="text-sm text-slate-500">No tasks yet. Create your first one above.</p>
        )}

        {tasksQuery.data && tasksQuery.data.length > 0 && (
          <ul className="divide-y divide-slate-200">
            {tasksQuery.data.map((task) => (
              <li key={task.id} className="py-4 flex items-start justify-between gap-4">
                <div className="min-w-0">
                  <div className="flex items-center gap-2">
                    <h3 className="font-medium text-slate-900 truncate">{task.title}</h3>
                    <span className={`text-xs font-medium px-2 py-0.5 rounded ${statusBadgeClass(task.status)}`}>
                      {taskStatusLabel(task.status)}
                    </span>
                  </div>
                  {task.description && (
                    <p className="text-sm text-slate-600 mt-1 whitespace-pre-line">{task.description}</p>
                  )}
                  <p className="text-xs text-slate-500 mt-1">
                    Due {new Date(task.dueDateUtc).toLocaleString()}
                  </p>
                </div>
                <div className="flex gap-2 shrink-0">
                  <button type="button" className="btn-secondary" onClick={() => setEditing(task)}>
                    Edit
                  </button>
                  <button
                    type="button"
                    className="btn-danger"
                    onClick={() => handleDelete(task)}
                    disabled={deleteMutation.isPending}
                  >
                    Delete
                  </button>
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}
