export default function PrivacyPage() {
  return (
    <div className="page-content">
      <h2>Privacy Policy</h2>
      <p><em>Last updated: May 2026</em></p>

      <h3>1. Data We Collect</h3>
      <p>
        42jobs collects the following personal data when you create an account:
      </p>
      <ul>
        <li><strong>Email address</strong> — required for account creation and authentication.</li>
        <li><strong>Name and last name</strong> — optional, to personalize your profile.</li>
        <li><strong>Profile data</strong> — languages, certifications, education, work experience, and projects you choose to provide.</li>
        <li><strong>CV data</strong> — generated resumes based on your profile and job descriptions.</li>
        <li><strong>LinkedIn and GitHub profile URLs</strong> — optional, for importing your professional data.</li>
      </ul>

      <h3>2. Why We Collect It</h3>
      <p>
        All data is used exclusively to provide the 42jobs service: matching you with junior-friendly job offers,
        generating tailored CVs, and tracking your job applications. No data is sold, shared, or used for advertising.
      </p>

      <h3>3. Data Storage</h3>
      <p>
        Your data is stored on private servers with PostgreSQL databases.
        API keys and sensitive credentials are encrypted at rest.
      </p>

      <h3>4. Third-Party Services</h3>
      <p>
        42jobs uses third-party AI APIs (OpenAI, Google Gemini, DeepSeek) to analyze job descriptions,
        extract keywords, and generate CVs. Your profile data and job descriptions are sent to these APIs
        temporarily for processing. These providers may process data on servers outside the European Union.
        By using 42jobs, you consent to this transfer.
      </p>

      <h3>5. Your Rights</h3>
      <p>
        Under the GDPR, you have the right to:
      </p>
      <ul>
        <li><strong>Access</strong> — request a copy of your personal data.</li>
        <li><strong>Rectification</strong> — correct inaccurate or incomplete data.</li>
        <li><strong>Erasure</strong> — request deletion of your account and all associated data.</li>
        <li><strong>Portability</strong> — receive your data in a structured, machine-readable format.</li>
      </ul>
      <p>
        To exercise any of these rights, contact us at{' '}
        <a href="mailto:samuel@hurtadom.dev">samuel@hurtadom.dev</a>.
      </p>

      <h3>6. Cookies</h3>
      <p>
        42jobs uses a single functional cookie (<code>42jobs_auth</code>) for authentication purposes.
        This cookie is strictly necessary for the service to function and does not track you across sites.
        No third-party cookies, analytics, or tracking scripts are used.
      </p>

      <h3>7. Data Retention</h3>
      <p>
        Your data is retained until you delete your account. You can request account deletion at any time
        by contacting us.
      </p>

      <h3>8. Contact</h3>
      <p>
        For privacy-related inquiries, contact the data controller:{' '}
        <a href="mailto:samuel@hurtadom.dev">samuel@hurtadom.dev</a>.
      </p>
    </div>
  );
}
