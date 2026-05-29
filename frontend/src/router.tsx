import { createBrowserRouter, createRoutesFromElements, Route, useRouteError } from 'react-router';
import { RequireAuth, RequireAdmin, AuthLayout, AdminLayout, PublicLayout } from './components';
import HomeGate from './components/auth/HomeGate';
import { Login, Register, Dashboard, OffersRoute, Profile, Tracking, KeywordsPage, HomePage } from './pages';
import { PrivacyPage, TermsPage, ContactPage, FaqPage } from './pages';
import { AdminDashboard, AiServices, AiModels, AiPrompts, Templates, JobProviders, Utils, DiscardedJobs, BlockedKeywords, Logs } from './pages/admin';

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

function HydrateFallback() {
  return (
    <div style={{
      minHeight: '100vh',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      background: 'var(--bg)',
    }}>
      <h1 style={{
        fontFamily: "'Outfit', sans-serif",
        fontWeight: 800,
        fontSize: '1.8rem',
        color: 'var(--text-bright)',
        animation: 'ledPulse 2s ease-in-out infinite',
      }}>
        42<span style={{ color: 'var(--amber)' }}>jobs</span>
      </h1>
    </div>
  );
}

const routes = createRoutesFromElements(
  <>
    {/* Public */}
    <Route path="/" element={<HomeGate />} />

    <Route element={<PublicLayout />}>
      <Route path="privacy" element={<PrivacyPage />} />
      <Route path="terms" element={<TermsPage />} />
      <Route path="faq" element={<FaqPage />} />
      <Route path="contact" element={<ContactPage />} />
    </Route>

    <Route path="/login" element={<Login />} />
    <Route path="/register" element={<Register />} />

    {/* Auth required */}
    <Route element={<RequireAuth />} errorElement={<ErrorPage />} hydrateFallbackElement={<HydrateFallback />}>
      <Route element={<AuthLayout />} errorElement={<ErrorPage />}>
        <Route path="home" element={<HomePage />} />
        <Route path="dashboard" element={<Dashboard />} loader={dashboardLoader} />
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
          <Route path="admin/utils" element={<Utils />} />
          <Route path="admin/discarded-jobs" element={<DiscardedJobs />} />
          <Route path="admin/blocked-keywords" element={<BlockedKeywords />} />
          <Route path="admin/logs" element={<Logs />} loader={adminLogsLoader} />
        </Route>
      </Route>
    </Route>
  </>
);

export const router = createBrowserRouter(routes);
