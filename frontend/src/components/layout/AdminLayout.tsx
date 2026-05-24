import { NavLink, Outlet, useNavigate } from 'react-router';

const adminNav = [
  { to: '/admin', label: 'Dashboard', end: true },
  { to: '/admin/ai-services', label: 'AI Services' },
  { to: '/admin/ai-models', label: 'AI Models' },
  { to: '/admin/ai-prompts', label: 'Prompts' },
  { to: '/admin/templates', label: 'CV Templates' },
  { to: '/admin/job-providers', label: 'Job Providers' },
  { to: '/admin/utils', label: 'Utils' },
  { to: '/admin/logs', label: 'Logs' },
];

export default function AdminLayout() {
  const navigate = useNavigate();

  return (
    <div className="admin-layout">
      <header className="admin-header">
        <NavLink to="/admin" className="layout-logo">
          42<span className="accent">jobs</span> Admin
        </NavLink>
        <div className="admin-header-right">
          <button className="logout-btn" onClick={() => navigate('/')}>
            Back to App
          </button>
        </div>
      </header>
      <div className="admin-body">
        <nav className="admin-sidebar">
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
    </div>
  );
}
