import { useState } from 'react';
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router';

const navItems = [
  { to: '/', label: 'Dashboard' },
  { to: '/offers', label: 'Offers' },
  { to: '/profile', label: 'Profile' },
  { to: '/tracking', label: 'Tracking' },
];

export default function AuthLayout() {
  const navigate = useNavigate();
  const location = useLocation();
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
        <aside className="layout-sidebar">
          <nav>
            {navItems.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.to === '/'}
                className={({ isActive }) => `sidebar-link${isActive ? ' active' : ''}`}
              >
                {item.label}
              </NavLink>
            ))}
          </nav>
        </aside>

        <main className="layout-main">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
