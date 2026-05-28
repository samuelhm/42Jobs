import { Link } from 'react-router';

export default function Footer() {
  return (
    <footer className="footer">
      <div className="footer-links">
        <Link to="/privacy">Privacy</Link>
        <Link to="/terms">Terms</Link>
        <Link to="/faq">FAQ</Link>
        <Link to="/contact">Contact</Link>
        <a href="https://github.com/samuelhm/42Jobs" target="_blank" rel="noopener noreferrer">GitHub</a>
      </div>
      <span className="footer-copy">&copy; {new Date().getFullYear()} Samuel Hurtado Marin</span>
    </footer>
  );
}
