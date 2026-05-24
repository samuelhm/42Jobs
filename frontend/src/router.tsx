import { createBrowserRouter, createRoutesFromElements, Route, useRouteError } from 'react-router';
import { RequireAuth, RequireAdmin, AuthLayout, AdminLayout } from './components';
import { Login, Register, Dashboard, OffersRoute, Profile, Tracking, KeywordsPage } from './pages';
import { AdminDashboard, AiServices, AiModels, AiPrompts, Templates, JobProviders, Dedup, Logs } from './pages/admin';

import { dashboardLoader } from './pages/dashboard/dashboard.loader';
import { offersLoader } from './pages/dashboard/offers.loader';
import { trackingLoader } from './pages/dashboard/tracking.loader';
import { keywordsPageLoader } from './pages/dashboard/keywordsPage.loader';
import { profileLoader } from './pages/profile/profile.loader';
import { adminLogsLoader } from './pages/admin/admin.loaders';

function ErrorPage() {
  const error = useRouteError() as { statusText?: string; message?: string } | undefined;
  console.error('Route error:', error);
  return (
    <div className="empty-state" style={{ padding: '4rem 2rem', textAlign: 'center' }}>
      <h2>Oops!</h2>
      <p style={{ color: 'var(--text-dim)', marginTop: '1rem' }}>
        {error?.statusText || error?.message || 'An unexpected error occurred.'}
      </p>
      <a href="/" style={{ display: 'inline-block', marginTop: '1.5rem' }}>Go back home</a>
    </div>
  );
}

const routes = createRoutesFromElements(
  <>
    <Route path="/login" element={<Login />} />
    <Route path="/register" element={<Register />} />

    <Route element={<RequireAuth />} errorElement={<ErrorPage />}>
      <Route element={<AuthLayout />} errorElement={<ErrorPage />}>
        <Route index element={<Dashboard />} loader={dashboardLoader} />
        <Route path="offers" element={<OffersRoute />} loader={offersLoader} />
        <Route path="profile" element={<Profile />} loader={profileLoader} />
        <Route path="tracking" element={<Tracking />} loader={trackingLoader} />
        <Route path="keywords" element={<KeywordsPage />} loader={keywordsPageLoader} />
      </Route>

      <Route element={<RequireAdmin />}>
        <Route element={<AdminLayout />} errorElement={<ErrorPage />}>
          <Route path="admin" element={<AdminDashboard />} />
          <Route path="admin/ai-services" element={<AiServices />} />
          <Route path="admin/ai-models" element={<AiModels />} />
          <Route path="admin/ai-prompts" element={<AiPrompts />} />
          <Route path="admin/templates" element={<Templates />} />
          <Route path="admin/job-providers" element={<JobProviders />} />
          <Route path="admin/dedup" element={<Dedup />} />
          <Route path="admin/logs" element={<Logs />} loader={adminLogsLoader} />
        </Route>
      </Route>
    </Route>
  </>
);

export const router = createBrowserRouter(routes);
