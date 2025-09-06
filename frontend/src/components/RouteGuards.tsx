import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "@/context/auth";

/**
 * Componente que se muestra mientras se verifica la autenticación.
 */
function AuthLoading() {
  return <div className="text-sm text-gray-500">Cargando…</div>;
}

/**
 * Protege rutas que requieren usuario autenticado.
 * - Si está cargando, muestra un indicador.
 * - Si no está autenticado, redirige a /login.
 * - Si está autenticado, renderiza el contenido hijo.
 */
export function RequireAuth() {
  const { user, loading } = useAuth();
  const loc = useLocation();
  if (loading) return <AuthLoading />;
  if (!user) return <Navigate to="/login" replace state={{ from: loc }} />;
  return <Outlet />;
}

/**
 * Protege rutas exclusivas para usuarios no autenticados.
 * - Si está cargando, muestra un indicador.
 * - Si ya está autenticado, redirige a la ruta previa o a la raíz (/).
 * - Si no está autenticado, renderiza el contenido hijo.
 */
export function RequireAnon() {
  const { user, loading, firstUser } = useAuth();
  const loc = useLocation();
  if (loading || firstUser === null) return <AuthLoading />; 
  if (user) {
    const back = (loc.state as any)?.from?.pathname || "/";
    return <Navigate to={back} replace />;
  }
  return <Outlet />;
}

/**
 * Protege rutas que requieren un rol específico.
 * - Si está cargando, muestra un indicador.
 * - Si no está autenticado, redirige a /login.
 * - Si está autenticado pero no tiene el rol requerido, redirige a la raíz (/).
 * - Si cumple el rol, renderiza el contenido hijo.
 *
 * @param role Rol requerido (ejemplo: "administrator").
 */
export function RequireRole({ role }: { role: string }) {
  const { user, loading } = useAuth();
  const loc = useLocation();
  if (loading) return <AuthLoading />;
  if (!user) return <Navigate to="/login" replace state={{ from: loc }} />;
  if (user.role !== role) return <Navigate to="/" replace />;
  return <Outlet />;
}
