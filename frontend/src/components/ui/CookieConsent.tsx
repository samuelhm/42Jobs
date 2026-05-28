import { useEffect, useState } from 'react';

const COOKIE_KEY = '42jobs-cookie-consent';

export default function CookieConsent() {
  const [visible, setVisible] = useState(false);
  const [hiding, setHiding] = useState(false);

  useEffect(() => {
    if (!localStorage.getItem(COOKIE_KEY))
      setVisible(true);
  }, []);

  function accept() {
    localStorage.setItem(COOKIE_KEY, '1');
    setHiding(true);
    setTimeout(() => setVisible(false), 300);
  }

  if (!visible) return null;

  return (
    <div className={`cookie-consent${hiding ? ' cookie-consent-hidden' : ''}`}>
      <span>
        This site uses a functional cookie for authentication. No tracking, no ads, no third-party cookies.
      </span>
      <button className="cookie-btn" onClick={accept}>Got it</button>
    </div>
  );
}
