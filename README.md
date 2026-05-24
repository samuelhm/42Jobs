# 42jobs — junior job search, done right

[![License](https://img.shields.io/badge/license-AGPL_v3-blue.svg?style=for-the-badge)](LICENSE)
[![Stars](https://img.shields.io/github/stars/samuelhm/42jobs?style=for-the-badge)](https://github.com/samuelhm/42jobs/stargazers)
[![Issues](https://img.shields.io/github/issues/samuelhm/42jobs?style=for-the-badge)](https://github.com/samuelhm/42jobs/issues)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg?style=for-the-badge)](CONTRIBUTING.md)

42jobs is a job search platform tailored for **junior software engineers**. It fetches job offers from LinkedIn, filters them with AI for relevance and junior-friendliness, extracts keywords, and generates ATS-optimized CVs — so you spend less time searching and more time landing interviews.

**Live at [42jobs.xyz](https://42jobs.xyz)**

## Table of Contents

- [Features](#features)
- [Screenshots](#screenshots)
- [Tech Stack](#tech-stack)
- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Architecture](#architecture)
- [Environment Variables](#environment-variables)
- [Roadmap](#roadmap)
- [Contributing](#contributing)
- [License](#license)
- [Contact](#contact)

## Features

- **Job fetching** — pulls offers from LinkedIn via RapidAPI by category
- **AI filtering** — keeps only relevant offers suitable for junior profiles
- **Keyword extraction** — identifies technologies, skills, and soft skills per offer
- **CV generation** — generates ATS-optimized CVs via LLM, customized per job offer
- **GitHub import** — analyzes your repositories and creates project entries automatically
- **Profile management** — education, experience, certifications, languages, skills
- **Job tracking** — status pipeline: saved → CV sent → interview → hired / rejected
- **Pluggable AI** — swap between Gemini and OpenAI, or add your own provider
- **Pluggable job sources** — LinkedIn today, more sources planned

## Screenshots

<!-- TODO: add screenshots of Dashboard, Offers, Profile, CV generation -->
| Dashboard | Job Search | CV Generation |
|-----------|------------|---------------|
| _coming soon_ | _coming soon_ | _coming soon_ |

## Tech Stack

| Layer       | Technology                                      |
|-------------|-------------------------------------------------|
| Backend     | .NET 10 (ASP.NET Core Web API), EF Core, JWT    |
| Database    | PostgreSQL 16                                   |
| Frontend    | React 19 + React Router 7 + TypeScript (Vite)   |
| AI          | OpenAI / Google Gemini (pluggable providers)    |
| Auth        | JWT in HttpOnly cookies + BCrypt                |
| Infra       | Docker + Docker Compose (dev & prod profiles)   |
| Package mgr | pnpm                                            |

## Prerequisites

Before you start, make sure you have:

- [Docker](https://docs.docker.com/get-docker/) & Docker Compose
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) _(optional, only for local dev without Docker)_
- [Node.js 20+](https://nodejs.org/) + [pnpm](https://pnpm.io/installation) _(optional, only for local dev without Docker)_
- A [RapidAPI](https://rapidapi.com/) account with LinkedIn Jobs API subscription
- An API key for [Google Gemini](https://aistudio.google.com/) or [OpenAI](https://platform.openai.com/)

## Quick Start

```bash
# 1. Clone
git clone https://github.com/samuelhm/42jobs.git
cd 42jobs

# 2. Configure environment
cp .env.example .env
# Edit .env with your database credentials and a random JWT secret

# 3. Start
make dev-up
```

The app will be available at:

- **Frontend**: http://localhost:3000
- **API**: http://localhost:8080

### First-time setup

1. Open the frontend, register an account
2. Go to **Admin** panel (you'll need to promote your user to Admin in the DB first — see [CONTRIBUTING.md](CONTRIBUTING.md))
3. Configure your AI provider (Gemini or OpenAI) with your API key
4. Configure a job provider (LinkedIn RapidAPI) with your API key
5. Create a search category (e.g. "React Developer") and hit **Fetch Jobs**

### Useful commands

```bash
make dev-up          # start development services
make dev-down        # stop development services
make dev-restart     # rebuild + restart
make dev-logs        # follow all logs
make prod-up         # start production services
make clean           # stop everything + delete volumes
```

## Architecture

```
42jobs/
├── backend/src/
│   ├── Controllers/     # 13 REST controllers (partial classes, one endpoint per file)
│   ├── Models/          # 22 C# entity models + DTOs
│   ├── Data/            # EF Core DbContext (Fluent API config)
│   ├── Services/
│   │   ├── Ai/          # AI abstraction layer
│   │   │   ├── AiService.cs               # reads prompts from DB, resolves providers
│   │   │   └── Providers/{Gemini,OpenAI}   # low-level API clients
│   │   ├── Jobs/        # Job fetching (background queue via Channel<T>)
│   │   ├── EncryptionService.cs            # API key encryption at rest
│   │   └── JwtService.cs
│   └── Utils/
├── frontend/src/        # React 19 + React Router 7 (Vite + TypeScript)
│   ├── router.tsx        # createBrowserRouter with loaders/actions
│   ├── types/            # Shared TypeScript interfaces
│   ├── utils/            # api, format, match (barrel)
│   ├── hooks/            # useDebounce, usePolling (barrel)
│   ├── context/          # AuthContext, ToastContext (barrel)
│   ├── styles/           # 13 CSS modules by responsibility
│   ├── components/       # Reusable components (7 domain folders + barrel)
│   └── pages/            # Route pages with loaders (4 domain folders + barrel)
├── database/migrations/  # 22 SQL migration + seed files
└── docs/                # Provider guides, encryption docs
```

### AI provider abstraction

Controllers never call AI providers directly. They inject `IAiService`. The actual provider (Gemini, OpenAI, or future ones) is selected at runtime from the database. Prompts and response schemas are stored in the DB, not hardcoded.

→ [Add a new AI provider](docs/IAProvider.md)

### Job provider abstraction

Job sources are pluggable. `JobFetchService` calls all enabled `IJobProvider` implementations, one per portal. Provider config (keys, URLs) lives in the database and is encrypted at rest.

→ [Add a new job provider](docs/JobProvider.md)
→ [How API key encryption works](docs/Encryption.md)

## Environment Variables

| Variable           | Description                  | Required |
|--------------------|------------------------------|----------|
| `POSTGRES_USER`    | Database user                | Yes      |
| `POSTGRES_PASSWORD`| Database password            | Yes      |
| `POSTGRES_DB`      | Database name                | Yes      |
| `DATABASE_URL`     | Full connection string (`postgres://user:pass@host:5432/db`) | Yes |
| `JWT_SECRET_KEY`   | Random string for JWT signing (≥ 256 bits) | Yes |

AI and job provider API keys are configured via the **Admin panel** (not env vars) and are **encrypted at rest** in the database.

## Release & Deploy

Releases follow **semantic versioning** with `v{major}.{minor}.{patch}-alpha` tags (e.g. `v0.1.4-alpha`).

To create a new release:

```bash
# 1. Ensure all changes are committed and pushed to master
git push origin master

# 2. Tag the release with the next version
git tag v0.1.4-alpha

# 3. Push the tag — this triggers the deploy pipeline
git push origin v0.1.4-alpha
```

### What happens when a tag is pushed

The [release workflow](.github/workflows/release.yml) is triggered on any `v*` tag push:

1. **Build** — Backend and frontend Docker images are built and pushed to GitHub Container Registry (`ghcr.io`) with the version tag and `:latest`
2. **GitHub Release** — A release is auto-generated from the commit history
3. **Deploy** — The VPS pulls the latest code, pulls the new images, and restarts the backend and frontend containers

```
git tag vX.Y.Z-alpha  ──>  GitHub Actions  ──>  ghcr.io images  ──>  VPS deploy
```

> Only commits on `master` should be tagged. The tag must start with `v` to trigger the pipeline.

## Roadmap

- [x] Job fetching from LinkedIn (RapidAPI)
- [x] AI filtering + keyword extraction (Gemini / OpenAI)
- [x] CV generation per job offer
- [x] GitHub project import
- [x] Job tracking pipeline
- [x] API key encryption at rest
- [ ] Email notifications for new matching jobs
- [ ] More job providers (InfoJobs, Indeed)
- [ ] UI tests with Playwright
- [ ] Dark mode
- [ ] Public demo instance

Got an idea? [Open an issue](https://github.com/samuelhm/42jobs/issues) or pick one from the roadmap and send a PR!

## Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines on how to get started, project conventions, and how to add new providers.

## License

Dual-licensed under [AGPL v3](LICENSE) for open source / non-commercial use. For commercial use (if you wish to keep your modifications private), contact the author for a commercial license.

## Contact

Samuel Hurtado — [@hurtadom.dev](https://hurtadom.dev) — samuel@hurtadom.dev

Project Link: [https://github.com/samuelhm/42jobs](https://github.com/samuelhm/42jobs)
