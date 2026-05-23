import { Routes, Route } from 'react-router';
import RequireAuth from './components/RequireAuth';
import AuthLayout from './components/AuthLayout';
import AdminLayout from './components/AdminLayout';
import Dashboard from './pages/Dashboard';
import Profile from './pages/Profile';
import Offers from './pages/Offers';
import Tracking from './pages/Tracking';
import KeywordsPage from './pages/KeywordsPage';
import Login from './pages/Login';
import Register from './pages/Register';
import AdminDashboard from './pages/admin/AdminDashboard';
import AdminAiServices from './pages/admin/AiServices';
import AdminAiModels from './pages/admin/AiModels';
import AdminAiPrompts from './pages/admin/AiPrompts';
import AdminAiSchemas from './pages/admin/AiSchemas';
import AdminTemplates from './pages/admin/Templates';
import AdminJobProviders from './pages/admin/JobProviders';
import AdminDedup from './pages/admin/Dedup';
import AdminLogs from './pages/admin/Logs';

export default function App() {
  return (
    <Routes>
      <Route element={<RequireAuth />}>
        <Route element={<AuthLayout />}>
          <Route path="/" element={<Dashboard />} />
          <Route path="/offers" element={<Offers />} />
          <Route path="/profile" element={<Profile />} />
          <Route path="/tracking" element={<Tracking />} />
          <Route path="/keywords" element={<KeywordsPage />} />
        </Route>
        <Route element={<AdminLayout />}>
          <Route path="/admin" element={<AdminDashboard />} />
          <Route path="/admin/ai-services" element={<AdminAiServices />} />
          <Route path="/admin/ai-models" element={<AdminAiModels />} />
          <Route path="/admin/ai-prompts" element={<AdminAiPrompts />} />
          <Route path="/admin/ai-schemas" element={<AdminAiSchemas />} />
          <Route path="/admin/templates" element={<AdminTemplates />} />
          <Route path="/admin/job-providers" element={<AdminJobProviders />} />
          <Route path="/admin/dedup" element={<AdminDedup />} />
          <Route path="/admin/logs" element={<AdminLogs />} />
        </Route>
      </Route>
      <Route path="/login" element={<Login />} />
      <Route path="/register" element={<Register />} />
    </Routes>
  );
}
