import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { setUnauthorizedHandler, tokenStorage } from '@/shared/apiClient';
import type { AuthResponse, AuthSession } from './types';

const SESSION_STORAGE_KEY = 'taskflow.session';

interface AuthContextValue {
  session: AuthSession | null;
  isAuthenticated: boolean;
  signIn: (response: AuthResponse) => void;
  signOut: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

function loadSession(): AuthSession | null {
  const raw = localStorage.getItem(SESSION_STORAGE_KEY);
  if (!raw) return null;
  try {
    const parsed = JSON.parse(raw) as AuthSession;
    if (new Date(parsed.expiresAtUtc).getTime() < Date.now()) {
      return null;
    }
    return parsed;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }): JSX.Element {
  const [session, setSession] = useState<AuthSession | null>(() => loadSession());

  const signOut = useCallback(() => {
    tokenStorage.clear();
    localStorage.removeItem(SESSION_STORAGE_KEY);
    setSession(null);
  }, []);

  const signIn = useCallback((response: AuthResponse) => {
    const next: AuthSession = {
      token: response.accessToken,
      userId: response.userId,
      email: response.email,
      expiresAtUtc: response.expiresAtUtc,
    };
    tokenStorage.set(response.accessToken);
    localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(next));
    setSession(next);
  }, []);

  useEffect(() => {
    setUnauthorizedHandler(() => signOut());
  }, [signOut]);

  const value = useMemo<AuthContextValue>(
    () => ({ session, isAuthenticated: session !== null, signIn, signOut }),
    [session, signIn, signOut],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within an AuthProvider');
  return ctx;
}
