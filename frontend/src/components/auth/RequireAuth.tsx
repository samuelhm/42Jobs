import { Outlet } from 'react-router';
import { AuthProvider, useAuth } from '../../context';

function AuthGate() {
  const { loading } = useAuth();
  if (loading) return null;
  return <Outlet />;
}

export default function RequireAuth() {
  return (
    <AuthProvider>
      <AuthGate />
    </AuthProvider>
  );
}
