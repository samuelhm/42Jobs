import { Link, Outlet } from 'react-router';
import { useAuth } from '../../context';
import Footer from './Footer';

export default function PublicLayout() {
  const { user } = useAuth();

  return (
    <div className="public-layout">
      <header className="public-header">
        <Link to="/" className="layout-logo">
          42<span className="accent">jobs</span>
        </Link>
        <div className="public-header-right">
          {user ? (
            <>
              <Link to="/home" className="landing-btn landing-btn-secondary" style={{ padding: '0.35rem 0.9rem', fontSize: '0.72rem' }}>
                Dashboard
              </Link>
              <Link to="/profile" className="landing-btn landing-btn-ghost" style={{ padding: '0.35rem 0.9rem', fontSize: '0.72rem' }}>
                Profile
              </Link>
            </>
          ) : (
            <>
              <Link to="/login" className="landing-btn landing-btn-ghost" style={{ padding: '0.35rem 0.9rem', fontSize: '0.72rem' }}>
                Login
              </Link>
              <Link to="/register" className="landing-btn landing-btn-primary" style={{ padding: '0.35rem 0.9rem', fontSize: '0.72rem' }}>
                Register
              </Link>
            </>
          )}
        </div>
      </header>
      <main className="public-main">
        <Outlet />
      </main>
      <Footer />
    </div>
  );
}
