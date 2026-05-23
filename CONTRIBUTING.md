# Contributing to 42jobs

Thanks for your interest in contributing.

## Getting started

1. Fork the repository
2. Clone your fork
3. Copy `.env.example` to `.env` and fill in the required API keys
4. Run `make dev-up` to start the development environment
5. Make your changes
6. Open a pull request

## Project conventions

### Backend

- Controllers use **partial classes**: one file per endpoint inside a folder named after the controller. See `Controllers/Users/` for the pattern.
- The constructor, fields, helpers, and `[Route]` attribute go in `{Name}Controller.cs`. Each endpoint lives in `{Name}Controller.{Verb}.cs`.
- All responses follow `{ success: bool, data: ... }` with `snake_case` JSON.
- Authentication uses JWT stored in a cookie named `42jobs_auth`.

### Database

- Migrations are plain SQL files in `database/migrations/`, applied alphabetically on container startup via `docker-entrypoint-initdb.d`.
- Naming convention: `NN-descriptive-name.sql` (e.g., `07-users.sql`, `019-ai-services.sql`).
- Seed data files follow the same pattern (e.g., `022-seed-ai.sql`).

### Frontend

- Use `pnpm` for package management (not npm).
- Components are in `frontend/src/components/`, pages in `frontend/src/pages/`.
- API calls use the Vite proxy (`/api` → backend) in development.

### AI layer

- Controllers **never** call AI providers directly. They inject `IAiService`.
- Prompts and response schemas live in the database, not in code.
- See [docs/IAProvider.md](docs/IAProvider.md) for adding a new provider.

## Adding a new feature

1. If it requires a database change, add a new migration SQL file.
2. Create or update the C# model in `backend/src/Models/`.
3. Add the EF Core configuration in `AppDbContext.cs`.
4. Create the controller endpoint (partial class file).
5. If it uses AI, inject `IAiService` and use the appropriate prompt from the DB.

## Adding a new AI provider

See the dedicated guide: [docs/IAProvider.md](docs/IAProvider.md).

## Commit messages

Keep them concise and follow this format:

```
type(scope): short description
```

Examples:
- `feat(db): add resumes table`
- `fix(cv): handle empty description`
- `refactor(ai): extract Gemini provider`

Types: `feat`, `fix`, `refactor`, `chore`, `docs`, `style`.

## Questions?

Open an issue or start a discussion.
