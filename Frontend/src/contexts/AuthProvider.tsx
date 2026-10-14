import { useCallback, useEffect, useState } from 'react';
import { AuthContext, type AuthContextValue } from './AuthContext';
import { authApi, clearAuth, getStoredUser } from '@/services/api';
import { pickToken } from '@/lib/json';
import { userFromToken } from '@/lib/jwt';
import type { Kullanici, LoginDto, RegisterDto } from '@/types';

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<Kullanici | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const token = sessionStorage.getItem('token');
    const stored = getStoredUser() as Kullanici | null;
    if (token) {
      const fromJwt = userFromToken(token, stored ?? undefined);
      setUser(fromJwt ?? stored);
    }
    setLoading(false);
  }, []);

  const login = useCallback(async (dto: LoginDto) => {
    const res = await authApi.login(dto);
    const token = pickToken(res.data);
    if (!token) {
      throw new Error('Sunucudan token alınamadı.');
    }
    const session = userFromToken(token, { Email: dto.Email });
    if (!session) throw new Error('Token çözümlenemedi.');
    sessionStorage.setItem('token', token);
    sessionStorage.setItem('user', JSON.stringify(session));
    setUser(session);
    return session;
  }, []);

  const register = useCallback(async (dto: RegisterDto) => {
    await authApi.register(dto);
  }, []);

  const logout = useCallback(async () => {
    try {
      await authApi.logout();
    } catch {
      /* token zaten geçersiz olabilir */
    }
    clearAuth();
    setUser(null);
  }, []);

  const hasRole = useCallback(
    (...roles: string[]) => Boolean(user?.Rol && roles.includes(String(user.Rol))),
    [user],
  );

  const value: AuthContextValue = {
    user,
    loading,
    login,
    register,
    logout,
    isAuthenticated: user !== null,
    hasRole,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
