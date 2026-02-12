# Quizory Backend (independent project)

Ovo je zaseban **ASP.NET Core Web API** projekat i može da se koristi potpuno nezavisno od frontend aplikacije.

## Pokretanje
```bash
cd backend/Quizory.Api
dotnet run
```

Ili iz solution-a:
```bash
cd backend
dotnet build Quizory.sln
dotnet run --project Quizory.Api
```

## API
- Swagger: `/swagger`
- Health check: `/health`

## Ključne osobine
- Multi-tenant model (Organization scoped podaci)
- RBAC (Owner/Admin/User)
- Subscription enforcement (Free/Trial/Premium)
- Kviz scoring + helps + ranking
- Localization (sr/en)

## Konfiguracija CORS
Dozvoljene origin-e zadaj kroz environment varijablu:

```bash
QUIZORY_ALLOWED_ORIGINS=http://localhost:5173;https://my-frontend.com
```

Ako nije podešeno, backend radi nezavisno i dozvoljava lokalne dev origin-e.
