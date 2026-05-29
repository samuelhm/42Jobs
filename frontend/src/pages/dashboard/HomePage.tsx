import { Link } from 'react-router';
import { useAuth } from '../../context';

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
        <h2>Guía rápida en video</h2>
        <div className="home-video-container">
          <iframe
            src="https://www.youtube.com/embed/VIDEO_ID"
            title="42jobs - Guía de uso"
            allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
            allowFullScreen
          />
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
