# AI Code Reviewer

Open-source AI-powered code review bot for GitHub pull requests. Built with
**.NET 10** + **ASP.NET Core**, **Angular 18**, and **Llama 3.3 70B** running on
the [Groq](https://groq.com) free tier.

When a PR is opened or updated on a repo where the GitHub App is installed,
the bot fetches the diff, asks Llama 3.3 70B for a structured review (summary,
1-10 score, inline comments), and posts the review back to the PR. A small
Angular dashboard lists every review the bot has ever produced.

## Stack

| Concern             | Tech                                                |
|---------------------|-----------------------------------------------------|
| API                 | ASP.NET Core 10 Web API                             |
| AI provider         | Groq (`llama-3.3-70b-versatile`, OpenAI-compatible) |
| GitHub integration  | GitHub App + webhooks, RS256 JWT auth, Octokit.net  |
| Persistence         | SQLite via EF Core                                  |
| Background work     | `Channel<T>` + `BackgroundService`                  |
| Frontend            | Angular 21, standalone components, signals          |
| Backend hosting     | Render (free web service, Docker)                   |
| Frontend hosting    | Vercel (free tier)                                  |
| CI                  | GitHub Actions                                      |

## Repository layout

```
src/
├── Backend/
│   ├── CodeReviewer.slnx
│   ├── CodeReviewer.Api/         (ASP.NET Core, controllers, middleware, queue)
│   ├── CodeReviewer.Core/        (domain entities, services, models)
│   ├── CodeReviewer.Data/        (EF Core DbContext + migrations)
│   └── CodeReviewer.Tests/       (xUnit + FluentAssertions + Moq)
└── Frontend/
    └── code-reviewer-ui/         (Angular 21, standalone components)
docs/
├── github-app-setup.md
└── architecture.md
```

See [docs/architecture.md](docs/architecture.md) for the full request flow.

## Quick start (local)

### 1. Prerequisites

- .NET 10 SDK
- Node.js 22+
- A Groq account (free) and API key — https://console.groq.com
- A GitHub App configured for a sandbox repo — see
  [docs/github-app-setup.md](docs/github-app-setup.md)

### 2. Configure secrets

1. Drop your GitHub App's private key at `secrets/github-app.pem` (the
   `secrets/` directory is mounted into the API container at runtime).
2. Create `.env` at the repo root (gitignored):

```bash
GROQ_API_KEY=gsk_xxxxxxxx
GITHUB_APP_ID=123456
GITHUB_WEBHOOK_SECRET=your-random-string
```

The PEM is loaded from the mounted file — Docker Compose `.env` files don't
handle multi-line values reliably, so the file mount keeps things simple.

### 3. Run the API

```bash
docker compose up --build
```

API listens on `http://localhost:5000`. Health check: `GET /healthz`.

### 4. Run the Angular dashboard

```bash
cd src/Frontend/code-reviewer-ui
npm install
npx ng serve
```

Dashboard at `http://localhost:4200`.

### 5. Forward webhooks to localhost

GitHub needs a public URL for webhooks. Use [smee.io](https://smee.io):

```bash
npm install -g smee-client
smee -u https://smee.io/<channel> -t http://localhost:5000/api/github/webhook
```

Open a PR on the repo where you installed the GitHub App — within seconds the
bot will post a review and the dashboard will show the new row.

## Tests

```bash
cd src/Backend && dotnet test
cd src/Frontend/code-reviewer-ui && npx ng test --watch=false
```

## Deployment (free tier)

### Backend on Render

1. New → Web Service → connect the repo.
2. Root directory: `src/Backend`. Dockerfile path: `CodeReviewer.Api/Dockerfile`.
3. Set env vars (Render's UI accepts multi-line values, so paste the PEM directly):
   - `Groq__ApiKey`, `GitHubApp__AppId`, `GitHubApp__WebhookSecret`
   - `GitHubApp__PrivateKeyPem` — the full PEM contents, including the BEGIN/END lines
   - `ASPNETCORE_URLS=http://+:10000`
   - `ConnectionStrings__Default=Data Source=/tmp/reviews.db`
   - `Cors__AllowedOrigins=https://<your-vercel-domain>`
4. Health check path: `/healthz`.

### Frontend on Vercel

1. New project → connect the repo.
2. Root directory: `src/Frontend/code-reviewer-ui`.
3. Build command: `npx ng build --configuration=production`.
4. Output directory: `dist/code-reviewer-ui/browser`.
5. Edit `src/environments/environment.ts` to set `apiBase` to your Render URL,
   then redeploy.

### GitHub App webhook URL

After Render gives you a URL, edit your GitHub App's webhook URL to
`https://<render-url>/api/github/webhook`.

## Persistence note (Render free tier)

Render's free web service uses **ephemeral storage** — the SQLite file resets
every time the container restarts (including the ~15 min idle stop). For a
public-demo dashboard this is fine; the dashboard simply shows reviews from
the current uptime window.

To make it persistent without paying, two drop-in upgrade paths:

1. **Turso libSQL** (recommended, free 9 GB cloud SQLite). Add the
   [`libsql-client-dotnet`](https://github.com/tursodatabase/libsql-client-dotnet)
   package and point `ConnectionStrings__Default` at the Turso URL — no other
   code changes needed.
2. **Supabase Postgres** (free 500 MB). Swap
   `Microsoft.EntityFrameworkCore.Sqlite` for `Npgsql.EntityFrameworkCore.PostgreSQL`
   and change `UseSqlite(...)` to `UseNpgsql(...)` in `Program.cs`.

## Roadmap (not built)

These were intentionally left out of the MVP — they're easy follow-ups:

- Manual "review this PR" trigger from the dashboard
- Per-repo settings (review style, ignored paths)
- Token-usage analytics
- Reply handling on inline comments

## License

MIT — see [LICENSE](LICENSE). Test review trigger
