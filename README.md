# Quizory

Repo je organizovan kao dva **zasebna projekta**:

- `backend/` → nezavisan ASP.NET Core Web API projekat
- `frontend/` → nezavisan React + TypeScript projekat

## 1) Backend (samostalno)
Detalji: `backend/README.md`

```bash
cd backend/Quizory.Api
dotnet run
```

Backend ima:
- Swagger (`/swagger`)
- Health endpoint (`/health`)
- Multi-tenant + RBAC + subscription logiku
- SR/EN lokalizaciju

## 2) Frontend (samostalno)
Detalji: `frontend/README.md`

```bash
cd frontend
npm install
npm run dev
```

Frontend API URL se podešava kroz `VITE_API_URL`, npr:

```bash
VITE_API_URL=http://localhost:5000/api npm run dev
```

## Napomena
Backend može da se koristi potpuno nezavisno od frontend-a (npr. mobilna aplikacija, drugi SPA, integracije).
