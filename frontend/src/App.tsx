import { Routes, Route, Navigate, useLocation } from "react-router-dom";
import Navbar from "@/components/Navbar";
import Home from "@/pages/Home";
import Login from "@/pages/Login";
import Register from "@/pages/Register";
import MovieDetail from "@/pages/MovieDetail";
import Watchlist from "@/pages/WatchList";
import Profile from "@/pages/Profile";
import { Toaster } from "sonner";
import {
  RequireAuth,
  RequireAnon,
  RequireRole,
} from "@/components/RouteGuards";
import AdminUsers from "./pages/AdminUsers";
import NotFound from "./pages/NotFound";
import { useAuth } from "./context/auth";

/**
 * Componente principal de la aplicación.
 * Define la estructura general y las rutas.
 * Utiliza RouteGuards para proteger rutas según el estado de autenticación y roles.
 */
export default function App() {
  const { firstUser, bootLoading } = useAuth();
  const loc = useLocation();

  if (bootLoading) {
    return <div className="text-sm text-gray-500 p-6">Cargando…</div>;
  }


  return (
    <div className="min-h-screen bg-gray-50">
      {firstUser && loc.pathname !== "/register" && (
        <Navigate to="/register" replace />
      )}
      <Navbar />
      <main className="w-full max-w-screen-2xl mx-auto px-6 py-8">
        <Routes>
          {/* Públicas */}
          <Route path="/" element={<Home />} />
          <Route path="/movies/:id" element={<MovieDetail />} />

          {/* Solo si NO hay sesión */}
          <Route element={<RequireAnon />}>
            <Route path="/login" element={<Login />} />
            <Route path="/register" element={<Register />} />
          </Route>

          {/* Requiere sesión */}
          <Route element={<RequireAuth />}>
            <Route path="/watchlist" element={<Watchlist />} />
            <Route path="/profile" element={<Profile />} />
          </Route>

          {/* Requiere admin */}
          <Route element={<RequireRole role="administrator" />}>
            <Route path="/admin/users" element={<AdminUsers />} />
          </Route>

          {/* 404  */}
          <Route path="*" element={<NotFound />} />
        </Routes>
      </main>
      <Toaster richColors closeButton />
    </div>
  );
}
