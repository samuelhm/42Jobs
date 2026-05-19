# =============================================================================
# BimbaJobs - Frontend Dockerfile (Multi-stage)
# =============================================================================

# ─── Development (browser-sync live-reload) ────────────────
FROM node:20-alpine AS development
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY public/ ./public/
COPY bs-config.cjs ./
EXPOSE 3000
CMD ["npx", "browser-sync", "start", "--config", "bs-config.cjs"]

# ─── Production (Nginx) ───────────────────────────────────
FROM nginx:alpine AS production
COPY public/ /usr/share/nginx/html/
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
