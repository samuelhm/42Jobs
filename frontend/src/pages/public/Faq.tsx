import { Link } from 'react-router';

interface FaqItem {
  q: string;
  a: React.ReactNode;
}

const faqs: FaqItem[] = [
  {
    q: '¿Por qué la web es lenta? / ¿Por qué generar un CV tarda?',
    a: (
      <>
        Estoy reduciendo costes. Soy junior, como tú. Construí esto para uso personal y lo abrí al público sin ánimo de lucro. No puedo permitirme tokens caros.
        Además metí saldo en DeepSeek en lugar de OpenRouter, y no puedo añadir más hasta gastar este.
      </>
    ),
  },
  {
    q: '¿Por qué hay pocos trabajos de X categoría?',
    a: (
      <>
        Algunas categorías tienen tendencia a pedir requisitos muy altos, incompatibles con un puesto junior.
        El filtro deja pasar ofertas que pidan ≤3 años de experiencia aunque no los tengas — puedes postular con tus proyectos.
        Pero si la oferta pide explícitamente senior, o mucha experiencia en varias tecnologías, prefiero filtrarla para no hacerte perder el tiempo.
      </>
    ),
  },
  {
    q: '¿Cómo se filtran las ofertas?',
    a: (
      <>
        <ol style={{ paddingLeft: '1.2rem', marginBottom: '0.5rem' }}>
          <li>LinkedIn API — buscamos trabajos por categoría y ubicación.</li>
          <li>Filtro automático — descartamos niveles <strong>Mid-Senior, Director, Executive</strong> sin gastar créditos de IA.</li>
          <li>IA analiza título + descripción — decide si la oferta es relevante y si es junior-friendly.</li>
          <li>IA extrae keywords técnicas — para que sepas qué se pide en cada categoría.</li>
          <li>Se guarda en BD y aparece en tu dashboard.</li>
        </ol>
        Los descartados van a Admin → Discarded por si algún falso positivo te interesa.
      </>
    ),
  },
  {
    q: '¿Para qué sirven las keywords?',
    a: (
      <>
        Cada oferta se analiza con IA para extraer las tecnologías y habilidades que solicita.
        Esas keywords se acumulan por categoría y puedes ver cuáles son las más demandadas en el mercado.
        Además, al generar un CV, la IA cruza tus keywords (las que has aprendido en proyectos o en 42) con las del puesto para optimizarlo al máximo.
      </>
    ),
  },
  {
    q: 'Encuentro más ofertas buscando directamente en LinkedIn.',
    a: (
      <>
        Seguro. Pero date cuenta: aunque pongas "junior" en LinkedIn, la mayoría de resultados no son realmente compatibles con un puesto junior.
        42jobs filtra eso automáticamente por ti. Menos ruido, más foco.
      </>
    ),
  },
  {
    q: '¿Puedo usar mis propias APIs de IA?',
    a: (
      <>
        Es un update que viene pronto. Podrás configurar tus claves de OpenAI, Gemini o DeepSeek desde tu perfil, y todo irá más rápido usando tus propios créditos.
      </>
    ),
  },
  {
    q: '¿Puedo acceder al panel de administrador?',
    a: (
      <>
        Claro. Mándame un correo a{' '}
        <a href="mailto:samuel@hurtadom.dev">samuel@hurtadom.dev</a>. Pero estarás bajo vigilancia :)
      </>
    ),
  },
  {
    q: '¿Por qué el CV se genera en HTML?',
    a: (
      <>
        Para que puedas modificarlo muy rápido, copiarlo, y luego imprimirlo como PDF.
        En próximas actualizaciones podrás editarlo in situ y descargar directamente el PDF.
      </>
    ),
  },
  {
    q: '¿Cada cuánto se actualizan las ofertas?',
    a: (
      <>
        El scheduler busca nuevas ofertas automáticamente cada día a las 00:00 UTC para todas las categorías y ubicaciones.
        También puedes forzar un fetch manual desde el dashboard haciendo clic en una categoría.
      </>
    ),
  },
  {
    q: '¿Es gratis?',
    a: (
      <>
        Sí. Totalmente gratis. Es un proyecto open source, sin fines comerciales.{' '}
        <a href="https://github.com/samuelhm/42Jobs" target="_blank" rel="noopener noreferrer">github.com/samuelhm/42Jobs</a>.
      </>
    ),
  },
  {
    q: '¿Puedo borrar mi cuenta y mis datos?',
    a: (
      <>
        Sí. Escríbeme a{' '}
        <a href="mailto:samuel@hurtadom.dev">samuel@hurtadom.dev</a> y borro tu cuenta con todos tus datos.
        Sin retención, sin letra pequeña.
      </>
    ),
  },
  {
    q: 'Generé un CV y la oferta desapareció de la lista. ¿La he perdido?',
    a: (
      <>
        No. Al generar un CV, la oferta pasa automáticamente a <strong>Tracking</strong> con estado "Saved".
        Deja de aparecer en Offers para no saturarte con duplicados, pero puedes verla en Tracking y cambiar su estado
        (CV enviado, entrevista conseguida, etc.).
      </>
    ),
  },
  {
    q: '¿Puedo contribuir al proyecto?',
    a: (
      <>
        Por supuesto. El código está en{' '}
        <a href="https://github.com/samuelhm/42Jobs" target="_blank" rel="noopener noreferrer">github.com/samuelhm/42Jobs</a>.
        PRs, issues y feedback son bienvenidos. Está hecho con .NET, React, PostgreSQL y Docker.
      </>
    ),
  },
  {
    q: '¿Qué datos recopiláis?',
    a: (
      <>
        Solo los necesarios: email, perfil profesional, CVs generados. Sin tracking, sin ads, sin terceros.{' '}
        <Link to="/privacy">Privacy Policy</Link> para los detalles.
      </>
    ),
  },
];

export default function FaqPage() {
  return (
    <div className="page-content">
      <h2>FAQ</h2>
      <p className="text-muted" style={{ marginBottom: '1.5rem' }}>Preguntas frecuentes sobre 42jobs.</p>
      <div className="faq-list">
        {faqs.map((item, i) => (
          <details key={i} className="faq-item">
            <summary>{item.q}</summary>
            <div className="faq-answer">{item.a}</div>
          </details>
        ))}
      </div>
    </div>
  );
}
