import { Link } from 'react-router';
import { useAuth } from '../../context';
import Footer from '../../components/layout/Footer';

export default function LandingPage() {
  const { user } = useAuth();

  return (
    <div className="landing">
      <div className="landing-hero">
        <h1 className="landing-logo">42<span className="accent">jobs</span></h1>
        <h2>Encuentra tu primer trabajo en tecnología</h2>
        <p>
          Búsqueda con IA, filtrado inteligente y CVs optimizados para ATS — hecho por y para developers junior.
          Gratis y open source.
        </p>

        <div className="landing-actions">
          {user ? (
            <Link to="/dashboard" className="landing-btn landing-btn-primary">Ir al Dashboard</Link>
          ) : (
            <>
              <Link to="/register" className="landing-btn landing-btn-primary">Empezar</Link>
              <Link to="/login" className="landing-btn landing-btn-secondary">Iniciar sesión</Link>
            </>
          )}
          <a href="https://github.com/samuelhm/42Jobs" target="_blank" rel="noopener noreferrer"
            className="landing-btn landing-btn-ghost">
            GitHub ★
          </a>
        </div>

        <div className="landing-features">
          <div className="landing-feature">
            <span className="icon">🔍</span>
            <h4>Búsqueda con IA</h4>
            <p>Buscamos ofertas y las filtramos automáticamente para puestos junior.</p>
          </div>
          <div className="landing-feature">
            <span className="icon">🧠</span>
            <h4>Filtrado inteligente</h4>
            <p>Una IA analiza cada oferta para decidir si es relevante y compatible con tu perfil.</p>
          </div>
          <div className="landing-feature">
            <span className="icon">📄</span>
            <h4>CVs para ATS</h4>
            <p>Genera CVs personalizados para cada oferta, optimizados para sistemas de tracking.</p>
          </div>
        </div>

        <Link to="/faq" className="landing-faq-card">
          <span className="icon">❓</span>
          <div>
            <span className="landing-faq-title">Preguntas frecuentes</span>
            <span className="landing-faq-sub">¿Cómo se filtran las ofertas? ¿Por qué el CV en HTML? ¿Puedo usar mis propias APIs?</span>
          </div>
          <span className="landing-faq-arrow">→</span>
        </Link>
      </div>

      <Footer />
    </div>
  );
}
