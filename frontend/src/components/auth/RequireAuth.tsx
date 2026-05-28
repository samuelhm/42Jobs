import { Navigate, Outlet } from 'react-router';
import { useAuth } from '../../context';

export default function RequireAuth() {
  const { user, loading } = useAuth();
  if (loading) return null;
  if (!user) return <Navigate to="/login" replace />;
  return <Outlet />;
}

export function RequireAdmin() {
  const { user, loading } = useAuth();
  if (loading) return null;
  if (!user || user.role !== 'Admin') return <Navigate to="/" replace />;
  return <Outlet />;
}
