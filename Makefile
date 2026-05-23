COMPOSE_DEV := docker compose
COMPOSE_PROD := docker compose -f docker-compose.yml -f docker-compose.prod.yml

# =============================================================================
# 42jobs - Makefile
# =============================================================================
.PHONY: help \
        dev-up dev-down dev-build dev-restart dev-logs dev-ps dev-shell \
        prod-up prod-down prod-build prod-restart prod-logs prod-ps \
        switch-prod switch-dev install clean

# ─── Default ───────────────────────────────────────────────
help:
	@echo "════════════════════════════════════════════════════════"
	@echo "  42jobs - Comandos"
	@echo "════════════════════════════════════════════════════════"
	@echo ""
	@echo "  DESARROLLO (base + override automático):"
	@echo "    make dev-up         Levantar servicios"
	@echo "    make dev-down       Detener servicios"
	@echo "    make dev-build      Reconstruir imágenes"
	@echo "    make dev-restart    Rebuild + restart (código nuevo)"
	@echo "    make dev-logs       Seguir logs de todos los servicios"
	@echo "    make dev-ps         Ver estado de contenedores"
	@echo "    make dev-shell S=b  Abrir shell (b=backend, f=frontend, d=db)"
	@echo ""
	@echo "  PRODUCCIÓN (base + docker-compose.prod.yml):"
	@echo "    make prod-up        Levantar servicios"
	@echo "    make prod-down      Detener servicios"
	@echo "    make prod-build     Reconstruir imágenes"
	@echo "    make prod-restart   Rebuild + restart"
	@echo "    make prod-logs      Seguir logs"
	@echo "    make prod-ps        Ver estado"
	@echo ""
	@echo "  GENERAL:"
	@echo "    make switch-prod    Bajar dev y subir producción"
	@echo "    make switch-dev     Bajar prod y subir desarrollo"
	@echo "    make install        dotnet restore (backend) + npm install (frontend)"
	@echo "    make clean          Down + eliminar volúmenes"

# ═══════════════════════════════════════════════════════════
#  DESARROLLO
# ═══════════════════════════════════════════════════════════

dev-up:
	$(COMPOSE_DEV) up -d

dev-down:
	$(COMPOSE_DEV) down

dev-build:
	$(COMPOSE_DEV) build

dev-restart:
	$(COMPOSE_DEV) down && $(COMPOSE_DEV) up -d --build

dev-logs:
	$(COMPOSE_DEV) logs -f

dev-ps:
	$(COMPOSE_DEV) ps

dev-shell:
	@case "$(S)" in \
		b) $(COMPOSE_DEV) exec backend sh ;; \
		f) $(COMPOSE_DEV) exec frontend sh ;; \
		d) $(COMPOSE_DEV) exec db sh ;; \
		*) echo "Usa: make dev-shell S={b|f|d}" ;; \
	esac

# ═══════════════════════════════════════════════════════════
#  PRODUCCIÓN
# ═══════════════════════════════════════════════════════════

prod-up:
	$(COMPOSE_PROD) up -d

prod-down:
	$(COMPOSE_PROD) down

prod-build:
	$(COMPOSE_PROD) build

prod-restart:
	$(COMPOSE_PROD) down && $(COMPOSE_PROD) up -d --build

prod-logs:
	$(COMPOSE_PROD) logs -f

prod-ps:
	$(COMPOSE_PROD) ps

# ═══════════════════════════════════════════════════════════
#  GENERAL
# ═══════════════════════════════════════════════════════════

install:
	cd backend && dotnet restore
	cd frontend && pnpm install

switch-prod:
	$(COMPOSE_DEV) down && $(COMPOSE_PROD) up -d

switch-dev:
	$(COMPOSE_PROD) down && $(COMPOSE_DEV) up -d

clean:
	$(COMPOSE_DEV) down -v || $(COMPOSE_PROD) down -v
