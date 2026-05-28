-- 28-normalize-user-emails.sql
-- Normaliza emails existentes a lowercase para que coincidan con el login normalizado
-- Arregla el bug donde usuarios con mayúsculas en el email quedaban lockeados

UPDATE users SET email = LOWER(TRIM(email)) WHERE email != LOWER(TRIM(email));
