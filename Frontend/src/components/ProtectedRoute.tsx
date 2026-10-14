import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '@/contexts/AuthContext';
import { LoadingSpinner } from './Loading';

export function ProtectedRoute({
  children,
  roles,
}: {
  children: React.ReactNode;
  roles?: string[];
}) {
  const { isAuthenticated, loading, user } = useAuth();
  const location = useLocation();

  if (loading) return <LoadingSpinner fullPage label="Oturum kontrol ediliyor..." />;
  if (!isAuthenticated) {
    return <Navigate to="/giris" replace state={{ from: location.pathname }}/>;
  }
 
  if (roles && user?.Rol && !roles.includes(String(user.Rol))) {
    return <Navigate to="/" replace />;
  }
  return <>{children}</>;
}
