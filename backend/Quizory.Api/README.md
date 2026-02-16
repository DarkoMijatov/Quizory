# Quizory API

Production-ready ASP.NET Core Web API for the Quizory multi-tenant SaaS (pub quiz events, scoring, statistics, leagues).

## Tech Stack

- **.NET 8**, ASP.NET Core Web API  
- **SQL Server** (or InMemory when no connection string)  
- **JWT** authentication (email/password, verification, password reset)  
- **BCrypt** password hashing  
- **EF Core** with soft delete and tenant isolation  
- **ClosedXML** (Excel), **QuestPDF** (PDF) for export/import  
- **Serbian (sr)** and **English (en)** localization (backend messages)

## Configuration

- **appsettings.json** / **appsettings.Development.json**
  - `ConnectionStrings:DefaultConnection` – SQL Server (leave empty for InMemory).
  - `Jwt:Secret` – min 32 characters for HMAC-SHA256.
  - `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpirationMinutes`.

## Run

```bash
dotnet run
```

- Swagger: `https://localhost:53448/swagger` (or port from launchSettings).
- With SQL Server: set `DefaultConnection` and run migrations (or use `EnsureCreatedAsync()` in dev).

## Auth

- **POST /api/auth/register** – Register (creates user + org + owner membership). Body: `{ "email", "password", "displayName?", "organizationName?" }`. Query: `?lang=sr|en`.
- **POST /api/auth/login** – Body: `{ "email", "password" }`. Returns JWT and current org.
- **POST /api/auth/verify-email?token=...** – Verify email.
- **POST /api/auth/password-reset/request** – Body: `{ "email" }`.
- **POST /api/auth/password-reset/confirm** – Body: `{ "token", "newPassword" }`.

Send JWT in `Authorization: Bearer <token>`. For multi-org, send **X-Organization-Id** header with the active org ID.

## API Overview

| Area | Endpoints |
|------|-----------|
| **Organizations** | GET /api/organizations/me, PUT me, GET/POST members, PUT members/{userId}/role, DELETE members/{userId}, POST language |
| **Teams** | GET/POST /api/teams, GET/PUT/DELETE /api/teams/{id}, POST /api/teams/{id}/aliases |
| **Categories** | GET/POST /api/categories, GET/PUT/DELETE /api/categories/{id} |
| **Leagues** (Premium) | GET/POST /api/leagues, GET/PUT/DELETE /api/leagues/{id} |
| **Global settings** | GET/PUT /api/global-settings |
| **Help types** | GET/POST /api/help-types, PUT/DELETE /api/help-types/{id} |
| **Quizzes** | GET/POST /api/quizzes, GET/PUT /api/quizzes/{id}, POST finish, POST scores, POST helps, GET ranking |
| **Question bank** (Premium) | GET/POST /api/questions, GET/PUT/DELETE /api/questions/{id} |
| **Statistics** | GET /api/statistics/quizzes, GET leagues/{id}, GET categories, GET teams/{id}/history |
| **Export/Import** | GET /api/export/template/excel, GET quiz/{id}/excel|pdf, GET league/{id}/excel, POST import/excel |
| **Share** (Premium) | POST /api/share/token, GET /api/share/leaderboard/{token} (anonymous) |
| **Subscription** | GET /api/subscription, POST /api/subscription/trial, POST premium, POST downgrade |
| **Payments** | POST /api/payments, GET /api/payments, GET /api/payments/{id}, POST confirm, POST {id}/confirm |

## Roles & Limits

- **Owner**: one per org, cannot be removed; can manage subscription, members, and assign Admin.
- **Admin**: manage quizzes, leagues, teams, categories, helps, import/export; cannot change Owner or other Admins.
- **User**: create/manage quizzes, enter scores only.
- **Admin cap**: max 3 admin-level accounts per org (1 Owner + 2 Admins).

## Subscription

- **Free**: single org, owner only, max 5 quizzes/month, no leagues/question bank/share/members. New organizations default to Free.
- **Trial**: 14 days premium (same features as Premium); can only be started from Free. Reminder email 5 days before expiry; expired trials are auto-downgraded to Free (daily job).
- **Premium**: members, up to 2 Admins, unlimited quizzes, leagues, question bank, share, custom branding.

**Subscription API** (Owner only for write): **GET /api/subscription** returns current plan, trial end (if any), usage (quizzes this month, member count), limits, and feature flags. **POST /api/subscription/trial** starts a 14-day trial (from Free). **POST /api/subscription/premium** sets Premium. **POST /api/subscription/downgrade** sets Free (fails if org has more than one member).

**Payment API** (Owner only): Payments are for **Premium** plan only. **POST /api/payments** body: `{ "plan": "Premium", "amount": 9.99, "currency": "EUR" }` creates a Pending payment and returns `PaymentDto` (id, amount, status, optional clientSecret for future Stripe). **GET /api/payments** lists payments (paginated). **GET /api/payments/{id}** gets one. **POST /api/payments/confirm** body: `{ "externalPaymentId": "..." }` or **POST /api/payments/{id}/confirm** marks payment Completed and sets organization to Premium.

## Seed (InMemory)

- User: `owner@quizory.local` / `Password1!`  
- One Trial organization with default help types (Joker, Double Chance).

## Localization

- `Accept-Language: en` or user’s stored `preferred_language` (from JWT or profile) drives backend messages (e.g. validation, errors) via `ITextLocalizer` (Serbian/English).

## Security

- JWT required on all endpoints except auth and **GET /api/share/leaderboard/{token}**.
- Tenant isolation by **OrganizationId**; **X-Organization-Id** selects current org.
- Soft delete on tenant entities; global query filters applied in DbContext.
