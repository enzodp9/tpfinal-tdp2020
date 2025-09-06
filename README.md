# TP Final - TDP 2020

Repositorio del Trabajo Práctico Final de la cátedra Taller de Programación 2020 de la carrera Ingeniería en Sistemas de Información de Universidad Tecnológica Nacional- Facultad Regional Concepción del Uruguay.

Este proyecto consiste en una aplicación web de gestión de películas.

## Estructura del repositorio

- **frontend/**: Contiene el código del cliente (React + Vite + TypeScript).  
  - `README.md` específico con instrucciones de instalación y ejecución.  
- **backend/**: Contiene el código del servidor (ASP.NET Core Web API).  
  - `README.md` específico con instrucciones de ejecución y endpoints.  

## Requisitos

- [Node.js](https://nodejs.org/) >= 18 para el frontend
- [npm](https://www.npmjs.com/) o [pnpm](https://pnpm.io/) como gestor de paquetes
- [.NET SDK](https://dotnet.microsoft.com/download) >= 9.0 para el backend
- SQL Server o SQLite (según `backend/.env` o `appsettings.json`)

## Configuración inicial

1. Clonar el repositorio:

```
git clone https://github.com/enzodp9/tpfinal-tdp2020.git
cd tpfinal-tdp2020
```

2. Instalar dependencias del frontend:
```
cd frontend
npm install
```

3. Restaurar paquetes y compilar el backend:
```
cd backend/
dotnet restore
dotnet build
```

5. Configurar las variables de entorno según corresponda

   - **Backend**: crear `backend/.env` (ejemplo)

     ```env
     CONNECTIONSTRINGS__DEFAULT=Data Source=./data/tppelis.db
     JWT__ISSUER=tp.api
     JWT__AUDIENCE=tp.client
     JWT__KEY=<clave_secreta_jwt>
     OMDB__APIKEY=<tu_api_key_omdb>
     OMDB__BASEURL=https://www.omdbapi.com/
     ```

   - **Frontend**: crear `frontend/.env` (ejemplo)

     ```env
     VITE_API_URL=http://localhost:5080/api
     ```

## Ejecución

- **Frontend**:
``` 
cd frontend
npm run dev
```

- **Backend**:
```
cd backend
dotnet run
```
Luego, acceder a http://localhost:5173 (frontend) y http://localhost:5080 (API).
