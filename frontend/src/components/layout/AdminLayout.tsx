import { NavLink, Outlet, useNavigate } from 'react-router';
import { useState } from 'react';
import Footer from './Footer';

const adminNav = [
  { to: '/admin', label: 'Dashboard', end: true },
  { to: '/admin/ai-services', label: 'AI Services' },
  { to: '/admin/ai-models', label: 'AI Models' },
  { to: '/admin/ai-prompts', label: 'Prompts' },
  { to: '/admin/templates', label: 'CV Templates' },
  { to: '/admin/job-providers', label: 'Job Providers' },
  { to: '/admin/utils', label: 'Utils' },
  { to: '/admin/discarded-jobs', label: 'Discarded' },
  { to: '/admin/logs', label: 'Logs' },
];

export default function AdminLayout() {
  const navigate = useNavigate();
  const [sidebarOpen, setSidebarOpen] = useState(false);

  function closeSidebar() {
    setSidebarOpen(false);
  }

  return (
    <div className="admin-layout">
      <header className="admin-header">
        <div className="admin-header-left">
          <button
            className="admin-menu-btn"
            onClick={() => setSidebarOpen(o => !o)}
            aria-label={sidebarOpen ? 'Close sidebar' : 'Open sidebar'}
          >
            {sidebarOpen ? '\u2715' : '\u2630'}
          </button>
          <NavLink to="/admin" className="layout-logo">
            42<span className="accent">jobs</span> Admin
          </NavLink>
        </div>
        <div className="admin-header-right">
          <button className="logout-btn" onClick={() => navigate('/home')}>
            Back to App
          </button>
        </div>
      </header>
      <div className={`admin-body${sidebarOpen ? ' sidebar-open' : ''}`}>
        <div className="admin-sidebar-overlay" onClick={closeSidebar} />
        <nav className="admin-sidebar" onClick={closeSidebar}>
          {adminNav.map(item => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              className={({ isActive }) => `admin-nav-link${isActive ? ' active' : ''}`}
            >
              {item.label}
            </NavLink>
          ))}
        </nav>
        <main className="admin-content">
          <Outlet />
        </main>
      </div>

      <Footer />
    </div>
  );
}
