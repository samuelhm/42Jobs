export interface TrackingJob {
  job_id: number;
  title: string;
  description: string | null;
  location: string | null;
  posted_date: string | null;
  salary: string | null;
  benefits: string | null;
  job_type: string | null;
  experience_level: string | null;
  job_url: string | null;
  company_name: string | null;
  company_type: string | null;
  keywords: string[];
  categories: { id: number; name: string }[];
  status: 'saved' | 'cv_enviado' | 'entrevista_conseguida' | 'empleo_conseguido' | 'rechazado';
  status_updated_at: string;
  notes: string | null;
  saved_at: string;
}
