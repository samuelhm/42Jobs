import { useEffect, useState } from 'react';
import { get } from '../../utils';

let freeTierCache: Promise<boolean> | null = null;

function checkFreeTier(): Promise<boolean> {
  if (!freeTierCache) {
    freeTierCache = get<any[]>('/api/admin/ai-services')
      .then(res => {
        const services = res?.data || [];
        return services.some((s: any) => s.is_free_tier);
      })
      .catch(() => false);
  }
  return freeTierCache;
}

export default function FreeTierBanner() {
  const [show, setShow] = useState(false);

  useEffect(() => {
    checkFreeTier().then(setShow);
  }, []);

  if (!show) return null;

  return (
    <div className="free-tier-banner">
      ⚠️ Free tier API keys detected — AI operations will be slower due to rate limits.
    </div>
  );
}
