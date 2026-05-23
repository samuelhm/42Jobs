# =============================================================================
# 42jobs - Frontend Dockerfile (Multi-stage)
# =============================================================================

# ─── Base (pnpm + deps) ─────────────────────────────────────
FROM node:20-alpine AS base
RUN corepack enable && corepack prepare pnpm@10 --activate
WORKDIR /app
COPY package.json pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile

# ─── Development (Vite dev server con HMR) ───────────────────
FROM base AS development
COPY . .
EXPOSE 5173
CMD ["pnpm", "dev"]

# ─── Builder (Vite build para producción) ────────────────────
FROM base AS builder
COPY . .
RUN pnpm run build

# ─── Production (Nginx sirviendo build estático) ─────────────
FROM nginx:alpine AS production
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=builder /app/dist /usr/share/nginx/html
EXPOSE 80
