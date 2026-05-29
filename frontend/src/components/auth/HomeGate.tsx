import { Navigate } from 'react-router';
import { useAuth } from '../../context';
import LandingPage from '../../pages/public/Landing';

export default function HomeGate() {
  const { user, loading } = useAuth();

  if (loading) return null;
  if (user) return <Navigate to="/home" replace />;
  return <LandingPage />;
}
