import { createContext, useContext, useEffect, useState } from "react";
import { api } from "@/lib/axios";
import { toast } from "sonner";

/** Tipo que representa al usuario autenticado o null si no hay sesión */
type User = {
  id: string;
  username: string;
  fullname: string;
  role: string;
  avatarUrl?: string | null;
} | null;

/** Tipo del contexto de autenticación */
type AuthContextType = {
  user: User;
  loading: boolean;
  login: (username: string, password: string) => Promise<boolean>;
  register: (
    username: string,
    fullname: string,
    password: string
  ) => Promise<boolean>;
  logout: () => void;
  refreshMe: () => Promise<void>;
  firstUser: boolean | null;
  bootLoading: boolean;
};

/** Contexto de autenticación */
const AuthContext = createContext<AuthContextType>({} as any);

/** Proveedor del contexto de autenticación */
export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User>(null);
  const [loading, setLoading] = useState(true);
  const [firstUser, setFirstUser] = useState<boolean | null>(null); // null = no checkeado aún

  async function checkFirstUser() {
    try {
      const { data } = await api.get<{ exists: boolean }>("/users/exists");
      setFirstUser(!data.exists); // true si NO hay usuarios
    } catch {
      setFirstUser(false); // si falla, asumimos que sí hay usuarios
    }
  }

  useEffect(() => {
    fetchMe();
    checkFirstUser(); // consulta al montar el proveedor
  }, []);

  async function fetchMe() {
    setLoading(true);
    const token = localStorage.getItem("token");
    if (!token) {
      setUser(null);
      setLoading(false);
      return; // no hay token, no hay sesión
    }
    try {
      const { data } = await api.get("/auth/me"); // obtiene datos del usuario
      setUser({
        id: data.id,
        username: data.username,
        fullname: data.fullname ?? data.fullName ?? "", // soporte camelCase y PascalCase
        role: data.role,
        avatarUrl: data.avatarUrl ?? null,
      }); // actualiza estado de usuario
    } catch {
      setUser(null); // si hay error, no hay sesión
    } finally {
      setLoading(false); // ya no está cargando
    }
  }

  const refreshMe = () => fetchMe(); // función para refrescar datos del usuario

  async function login(username: string, password: string) {
    try {
      const { data } = await api.post("/auth/login", { username, password });
      localStorage.setItem("token", data.token);
      await fetchMe();
      return true;
    } catch (e: any) {
      const msg = e?.response?.data?.error ?? "No se pudo iniciar sesión";
      toast.error(msg);
      return false;
    }
  } // función para iniciar sesión

  async function register(
    username: string,
    fullname: string,
    password: string
  ) {
    try {
      await api.post("/auth/register", { username, fullname, password });
      toast.success("Cuenta creada");
      setFirstUser(false);
      return await login(username, password);
    } catch (e: any) {
      const msg = e?.response?.data?.error ?? "No se pudo registrar";
      toast.error(msg);
      return false;
    }
  } // función para registrar usuario

  function logout() {
    localStorage.removeItem("token");
    setUser(null);
    toast.success("Sesión cerrada");
  } // función para cerrar sesión

  const bootLoading = loading || firstUser === null;

  return (
    <AuthContext.Provider
      value={{
        user,
        loading,
        login,
        register,
        logout,
        refreshMe,
        firstUser,
        bootLoading,
      }}
    >
      {children}
    </AuthContext.Provider>
  ); // renderiza proveedor con valores y children
}

export function useAuth() {
  return useContext(AuthContext);
} // hook para usar el contexto de autenticación
