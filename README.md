# BimJobsNet

BimJobsNet is a job search platform tailored for **junior software engineers**. It fetches job offers from LinkedIn, filters them with AI for relevance and junior-friendliness, extracts keywords, and generates ATS-optimized CVs.

## Features

- **Job fetching** — pulls offers from LinkedIn via RapidAPI by category
- **AI filtering** — keeps only relevant offers suitable for junior profiles
- **Keyword extraction** — identifies technologies, skills, and soft skills per offer
- **CV generation** — generates ATS-optimized CVs via LLM, customized per job offer
- **GitHub import** — analyzes your repositories and creates project entries automatically
- **Profile management** — education, experience, certifications, languages, skills
- **Job tracking** — status pipeline: saved → CV sent → interview → hired / rejected

## Tech Stack

| Layer       | Technology                                      |
|-------------|-------------------------------------------------|
| Backend     | .NET 10 (ASP.NET Core Web API), EF Core, JWT    |
| Database    | PostgreSQL 16                                   |
| Frontend    | React + React Router + TypeScript (Vite)        |
| AI          | OpenAI / Google Gemini (pluggable providers)    |
| Infra       | Docker + Docker Compose                         |
| Package mgr | pnpm                                            |

## Quick Start

```bash
cp .env.example .env   # edit with your API keys
make dev-up            # starts db + backend + frontend
```

The app will be available at:
- **Frontend**: http://localhost:3000
- **API**: http://localhost:8080

### Available commands

```bash
make dev-up          # start development services
make dev-down        # stop development services
make dev-restart     # rebuild + restart
make dev-logs        # follow all logs
make prod-up         # start production services
make prod-down       # stop production services
make clean           # stop everything + delete volumes
```

## Architecture

```
bimjobsnet/
├── backend/src/
│   ├── Controllers/     # 13 REST controllers (partial classes, one endpoint per file)
│   ├── Models/          # 22 C# entity models + DTOs
│   ├── Data/            # EF Core DbContext (Fluent API config)
│   ├── Services/
│   │   ├── Ai/          # AI abstraction layer
│   │   │   ├── AiService.cs             # reads prompts from DB, resolves providers
│   │   │   └── Providers/{Gemini,OpenAI} # low-level API clients
│   │   ├── JwtService.cs
│   │   ├── LinkedInApiService.cs
│   │   └── JobFetchOrchestrator.cs       # background job queue
│   └── Utils/
├── frontend/src/       # React SPA (Vite)
├── database/migrations/ # 22 SQL migration + seed files
└── docs/               # Documentation
```

### AI provider abstraction

Controllers inject `IAiService`. The actual provider (Gemini, OpenAI, or future ones) is selected at runtime based on the **default model** configured in the database (`ai_models.is_default`). Prompts and response schemas are stored in the DB, not hardcoded.

See [docs/IAProvider.md](docs/IAProvider.md) to add a new provider.

## Environment Variables

| Variable           | Description                  |
|--------------------|------------------------------|
| `POSTGRES_USER`    | Database user                |
| `POSTGRES_PASSWORD`| Database password            |
| `POSTGRES_DB`      | Database name                |
| `DATABASE_URL`     | Full connection string       |
| `JWT_SECRET_KEY`   | JWT signing key              |
| `LINKEDIN_API_KEY` | RapidAPI key for LinkedIn    |
| `LINKEDIN_API_HOST`| RapidAPI LinkedIn host       |
| `LLM_GOOGLE_API_KEY`| Google Gemini API key       |
| `LLM_OPENAI_API_KEY`| OpenAI API key              |

## License

Dual-licensed under [AGPL v3](LICENSE) for open source / non-commercial use. For commercial use (if you wish to keep your modifications private), contact the author for a commercial license.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).
