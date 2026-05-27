import { useEffect, useRef } from 'react';
import { Chart, DoughnutController, ArcElement, Tooltip } from 'chart.js';
import { fetchWithAuth } from '../../utils';

Chart.register(DoughnutController, ArcElement, Tooltip);

interface CompanyType {
  name: string;
  count: number;
}

interface Props {
  categoryId: string | null;
}

const COLORS: Record<string, string> = {
  'Multinacional': '#4a9eff',
  'Startup': '#4ecf7d',
  'Pyme': '#e6a845',
  'Consultora': '#b870d0',
  'Unknown': '#5a5240',
};

export default function CompanyTypesChart({ categoryId }: Props) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const chartRef = useRef<Chart<'doughnut'> | null>(null);

  useEffect(() => {
    if (!categoryId) return;

    let cancelled = false;

    fetchWithAuth(`/api/categories/${categoryId}/company-types`)
      .then((r) => r.json())
      .then((json) => {
        if (cancelled || !json.success || !canvasRef.current) return;

        const types: CompanyType[] = json.data;
        if (types.length === 0) return;

        if (chartRef.current) chartRef.current.destroy();

        const data = types.map((t) => t.count);
        const labels = types.map((t) => t.name);
        const colors = labels.map((l) => COLORS[l] || '#5a5240');

        chartRef.current = new Chart(canvasRef.current, {
          type: 'doughnut',
          data: {
            labels,
            datasets: [{
              data,
              backgroundColor: colors,
              borderColor: '#21201d',
              borderWidth: 2,
              hoverBorderColor: '#ddd6c8',
              hoverBorderWidth: 2,
            }],
          },
          options: {
            responsive: true,
            cutout: '55%',
            plugins: {
              legend: { display: false },
              tooltip: {
                backgroundColor: '#21201d',
                titleColor: '#f5efe0',
                bodyColor: '#ddd6c8',
                borderColor: '#5a5240',
                borderWidth: 1,
                padding: 10,
                titleFont: { family: 'JetBrains Mono', size: 12 },
                bodyFont: { family: 'JetBrains Mono', size: 11 },
                callbacks: {
                  label: (ctx) => ` ${ctx.label}: ${ctx.parsed} offers`,
                },
              },
            },
          },
        });
      });

    return () => {
      cancelled = true;
      if (chartRef.current) { chartRef.current.destroy(); chartRef.current = null; }
    };
  }, [categoryId]);

  return (
    <div className="chart-wrap">
      <div className="chart-container">
        <canvas ref={canvasRef} />
      </div>
    </div>
  );
}
