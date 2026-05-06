import { Outlet } from 'react-router-dom';
import { useAuth } from '@/features/auth/AuthContext';

export function AppLayout(): JSX.Element {
  const { session, signOut } = useAuth();

  return (
    <div className="min-h-screen flex flex-col">
      <header className="bg-white border-b border-slate-200">
        <div className="max-w-4xl mx-auto px-4 py-3 flex items-center justify-between">
          <h1 className="text-xl font-semibold text-brand-700">TaskFlow</h1>
          {session && (
            <div className="flex items-center gap-3 text-sm text-slate-600">
              <span className="hidden sm:inline">{session.email}</span>
              <button type="button" className="btn-secondary" onClick={signOut}>
                Sign out
              </button>
            </div>
          )}
        </div>
      </header>
      <main className="flex-1 max-w-4xl w-full mx-auto px-4 py-6">
        <Outlet />
      </main>
    </div>
  );
}
