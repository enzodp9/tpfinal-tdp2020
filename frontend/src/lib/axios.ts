import axios from "axios";


/** Instancia de Axios preconfigurada */
export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? "http://localhost:5080/api",
});


/* Request: agregar token si existe en localStorage */
api.interceptors.request.use((config) => {
  const token = localStorage.getItem("token");
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

/* Response: manejar 401 globalmente */
api.interceptors.response.use(
  (res) => res,
  (err) => {
    const status = err?.response?.status;
    const hadToken = !!localStorage.getItem("token");
    const path = window.location.pathname;

    if (status === 401 && hadToken && path !== "/login" && path !== "/register") {
      localStorage.removeItem("token");
      // opcional: toast acá si querés
      setTimeout(() => { window.location.href = "/login"; }, 300);
    }
    return Promise.reject(err);
  }
);

