-- Remove preferred_date_posted from users table
-- The date filter feature is removed. Backend auto-uses 'past-week' as default.
ALTER TABLE users DROP COLUMN IF EXISTS preferred_date_posted;
