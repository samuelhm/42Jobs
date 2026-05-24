import { createBrowserRouter, createRoutesFromElements, Route } from 'react-router';
import { RequireAuth, AuthLayout, AdminLayout } from './components';
import { Login, Register, Dashboard, Offers, Profile, Tracking, KeywordsPage } from './pages';
import { AdminDashboard, AiServices, AiModels, AiPrompts, Templates, JobProviders, Dedup, Logs } from './pages/admin';

import { dashboardLoader } from './pages/dashboard/dashboard.loader';
import { offersLoader } from './pages/dashboard/offers.loader';
import { trackingLoader } from './pages/dashboard/tracking.loader';
import { keywordsPageLoader } from './pages/dashboard/keywordsPage.loader';
import { profileLoader } from './pages/profile/profile.loader';
import { loginAction } from './pages/auth/login.action';
import { registerAction } from './pages/auth/register.action';

const routes = createRoutesFromElements(
  <>
    <Route path="/login" element={<Login />} action={loginAction} />
    <Route path="/register" element={<Register />} action={registerAction} />

    <Route element={<RequireAuth />}>
      <Route element={<AuthLayout />}>
        <Route index element={<Dashboard />} loader={dashboardLoader} />
        <Route path="offers" element={<Offers />} loader={offersLoader} />
        <Route path="profile" element={<Profile />} loader={profileLoader} />
        <Route path="tracking" element={<Tracking />} loader={trackingLoader} />
        <Route path="keywords" element={<KeywordsPage />} loader={keywordsPageLoader} />
      </Route>

      <Route element={<AdminLayout />}>
        <Route path="admin" element={<AdminDashboard />} />
        <Route path="admin/ai-services" element={<AiServices />} />
        <Route path="admin/ai-models" element={<AiModels />} />
        <Route path="admin/ai-prompts" element={<AiPrompts />} />
        <Route path="admin/templates" element={<Templates />} />
        <Route path="admin/job-providers" element={<JobProviders />} />
        <Route path="admin/dedup" element={<Dedup />} />
        <Route path="admin/logs" element={<Logs />} />
      </Route>
    </Route>
  </>
);

export const router = createBrowserRouter(routes);
