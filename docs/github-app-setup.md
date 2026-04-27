# GitHub App setup

This bot integrates with GitHub via a **GitHub App**, not a personal access token.
The App receives PR webhooks and posts reviews under its own bot identity.

## 1. Create the GitHub App

1. Go to **Settings → Developer settings → GitHub Apps → New GitHub App**
   (or `https://github.com/settings/apps/new` for a personal account, or
   `https://github.com/organizations/<org>/settings/apps/new` for an org).
2. Fill in the form:
   - **GitHub App name**: e.g. `ai-code-reviewer-yourname`
   - **Homepage URL**: your repo URL (or the live demo URL once deployed)
   - **Webhook URL**: `https://<your-render-url>/api/github/webhook`
     (for local dev see *Local development* below)
   - **Webhook secret**: generate a random string, keep it for the env var
   - Uncheck **Active** for "Expire user authorization tokens" (not used here)

## 2. Permissions

Under **Repository permissions** set:

| Permission       | Access     |
|------------------|------------|
| Contents         | Read-only  |
| Metadata         | Read-only  |
| Pull requests    | Read & write |

## 3. Subscribe to events

Under **Subscribe to events**, check **Pull request**.

## 4. Generate a private key

After creating the App, scroll to **Private keys** and click **Generate a private key**.
A `.pem` file will download. You'll paste its contents into `GITHUB_APP_PRIVATE_KEY`.

## 5. Install the App on a repo

From the App's page, click **Install App** and select the repo(s) you want
the bot to review.

## 6. Wire up env vars

For local development, create `.env` in the repo root (gitignored):

```
GITHUB_APP_ID=123456
GITHUB_APP_PRIVATE_KEY="-----BEGIN RSA PRIVATE KEY-----
...
-----END RSA PRIVATE KEY-----"
GITHUB_WEBHOOK_SECRET=the-secret-you-chose
GROQ_API_KEY=gsk_xxxxxxxxxxxx
```

For Render, paste the same values into the service's **Environment** tab.

## Local development with smee.io

GitHub needs a public URL for webhooks. For local dev:

```bash
# install smee CLI
npm install -g smee-client

# get a smee channel from https://smee.io/new (copy the URL it gives you)
smee -u https://smee.io/<your-channel> -t http://localhost:5000/api/github/webhook
```

Then set the GitHub App's webhook URL to the smee channel URL temporarily,
and run `docker compose up`. Open a PR — webhook events flow through smee
into your local API.
