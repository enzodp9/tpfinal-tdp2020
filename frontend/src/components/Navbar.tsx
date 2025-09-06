import { Link, useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { useAuth } from "@/context/auth";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { fileUrl } from "@/lib/utils";


/**
 * Barra de navegación superior con opciones de autenticación y menú de usuario.
 */
export default function Navbar() {
  const { user, logout } = useAuth(); // Contexto de autenticación
  const nav = useNavigate();

  const src = user?.avatarUrl
    ? `${fileUrl(user.avatarUrl)}?v=${encodeURIComponent(user.avatarUrl)}`
    : undefined; // URL del avatar con cache-busting

  return (
    <nav className="bg-white border-b">
      <div className="max-w-screen-2xl mx-auto w-full px-6 py-3 flex items-center justify-between">
        {/* Izquierda: brand */}
        <div className="flex items-center gap-6">
          <Link to="/" className="font-semibold">
            Taller de Programación
          </Link>
        </div> 

        {/* Derecha: auth / menú de usuario */}
        <div className="flex items-center gap-2">
          {!user ? (
            <>
              <Link to="/login">
                <Button variant="ghost" size="sm">
                  Ingresar
                </Button>
              </Link>
              <Link to="/register">
                <Button size="sm">Crear cuenta</Button>
              </Link>
            </>
          ) : (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <button className="flex items-center gap-2">
                  <Avatar className="h-8 w-8">
                    <AvatarImage src={src} alt={user?.username} />
                    <AvatarFallback>
                      {(user?.fullname ?? user?.username ?? "?")
                        .slice(0, 1)
                        .toUpperCase()}
                    </AvatarFallback>
                  </Avatar>
                  <span className="text-sm">{user.username}</span>
                </button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-48">
                <DropdownMenuLabel>Cuenta</DropdownMenuLabel>
                <DropdownMenuSeparator />
                <DropdownMenuItem onClick={() => nav("/profile")}>
                  Mi perfil
                </DropdownMenuItem>
                <DropdownMenuItem onClick={() => nav("/watchlist")}>
                  Mi lista
                </DropdownMenuItem>
                <DropdownMenuSeparator />
                {user?.role === "administrator" && (
                  <DropdownMenuItem onClick={() => nav("/admin/users")}>
                    Administración
                  </DropdownMenuItem>
                )}
                <DropdownMenuItem
                  onClick={() => {
                    logout();
                    nav("/");
                  }}
                >
                  Salir
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          )}
        </div>
      </div>
    </nav>
  );
}
