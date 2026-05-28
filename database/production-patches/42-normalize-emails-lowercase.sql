-- 42-normalize-emails-lowercase.sql
-- Parche de producción: normaliza emails existentes a lowercase
-- Relacionado con el fix #2 del backend (email normalization en login/registro)
-- Sin este parche, usuarios con emails en mayúsculas no podrán hacer login

UPDATE users SET email = LOWER(TRIM(email)) WHERE email != LOWER(TRIM(email));
