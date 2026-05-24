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
- **Barrel pattern**: every folder has an `index.ts` that re-exports its contents. Import from the folder, never from internal files directly.
  ```ts
  // ✅ good — barrel import
  import { CategoriesBar, NotesModal } from '../../components';

  // ❌ bad — direct import
  import CategoriesBar from '../../components/categories/CategoriesBar';
  ```
- **Data router**: routes are defined in `router.tsx` using `createBrowserRouter` with loaders and actions. The main entry point uses `<RouterProvider>`.
- **Loaders per page**: data fetching goes in `pageName.loader.ts`, not in `useEffect` inside the component. Use `useLoaderData()` in the component.
- **Actions for mutations**: form submissions use action files (`login.action.ts`, `register.action.ts`). For programmatic mutations, use `useRevalidator()` to refresh loader data.
- **File splitting**: each page is a folder with:
  - `PageName.tsx` — pure component (JSX)
  - `pageName.loader.ts` — data fetching (keywords, jobs, etc.)
  - `pageName.types.ts` — local interfaces (optional, when needed)
  - `pageName.action.ts` — form actions (optional, for POST/PUT/DELETE)
- **No file over ~150 lines.** If a file grows too long, extract sub-components or utility functions.
- **Custom hooks** go in `hooks/` (with barrel). Shared utils go in `utils/`.
- **Shared types** go in `types/index.ts`. Page-local types go in the page's `*.types.ts`.
- **CSS** is split by responsibility in `styles/` (13 modules). `index.css` only imports them. Do not write new styles in `index.css`. Place them in the appropriate module or create a new one.
- API calls use the Vite proxy (`/api` → backend) in development.

### AI layer

- Controllers **never** call AI providers directly. They inject `IAiService`.
- Prompts and response schemas live in the database, not in code.
- See [docs/IAProvider.md](docs/IAProvider.md) for adding a new AI provider.

### Job sourcing layer

- Controllers **never** call job APIs directly. They inject `IJobFetchService` (for fetching) or `IJobProvider` (for refresh/details).
- `JobFetchService` calls all enabled `IJobProvider` implementations, one per portal.
- See [docs/JobProvider.md](docs/JobProvider.md) for adding a new job source.

## Adding a new feature

1. If it requires a database change, add a new migration SQL file.
2. Create or update the C# model in `backend/src/Models/`.
3. Add the EF Core configuration in `AppDbContext.cs`.
4. Create the controller endpoint (partial class file).
5. If it uses AI, inject `IAiService` and use the appropriate prompt from the DB.

## Adding a new AI provider

See the dedicated guide: [docs/IAProvider.md](docs/IAProvider.md).

## Adding a new job provider

See the dedicated guide: [docs/JobProvider.md](docs/JobProvider.md).

## Commit messages

Keep them concise and follow this format:

```
type(scope): short description
```

Types: `feat`, `fix`, `refactor`, `chore`, `docs`, `style`.

Examples:
- `feat(db): add resumes table`
- `fix(cv): handle empty description`
- `refactor(ai): extract Gemini provider`
- `refactor(frontend): split CSS into modules`
- `feat(frontend): add loader for Dashboard`

## Questions?

Open an issue or start a discussion.
