import { createContext, useContext } from 'react';
import type { Kullanici, LoginDto, RegisterDto } from '@/types';

export interface AuthContextValue {
  user: Kullanici | null;
  loading: boolean;
  login: (dto: LoginDto) => Promise<Kullanici>;
  register: (dto: RegisterDto) => Promise<void>;
  logout: () => Promise<void>;
  isAuthenticated: boolean;
  hasRole: (...roles: string[]) => boolean;
}

export const AuthContext = createContext<AuthContextValue | null>(null);

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
