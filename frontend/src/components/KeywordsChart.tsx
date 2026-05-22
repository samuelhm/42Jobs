import { useEffect, useRef } from 'react';
import { Chart, DoughnutController, ArcElement, Tooltip } from 'chart.js';

Chart.register(DoughnutController, ArcElement, Tooltip);

interface Keyword {
  name: string;
  count: number;
}

interface Props {
  categoryId: string | null;
  onHover: (keyword: string | null) => void;
}

const PALETTE = [
  '#e6a845', '#d06b5a', '#5b9fd4', '#55c980', '#c4a43e',
  '#e87860', '#4d8dc0', '#48b868', '#d49c3a', '#c06088',
  '#57b0d8', '#e89068', '#6cac58', '#b870d0', '#d4a848',
  '#609cc8', '#e8a040', '#58c0a0', '#d06078', '#7898d0',
  '#e09858', '#48b8c0', '#b8c058', '#c878a0', '#d8b048',
];

export default function KeywordsChart({ categoryId, onHover }: Props) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const chartRef = useRef<Chart<'doughnut'> | null>(null);
  const keywordsRef = useRef<Keyword[]>([]);

  useEffect(() => {
    if (!categoryId) return;

    let cancelled = false;

    fetch(`/api/categories/${categoryId}/keywords`)
      .then((r) => r.json())
      .then((json) => {
        if (cancelled || !json.success || !canvasRef.current) return;

        const keywords: Keyword[] = json.data;
        keywordsRef.current = keywords;

        if (chartRef.current) chartRef.current.destroy();

        const data = keywords.map((k) => k.count);
        const labels = keywords.map((k) => k.name);
        const colors = labels.map((_, i) => PALETTE[i % PALETTE.length]);

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
            onHover: (_event, elements) => {
              if (elements.length > 0) {
                const idx = elements[0].index;
                onHover(keywords[idx].name);
              } else {
                onHover(null);
              }
            },
          },
        });
      });

    return () => {
      cancelled = true;
      if (chartRef.current) { chartRef.current.destroy(); chartRef.current = null; }
    };
  }, [categoryId, onHover]);

  return (
    <div className="chart-wrap">
      <div className="chart-container">
        <canvas ref={canvasRef} />
      </div>
    </div>
  );
}
