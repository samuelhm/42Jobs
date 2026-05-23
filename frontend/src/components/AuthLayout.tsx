import { useState } from 'react';
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router';
import { ToastProvider } from '../context/ToastContext';
import { useAuth } from '../context/AuthContext';
import ToastContainer from './ToastContainer';

const navItems = [
  { to: '/', label: 'Dashboard' },
  { to: '/offers', label: 'Offers' },
  { to: '/profile', label: 'Profile' },
  { to: '/tracking', label: 'Tracking' },
  { to: '/keywords', label: 'Keywords' },
];

function AuthLayoutInner() {
  const navigate = useNavigate();
  const location = useLocation();
  const { user } = useAuth();
  const [loggingOut, setLoggingOut] = useState(false);

  async function handleLogout() {
    setLoggingOut(true);
    try {
      await fetch('/api/users/logout', { method: 'POST' });
    } finally {
      navigate('/login', { replace: true });
    }
  }

  return (
    <div className="auth-layout">
      <header className="layout-header">
        <NavLink to="/" className="layout-logo">
          42<span className="accent">jobs</span>
        </NavLink>

        <nav className="layout-nav-desktop">
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === '/'}
              className={({ isActive }) => `layout-nav-link${isActive ? ' active' : ''}`}
            >
              {item.label}
            </NavLink>
          ))}
        </nav>

        <div className="layout-header-right">
          <select
            className="layout-nav-mobile"
            value={location.pathname}
            onChange={(e) => navigate(e.target.value)}
          >
            {navItems.map((item) => (
              <option key={item.to} value={item.to}>
                {item.label}
              </option>
            ))}
          </select>

          {user?.role === 'Admin' && (
            <NavLink to="/admin" className="admin-btn">
              Admin
            </NavLink>
          )}

          <button
            className="logout-btn"
            onClick={handleLogout}
            disabled={loggingOut}
          >
            {loggingOut ? '...' : 'Logout'}
          </button>
        </div>
      </header>

      <div className="layout-body">
        <Outlet />
      </div>
    </div>
  );
}

export default function AuthLayout() {
  return (
    <ToastProvider>
      <AuthLayoutInner />
      <ToastContainer />
    </ToastProvider>
  );
}
