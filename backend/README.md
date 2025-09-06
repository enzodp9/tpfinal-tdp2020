# TPFinal API — ASP.NET Core 9 + EF Core

API para autenticación de usuarios, catálogo de películas/series, ratings y watchlist. Integra OMDb para completar datos de películas y expone documentación con Swagger.

## Tecnologías

- ASP.NET Core 9
- Entity Framework Core
- SQLite (por defecto) / SQL Server (opcional)
- JWT Bearer Auth
- Swagger (Swashbuckle)

## Requisitos

- .NET SDK 9
- (Opcional) EF Core CLI: `dotnet tool install --global dotnet-ef`

## Configuración

Las variables de entorno se cargan con DotNetEnv desde un archivo `.env` en `TPFinal.Api`.

Ejemplo (`tdp-tpfinal/server/TPFinal.Api/.env`):

```
CONNECTIONSTRINGS__DEFAULT=Data Source=./data/tppelis.db
JWT__ISSUER=tp.api
JWT__AUDIENCE=tp.client
JWT__KEY=<clave_secreta_jwt>
OMDB__APIKEY=<tu_api_key_omdb>
OMDB__BASEURL=https://www.omdbapi.com/
```

- CORS: por defecto se permite `http://localhost:5173` (Vite). Se puede ajustar en `Program.cs` (política `client`).

## Ejecución (desarrollo)

```
cd tdp-tpfinal/server/TPFinal.Api
dotnet restore
dotnet run
```

- Base URL: `http://localhost:5080`
- API base: `http://localhost:5080/api`
- Swagger: `http://localhost:5080/swagger`
- Ping rápido: `http://localhost:5080/ping`

> El perfil de `launchSettings.json` expone `http://localhost:5080` en Development.

## Base de datos

- Por defecto SQLite en `./data/tppelis.db` (creado automáticamente en primer uso).
- Migraciones en la carpeta `Migrations`.

Comandos útiles:

```
# Aplicar migraciones
dotnet ef database update

# Crear nueva migración
dotnet ef migrations add NombreMigracion
```

Usar SQL Server (opcional):

1) Cambiar `CONNECTIONSTRINGS__DEFAULT` a tu cadena de conexión SQL Server (por ejemplo, LocalDB o SQL Server).  
2) En `Program.cs`, reemplazar `UseSqlite` por `UseSqlServer` (ya hay un snippet comentado).

## Endpoints principales (resumen)

- Auth (`/api/auth`):
  - `POST /register` — body: `Username`, `FullName`, `Password`, `IsAdmin?`
  - `POST /login` — devuelve JWT
  - `GET /me` — requiere JWT

- Movies (`/api/movies`):
  - `GET /{imdbId}` — detalle por IMDb ID
  - `GET /search?imdbId=&title=&genre=&type=` — busca; si no existen localmente, trae de OMDb y persiste

- Watchlist (`/api/watchlist`, requiere JWT):
  - `GET /` — mi lista
  - `POST /` — body: `ImdbId`, `Position?`
  - `DELETE /{imdbId}`
  - `PATCH /reorder` — body: `ImdbId`, `NewPosition`

- Users (`/api/users`, requiere rol `administrator`):
  - `GET /` (listar con `?q=`), `GET /{id}`
  - `POST /` (upsert), `DELETE /{id}`
  - `POST /{id}/password`
  - Perfil: `GET /me`, `PATCH /me`, `POST /me/avatar` (multipart/form-data)

## Integración con el Frontend

- El cliente consume `VITE_API_URL` (por defecto `http://localhost:5080/api`).
- Ver `client/.env` y `client/src/lib/axios.ts` para configuración de baseURL y manejo de tokens.

