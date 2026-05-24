import { useEffect, useState } from 'react';
import { fetchWithAuth } from '../../utils';

export default function FreeTierBanner() {
  const [show, setShow] = useState(false);

  useEffect(() => {
    fetchWithAuth('/api/admin/ai-services')
      .then(r => r.json().catch(() => ({})))
      .then(d => {
        const services = d?.data || [];
        if (services.some((s: any) => s.is_free_tier))
          setShow(true);
      })
      .catch(() => {});
  }, []);

  if (!show) return null;

  return (
    <div className="free-tier-banner">
      ⚠️ Free tier API keys detected — AI operations will be slower due to rate limits.
    </div>
  );
}
