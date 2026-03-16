# Student Dashboard

A web-based student internship and thesis management dashboard for tracking stage (internship) and scriptie (thesis) students.

## Deploying to Render.com (Free Plan)

This app has been converted from a Windows desktop (WPF) app to an ASP.NET Core Razor Pages web app that can run on Render.com's **free plan**.

### One-click deploy via render.yaml

1. Fork this repository to your GitHub account.
2. Go to [render.com](https://render.com) and create a new account (or log in).
3. Click **New → Blueprint** and connect your GitHub repository.
4. Render will detect `render.yaml` and automatically create:
   - A **Web Service** (Docker-based) running the ASP.NET Core app.
   - A **PostgreSQL database** (free tier).
5. Click **Apply** and wait for the deploy to complete (~5–10 minutes on first build).

### Manual deploy

1. Create a **PostgreSQL** database on Render (free tier).
2. Create a **Web Service** on Render:
   - **Runtime**: Docker
   - **Repository**: this repo
   - **Dockerfile path**: `./Dockerfile`
3. Add the environment variable:
   - `DATABASE_URL` → use the **Internal Database URL** from the PostgreSQL instance.

### Free plan limitations to be aware of

| Feature | Free plan |
|---|---|
| Web service uptime | Spins down after **15 minutes of inactivity**; ~30-second cold start |
| PostgreSQL storage | 1 GB |
| PostgreSQL lifetime | **Deleted after 90 days** (data loss!) |
| Bandwidth | 100 GB/month |
| File uploads | Not persisted (ephemeral filesystem) — attachments are disabled on Render |

> **Important:** The free PostgreSQL database is **automatically deleted after 90 days**. To keep your data, either upgrade to a paid plan or export your data regularly.

### Running locally

Requirements: [.NET 10 SDK](https://dotnet.microsoft.com/download)

```bash
cd WebApp
dotnet run
```

The app will start on `http://localhost:5000` using a local SQLite file (`students.db`).

### Features

- 📋 Dashboard with active student list and summary statistics
- ➕ Add, edit, and archive students
- 📂 Stage (internship) and Scriptie (thesis) type support
- ✅ Workflow progress tracking (Opstart → PvA → Concept 1 → … → Afgerond)
- 📅 Deadline management with urgency alerts
- 💬 Contact moment logging
- 🔍 Search by name, student number, or company
