import { Link } from 'react-router';
import { useAuth } from '../../context';
import tutorialVideo from '../../assets/tutorial.mp4';

function getTodayString() {
  const now = new Date();
  return now.toLocaleDateString('es-ES', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  }) + ' a las ' + now.toLocaleTimeString('es-ES', {
    hour: '2-digit',
    minute: '2-digit',
  });
}

const steps = [
  {
    num: '01',
    title: 'Completa tu perfil',
    desc: 'Rellena todas las secciones de tu perfil: datos personales, idiomas, certificaciones, educación, experiencia, proyectos y enlaces. La mayoría se rellenan automáticamente importando desde LinkedIn o GitHub.',
    highlight: true,
    link: '/profile',
    linkLabel: 'Ir al perfil',
  },
  {
    num: '02',
    title: 'Explora las ofertas',
    desc: 'En la sección Offers verás todas las ofertas filtradas por IA para puestos junior. Cada oferta muestra su % de compatibilidad con tu perfil. Al visualizar una oferta puedes asignar sus keywords directamente a tu perfil con un solo click.',
    link: '/offers',
    linkLabel: 'Ver ofertas',
  },
  {
    num: '03',
    title: 'Añade keywords a tu perfil',
    desc: 'Las keywords son tecnologías, herramientas o habilidades (ej. React, Docker, Python). Puedes añadirlas manualmente desde tu perfil o directamente desde cualquier oferta de trabajo. El sistema calcula automáticamente el % de match con cada oferta.',
    link: '/keywords',
    linkLabel: 'Gestionar keywords',
  },
  {
    num: '04',
    title: 'Mueve ofertas a Tracking',
    desc: 'Cuando una oferta te interese, muévela a Tracking. Desde allí podrás generar un CV personalizado para esa oferta y cambiar el estado de tu candidatura (Applied, Interview, Offer, Rejected...).',
    link: '/tracking',
    linkLabel: 'Ir a tracking',
  },
  {
    num: '05',
    title: 'Genera tu CV en HTML',
    desc: 'Los CV se generan en formato HTML para que puedas editarlos fácilmente antes de imprimirlos como PDF. Así tienes control total sobre el contenido y el formato final.',
    link: '/tracking',
    linkLabel: 'Gestionar CVs',
  },
];

export default function HomePage() {
  const { user } = useAuth();

  return (
    <div className="home-page">
      <div className="home-notice">
        <span className="home-notice-date">{getTodayString()}</span>
        <p>
          Por un error en un algoritmo heurístico he eliminado de la base de datos keywords importantes como &ldquo;C&rdquo; o &ldquo;C++&rdquo;. Como resultado, el % de match con las ofertas puede verse alterado. Estoy solucionando este problema para que puedas guiarte del % de match a partir del día 1 de junio sin miedo.
        </p>
      </div>

      <section className="home-hero">
        <div className="home-hero-text">
          <h1>
            Bienvenido{user?.name ? `, ${user.name}` : ''}
          </h1>
          <p>
            Sigue estos pasos para sacar el máximo partido a 42jobs. En menos de 5 minutos tendrás todo listo para empezar a buscar tu primer trabajo en tecnología.
          </p>
        </div>
        <div className="home-hero-action">
          <Link to="/dashboard" className="home-dashboard-btn">
            Ir al Dashboard
          </Link>
        </div>
      </section>

      <section className="home-video">
        <h2>Cómo completar tu perfil en 2 minutos</h2>
        <div className="home-video-container">
          <video
            src={tutorialVideo}
            controls
            className="home-video-player"
          >
            Tu navegador no soporta el elemento de video.
          </video>
        </div>
      </section>

      <section className="home-advantages">
        <h2>¿Por qué 42jobs en lugar de las plataformas de empleo tradicionales?</h2>
        <div className="home-advantages-grid">
          <div className="home-advantage-card">
            <span className="home-advantage-icon">🎯</span>
            <div>
              <h3>Filtrado automático para juniors</h3>
              <p>Las plataformas tradicionales mezclan ofertas senior, mid y junior sin distinción clara. 42jobs filtra automáticamente con IA para mostrarte solo lo que de verdad encaja con tu perfil junior. No pierdas tiempo leyendo ofertas que piden 5 años de experiencia.</p>
            </div>
          </div>
          <div className="home-advantage-card">
            <span className="home-advantage-icon">📊</span>
            <div>
              <h3>Match % real con tu perfil</h3>
              <p>En LinkedIn o InfoJobs envías el mismo CV a todo. Aquí ves exactamente qué tecnologías coinciden entre la oferta y tu perfil, y cuál es tu % de compatibilidad real. Así priorizas las ofertas donde tienes más posibilidades.</p>
            </div>
          </div>
          <div className="home-advantage-card">
            <span className="home-advantage-icon">📄</span>
            <div>
              <h3>CV por oferta, sin esfuerzo</h3>
              <p>En plataformas normales tienes que adaptar tu CV manualmente para cada puesto. 42jobs genera un CV en HTML optimizado para ATS con tus datos y las keywords de la oferta automáticamente. Edítalo, conviértelo a PDF y envíalo.</p>
            </div>
          </div>
          <div className="home-advantage-card">
            <span className="home-advantage-icon">🗂️</span>
            <div>
              <h3>Seguimiento centralizado</h3>
              <p>Deja de usar hojas de cálculo para trackear candidaturas. Con 42jobs mueves ofertas a Tracking, cambias el estado (Applied, Interview, Offer, Rejected) y llevas el control de todo tu proceso desde un solo lugar.</p>
            </div>
          </div>
          <div className="home-advantage-card">
            <span className="home-advantage-icon">🆓</span>
            <div>
              <h3>Gratis y open source</h3>
              <p>Sin suscripciones, sin premium, sin límites artificiales. 42jobs es 100% gratuito y de código abierto. Tú controlas tus datos y puedes incluso ejecutarlo en tu propio servidor si lo prefieres.</p>
            </div>
          </div>
        </div>
      </section>

      <section className="home-steps">
        <h2>Pasos a seguir</h2>
        <div className="home-steps-grid">
          {steps.map((step) => (
            <div key={step.num} className={`home-step-card${step.highlight ? ' home-step-highlight' : ''}`}>
              <span className="home-step-num">{step.num}</span>
              <h3>{step.title}</h3>
              <p>{step.desc}</p>
              <Link to={step.link} className="home-step-link">
                {step.linkLabel} →
              </Link>
            </div>
          ))}
        </div>
      </section>
    </div>
  );
}
