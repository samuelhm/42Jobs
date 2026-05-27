export interface User {
  id: string;
  email: string;
  name: string | null;
  last_name: string | null;
  role: string;
}

export interface Toast {
  id: string;
  message: string;
  type: 'info' | 'success' | 'error';
}

export interface ProfileData {
  name?: string;
  last_name?: string;
  phone?: string;
  email?: string;
  address?: string;
  linkedin_url?: string;
  website_url?: string;
  github_url?: string;
  junior?: boolean;
  presentation?: string;
  preferred_location?: string;
  photo?: string;
}

export interface Job {
  id: number;
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
  notes: string | null;
  created_at: string;
}

export interface UserKeyword {
  id: number;
  name: string;
  learning_status: string;
}

export interface Category {
  id: number;
  name: string;
  job_count: number;
  last_fetched_at: string | null;
}

export interface ApiResponse<T> {
  success: boolean;
  data: T;
  error?: string;
  status?: number;
}
