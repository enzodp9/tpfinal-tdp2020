import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Film } from "lucide-react";

/**
 * Página de no encontrada (404).
 * Indica que la ruta solicitada no existe o fue movida.
 * Ofrece enlace para volver al inicio y seguir navegando.
*/
export default function NotFound() {
  return (
    <div className="flex flex-col items-center justify-center min-h-[70vh] text-center px-4">
      <Film className="w-16 h-16 text-gray-400 mb-4" />
      <h1 className="text-4xl font-bold mb-2">404</h1>
      <p className="text-lg text-gray-600 mb-6">
        Oops... La página que buscás no existe.
      </p>
      <Link to="/">
        <Button>Volver al inicio</Button>
      </Link>
    </div>
  );
}
