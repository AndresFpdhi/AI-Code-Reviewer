# Local development secrets

Drop your GitHub App's private key here as `github-app.pem`. The path is mounted
read-only into the API container at `/secrets/github-app.pem` and read at startup
via `GitHubApp__PrivateKeyPath`.

This directory (except for `.gitkeep` and this README) is gitignored.

For Render, paste the PEM contents directly into the `GitHubApp__PrivateKeyPem`
environment variable instead — Render's UI handles multi-line values.
