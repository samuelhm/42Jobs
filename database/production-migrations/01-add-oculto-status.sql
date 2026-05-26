ALTER TABLE user_jobs DROP CONSTRAINT IF EXISTS CK_user_jobs_status;
ALTER TABLE user_jobs ADD CONSTRAINT CK_user_jobs_status
    CHECK (status IN ('saved', 'cv_enviado', 'entrevista_conseguida', 'empleo_conseguido', 'rechazado', 'oculto'));
