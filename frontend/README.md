# TPFinal Client — React + Vite + TypeScript

Cliente web para autenticación, búsqueda de películas, detalle, watchlist y administración de usuarios. Consume la API del backend (`VITE_API_URL`) y maneja autenticación por JWT.

## Tecnologías

- React 19, Vite 7, TypeScript
- TailwindCSS, shadcn/ui, lucide-react
- React Router, React Hook Form, Zod
- Axios (interceptores con JWT)

## Requisitos

- Node.js 18+ (recomendado 20+)
- npm 9+ (o pnpm/bun equivalente)

## Configuración y ejecución

1. Instalar dependencias:

   ```bash
   cd client
   npm install
   ```

2. Variables de entorno (crear `client/.env` si no existe):

   ```env
   VITE_API_URL=http://localhost:5080/api
   ```

3. Desarrollo:

   ```bash
   npm run dev
   # abre http://localhost:5173
   ```

4. Build y preview:

   ```bash
   npm run build
   npm run preview
   ```

5. Lint:

   ```bash
   npm run lint
   ```

## Rutas principales

- Públicas: `/` (Home), `/movies/:id`
- Solo anónimos: `/login`, `/register`
- Autenticadas: `/watchlist`, `/profile`
- Administrador: `/admin/users`
- 404: `*`

## Autenticación

- Tras el login, el token JWT se guarda en `localStorage` como `token`.
- `client/src/lib/axios.ts` agrega automáticamente `Authorization: Bearer <token>`.
- En respuesta `401`, se limpia el token y se redirige a `/login`.

## Integración con el Backend

- `VITE_API_URL` apunta a `http://localhost:5080/api` por defecto.
- El backend tiene CORS habilitado para `http://localhost:5173`.

## Estructura relevante

- `src/main.tsx`: Router + `AuthProvider`.
- `src/App.tsx`: rutas y guards (`RequireAuth`, `RequireAnon`, `RequireRole`).
- `src/lib/axios.ts`: instancia de Axios con interceptores.
