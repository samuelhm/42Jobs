# Deployment Guide

## Production architecture

```
Browser (HTTPS) → Nginx Proxy Manager → frontend (nginx:80)
                                            │
                                            ├── /api/* → backend:8080
                                            └── /*     → static files (React SPA)
```

- **Nginx Proxy Manager** handles SSL termination (Let's Encrypt), domain routing, and rate limiting
- **Frontend nginx** serves the React build and proxies `/api/` to the backend
- **Backend** is NOT exposed to the internet — only reachable via the internal Docker network
- **Database** has no port exposed in production; only accessible from the internal network
- **API keys** are configured via the admin panel (stored in DB), NOT in environment variables

## Before deploying checklist

### 1. Server setup

- [ ] Docker + Docker Compose installed
- [ ] Nginx Proxy Manager running with `red_proxy` network created
- [ ] Domain `42jobs.xyz` pointing to the server
- [ ] Minimum specs: 2 vCPU, 2 GB RAM, 20 GB disk

### 2. Environment

- [ ] Copy `.env.example` → `.env` on the production server
- [ ] Generate a strong `JWT_SECRET_KEY` (64+ random chars):
  ```bash
  openssl rand -base64 48
  ```
- [ ] Set `POSTGRES_PASSWORD` to a strong random value
- [ ] Update `DATABASE_URL` with the chosen password:
  ```
  DATABASE_URL=postgresql://42jobs:RANDOM_PASS@db:5432/42jobs
  ```
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production` (already in docker-compose.yml)
- [ ] Do NOT set any `LINKEDIN_*` or `LLM_*` keys — these are configured via admin panel

### 3. JWT and cookies

- [ ] Verify `appsettings.json` JWT config:
  - `CookieName`: `42jobs_auth`
  - `Domain`: `42jobs.xyz` (your production domain)
  - `HttpOnly`: `true`
  - `Secure`: `true` (requires HTTPS via reverse proxy)
  - `SameSite`: `Strict`

### 4. Start the stack

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

### 5. Nginx Proxy Manager configuration

- [ ] Add proxy host: `42jobs.xyz` → `http://42jobs-frontend:80` (container name)
- [ ] Enable SSL with Let's Encrypt
- [ ] Force HTTPS redirect
- [ ] Set websocket support (for potential future use)

### 6. First admin setup

- [ ] Create an account via the web UI
- [ ] Promote to admin via the make-admin script:
  ```bash
  docker compose -f docker-compose.yml -f docker-compose.prod.yml exec db \
    psql -U 42jobs -d 42jobs -c "UPDATE users SET role = 'Admin' WHERE email = 'you@example.com';"
  ```
- [ ] Log in, go to Admin panel
- [ ] Configure **AI Services**: set API keys for Google and/or OpenAI
- [ ] Configure **Job Providers**: set RapidAPI host + API key
- [ ] Set the default model for each operation in **Prompts**

### 7. Verify

- [ ] Visit `https://42jobs.xyz` — should load the login page
- [ ] Register, login, verify JWT cookie is set
- [ ] Go to Admin panel, check all sections load
- [ ] Test job fetch: add a category, click fetch
- [ ] Check `docker compose ps` — all containers healthy

## Backup

### Database

```bash
# Backup
docker compose -f docker-compose.yml -f docker-compose.prod.yml exec db \
  pg_dump -U 42jobs 42jobs | gzip > backup-$(date +%Y%m%d).sql.gz

# Restore
gunzip -c backup-20250101.sql.gz | \
  docker compose -f docker-compose.yml -f docker-compose.prod.yml exec -T db \
  psql -U 42jobs 42jobs
```

Set up a cron job for daily backups.

### Volumes

The PostgreSQL data is stored in a named volume (`pgdata`). Back up regularly using `pg_dump` as shown above.

## Updating

```bash
# Automated: bumps version, creates tag, triggers CI deploy
make release

# Manual (from the server):
git pull
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

## Troubleshooting

| Symptom | Check |
|---------|-------|
| Backend 500 errors | Check `docker compose logs backend` for exceptions |
| AI calls fail | Verify API keys in Admin > AI Services. Test the model is active |
| Job fetch returns no results | Check Admin > Job Providers — host and key configured correctly? |
| Cookie not persisting | Check HTTPS is working (Secure cookies require HTTPS) |
| Frontend shows blank page | Check `docker compose logs frontend` — build may have failed |
| Database connection refused | Check `DATABASE_URL` in `.env` matches `POSTGRES_USER/PASSWORD/DB` |
