# TodoApi

A scalable TODO list API built with ASP.NET Core and MySQL.

## Features
- CRUD for TODOs (with filtering and sorting)
- User registration and authentication (JWT)
- DTOs, AutoMapper, FluentValidation
- Swagger API documentation
- MySQL database (via EF Core)

## Setup

1. **Clone the repo**
2. **Configure MySQL**
   - Update `appsettings.json` with your MySQL connection string.
3. **Run EF Core migrations**
   - `dotnet ef migrations add InitialCreate -p TodoApi`
   - `dotnet ef database update -p TodoApi`
4. **Run the API**
   - `dotnet run --project TodoApi`
5. **Swagger UI**
   - Visit `http://localhost:5000/swagger` (or the port shown in the console)

## API Endpoints

### Auth
- `POST /api/auth/register` — Register a new user
- `POST /api/auth/login` — Login and get JWT token

### Todos (JWT required)
- `GET /api/todo` — List TODOs (filter/sort via query params)
- `GET /api/todo/{id}` — Get TODO by ID
- `POST /api/todo` — Create TODO
- `PUT /api/todo/{id}` — Update TODO
- `DELETE /api/todo/{id}` — Delete TODO

## Testing
- Add tests in the `Tests` folder (not included in this scaffold)
- Run tests: `dotnet test`

## Notes
- Update JWT secret in `appsettings.json` for production.
- Add more features (teams, tags, real-time, etc.) as needed. 