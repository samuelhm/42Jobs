import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { RouterProvider } from 'react-router';
import { AuthProvider } from './context';
import { router } from './router';
import CookieConsent from './components/ui/CookieConsent';
import './index.css';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AuthProvider>
      <RouterProvider router={router} />
      <CookieConsent />
    </AuthProvider>
  </StrictMode>,
);
