# Architecture

```
┌──────────────┐   PR opened/synced     ┌────────────────────────────┐
│   GitHub     │ ─────webhook─────────► │  CodeReviewer.Api          │
│  (GH App)    │                        │  ├─ HMAC-SHA256 verify     │
│              │ ◄────review posted──── │  ├─ Enqueue ReviewRequest  │
└──────────────┘                        │  └─ 202 Accepted           │
                                        └─────────────┬──────────────┘
                                                      │
                                                      ▼
                                        ┌────────────────────────────┐
                                        │  ReviewQueueProcessor       │
                                        │  (BackgroundService)        │
                                        └─────────────┬──────────────┘
                                                      │
                                                      ▼
                                        ┌────────────────────────────┐
                                        │  ReviewService              │
                                        │  ├─ GitHubAppClient (JWT)   │
                                        │  │   └─ fetch PR + diff     │
                                        │  ├─ GroqClient              │
                                        │  │   └─ llama-3.3-70b       │
                                        │  ├─ post review on PR       │
                                        │  └─ persist via repository  │
                                        └─────────────┬──────────────┘
                                                      │
                                                      ▼
                                        ┌────────────────────────────┐
                                        │  AppDbContext (EF Core)     │
                                        │  Reviews → SQLite           │
                                        └─────────────┬──────────────┘
                                                      │
                                                      ▼
                                        ┌────────────────────────────┐
                                        │  ReviewsController          │
                                        │  GET /api/reviews           │
                                        └─────────────┬──────────────┘
                                                      │
                                                      ▼
                                        ┌────────────────────────────┐
                                        │  Angular SPA (Vercel)       │
                                        │  list + detail views        │
                                        └────────────────────────────┘
```

## Project layout

| Project              | Responsibility                                                      |
|----------------------|---------------------------------------------------------------------|
| `CodeReviewer.Api`   | HTTP endpoints, signature middleware, background queue processor    |
| `CodeReviewer.Core`  | Domain entities, services, models — no ASP.NET or EF dependencies   |
| `CodeReviewer.Data`  | EF Core `DbContext`, repository implementation, migrations          |
| `CodeReviewer.Tests` | xUnit unit tests for prompt builder, signature middleware, service  |

## Why the layers split this way

- **Core has no infrastructure deps.** It defines `IReviewRepository` so the
  orchestration logic in `ReviewService` is unit-testable without a database.
- **Data depends on Core**, not the other way around. The `Review` entity lives
  in `Core/Entities` because it's a domain concept; only the EF mapping/migrations
  live in `Data`.
- **Api wires everything together** at startup and owns the HTTP-facing
  concerns (controllers, middleware, hosted services).

## Request flow for a webhook

1. GitHub `POST /api/github/webhook` with `X-Hub-Signature-256` header.
2. `GitHubSignatureMiddleware` reads the raw body, computes
   `HMAC-SHA256(secret, body)`, and constant-time compares with the header.
   On mismatch → 401, before model binding ever runs.
3. `GitHubWebhookController.Receive` filters to `pull_request` events with
   action `opened` / `synchronize` / `reopened`, parses the JSON, and pushes
   a `ReviewRequest` onto a bounded `Channel<T>`. Returns **202 Accepted** so
   GitHub doesn't time out and retry.
4. `ReviewQueueProcessor` (a `BackgroundService`) drains the channel,
   resolving a scoped `IReviewService` per item.
5. `ReviewService`:
   - Calls `IGitHubAppClient.GetPullRequestContextAsync` — generates an
     RS256 JWT, exchanges it for an installation token, fetches PR + diff.
   - Calls `IGroqClient.ReviewAsync` — POSTs to the OpenAI-compatible
     Groq endpoint with `response_format: { type: "json_object" }`.
   - Calls `IGitHubAppClient.PostReviewAsync` — posts a `COMMENT` review
     with inline comments (falls back to summary-only if inline comments
     are rejected, e.g. when the AI cites a line outside the diff).
   - Calls `IReviewRepository.AddAsync` — persists the row for the
     dashboard.
