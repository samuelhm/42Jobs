export default function TermsPage() {
  return (
    <div className="page-content">
      <h2>Terms of Service</h2>
      <p><em>Last updated: May 2026</em></p>

      <h3>1. About 42jobs</h3>
      <p>
        42jobs is a free, open-source tool designed to help junior developers find their first tech job.
        It is not a commercial product. The source code is available at{' '}
        <a href="https://github.com/samuelhm/42Jobs" target="_blank" rel="noopener noreferrer">github.com/samuelhm/42Jobs</a>.
      </p>

      <h3>2. No Guarantees</h3>
      <p>
        The service is provided &ldquo;as is&rdquo; without warranties of any kind. We do not guarantee:
      </p>
      <ul>
        <li>Service availability or uptime.</li>
        <li>Accuracy, completeness, or relevance of job listings.</li>
        <li>That job applications submitted through external links will result in interviews or offers.</li>
        <li>That AI-generated CVs will be error-free or ATS-compatible with all systems.</li>
      </ul>

      <h3>3. User Responsibilities</h3>
      <p>
        You are responsible for:
      </p>
      <ul>
        <li>Providing accurate information in your profile.</li>
        <li>Reviewing AI-generated CVs before submitting them to employers.</li>
        <li>Complying with the terms of any external job platforms you apply through.</li>
      </ul>

      <h3>4. Acceptable Use</h3>
      <p>
        You agree not to use 42jobs for any unlawful purpose, including but not limited to
        scraping, automated data collection, or resource abuse.
      </p>

      <h3>5. Limitation of Liability</h3>
      <p>
        To the fullest extent permitted by law, the maintainers of 42jobs shall not be liable for
        any damages arising from the use or inability to use the service, including lost opportunities,
        data loss, or service interruptions.
      </p>

      <h3>6. Changes</h3>
      <p>
        We may update these terms at any time. Continued use of the service after changes constitutes
        acceptance of the new terms.
      </p>

      <h3>7. Contact</h3>
      <p>
        Questions about these terms?{' '}
        <a href="mailto:samuel@hurtadom.dev">samuel@hurtadom.dev</a>.
      </p>
    </div>
  );
}
