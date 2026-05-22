import { Routes, Route } from 'react-router';
import RequireAuth from './components/RequireAuth';
import AuthLayout from './components/AuthLayout';
import Dashboard from './pages/Dashboard';
import Profile from './pages/Profile';
import Offers from './pages/Offers';
import Tracking from './pages/Tracking';
import KeywordsPage from './pages/KeywordsPage';
import Login from './pages/Login';
import Register from './pages/Register';

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
      </Route>
      <Route path="/login" element={<Login />} />
      <Route path="/register" element={<Register />} />
    </Routes>
  );
}
