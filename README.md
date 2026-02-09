# Quizory

Quizory is a multi-tenant SaaS platform for running pub quizzes with live scoring, rankings, and leagues.

## Tech stack
- **Frontend**: React + TypeScript (Vite)
- **Backend**: ASP.NET Core Web API (.NET 8)
- **Database**: EF Core InMemory now (prepared for PostgreSQL/SQL Server)
- **Localization**: Serbian + English on frontend and backend response messages

## Monorepo structure
- `backend/Quizory.Api` - Web API with tenant-aware domain, RBAC, subscription limits, scoring logic.
- `frontend` - Responsive React app with i18n and key screens (dashboard, quizzes, teams, leagues, settings).

## Implemented capabilities
- Multi-tenant data model with organization-scoped entities.
- RBAC (`Owner`, `Admin`, `User`) and admin-level cap enforcement (max 3 incl. owner).
- Subscription model constraints (free-plan member/leagues/question bank/share restrictions + monthly quiz cap).
- Quiz creation flow base: create quiz + generate score matrix for team/category combinations.
- Live scoring with lock flag and automatic ranking.
- Helps support (`Joker` doubles score, `Double Chance` marker) with one-use-per-team-per-quiz constraint.
- Team alias model and alias-based team search endpoint.
- Soft delete fields and audit-log entity scaffold.
- Serbian/English translations in JSON files and UI language switcher with persisted preference.

## Backend run
```bash
cd backend/Quizory.Api
dotnet run
```
API: `http://localhost:5000` (or assigned port), Swagger enabled in development.

Pass headers for context:
- `X-User-Id`
- `X-Organization-Id`
- `Accept-Language: sr|en`

## Frontend run
```bash
cd frontend
npm install
npm run dev
```

## Production-readiness next steps
- Replace InMemory DB with PostgreSQL/SQL Server provider + migrations.
- Add full auth module (register/login/email verification/reset password/JWT refresh).
- Add import/export pipelines (Excel/PDF) and background emails (trial reminder).
- Add comprehensive validation/error localization and automated tests (unit/integration/e2e).
