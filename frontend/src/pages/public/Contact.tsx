import yoPhoto from '../../assets/yo.webp';

export default function ContactPage() {
  return (
    <div className="page-content">
      <h2>Contact</h2>

      <div className="contact-card">
        <img src={yoPhoto} alt="Samuel Hurtado Marin" className="contact-photo"
          onError={(e) => { (e.target as HTMLImageElement).style.display = 'none'; }} />

        <div className="contact-info">
          <span className="name">Samuel Hurtado Marin</span>
          <span className="role">Software Developer</span>
          <a href="mailto:samuel@hurtadom.dev" className="email">samuel@hurtadom.dev</a>

          <div className="contact-links">
            <a href="https://github.com/samuelhm" target="_blank" rel="noopener noreferrer">
              GitHub (@samuelhm)
            </a>
            <a href="https://github.com/samuelhm/42Jobs" target="_blank" rel="noopener noreferrer">
              42jobs Repository
            </a>
          </div>
        </div>
      </div>

      <p style={{ marginTop: '1.5rem', color: 'var(--text-dim)', fontSize: '0.8rem' }}>
        This project is maintained as a personal learning initiative.
        Feel free to reach out with questions, feedback, or contributions.
      </p>
    </div>
  );
}
