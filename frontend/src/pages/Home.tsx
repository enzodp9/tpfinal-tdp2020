import { useEffect, useMemo, useRef, useState } from "react";
import { api } from "@/lib/axios";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardFooter, CardHeader } from "@/components/ui/card";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { toast } from "sonner";
import { Link } from "react-router-dom";
import { Search } from "lucide-react";


/* Tipo que representa un ítem de película en la lista de resultados */
type MovieListItemDto = {
  imdbId: string;
  title: string;
  type: "movie" | "series" | string;
  genre?: string | null;
  poster?: string | null;
  imdbRating?: number | null;
  year?: number | null; 
}; 

// Géneros disponibles (según OMDB)
const GENRES = [
  "Action","Adventure","Animation","Biography","Comedy","Crime","Documentary","Drama",
  "Family","Fantasy","History","Horror","Music","Musical","Mystery","Romance","Sci-Fi",
  "Sport","Thriller","War","Western",
];

/**
 * Página de inicio con búsqueda y filtros de películas.
 * Permite buscar por título, género y tipo (película o serie).
 * Muestra resultados en una grilla de tarjetas.
 * Utiliza debounce para optimizar búsquedas.
 * Muestra mensajes cuando no hay resultados o no se ha buscado aún.
 */
export default function Home() {
  // Filtros (servidor)
  const [title, setTitle] = useState("");
  const [genre, setGenre] = useState<string>("");                           // "" = sin filtro
  const [type, setType] = useState<"" | "movie" | "series">("");            // "" = sin filtro

  // Resultados
  const [loading, setLoading] = useState(false);
  const [items, setItems] = useState<MovieListItemDto[]>([]);
  const [autoFetched, setAutoFetched] = useState(false);

  // Debounce
  const timer = useRef<number | null>(null);
  const canSearch = useMemo(
    () =>
      title.trim().length >= 2 ||
      genre.trim().length > 0 ||
      type.trim().length > 0,
    [title, genre, type]
  );

  useEffect(() => {
    if (!canSearch) {
      setItems([]);
      setAutoFetched(false);
      return;
    }
    if (timer.current) window.clearTimeout(timer.current);
    timer.current = window.setTimeout(() => {
      void search();
    }, 400);
    return () => {
      if (timer.current) window.clearTimeout(timer.current);
    };
  }, [title, genre, type]);

  async function search() {
    if (!canSearch) return;
    setLoading(true);
    try {
      const { data } = await api.get<MovieListItemDto[]>("/movies/search", {
        params: {
          title,
          genre: genre || undefined,      // Si es = a Todos, no lo mandamos
          type: type || undefined,        // Si es = a Todos, no lo mandamos
        },
      });
      setItems(data ?? []);
      setAutoFetched(true);
    } catch (e: any) {
      const msg = e?.response?.data?.error ?? "No se pudo realizar la búsqueda";
      toast.error(msg);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="max-w-6xl mx-auto">
      {/* Header */}
      <div className="mb-8 text-center">
        <h1 className="text-3xl font-bold tracking-tight">🎬 Buscá tu próxima película</h1>
        <p className="text-gray-600">
          Filtrá por <span className="font-medium">título</span>, <span className="font-medium">género</span> o <span className="font-medium">tipo</span>.
        </p>
      </div>

      {/* Search + filtros */}
      <div className="flex flex-col gap-4 mb-6">
        <div className="flex gap-2 max-w-xl mx-auto w-full">
          <div className="relative flex-1">
            <Input
              placeholder="Busca por título o ImdbId..."
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              onKeyDown={(e) => e.key === "Enter" && search()}
              className="pl-10"
            />
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
          </div>
          <Button onClick={search} disabled={!canSearch || loading}>
            {loading ? "Buscando..." : "Buscar"}
          </Button>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-3 max-w-3xl mx-auto w-full">
          {/* Género */}
          <div>
            <div className="text-xs text-gray-500 mb-1">Género</div>
            <Select
              // usamos "all" para representar "Todos" en el Select, y lo traducimos a "" en el estado
              value={genre || "all"}
              onValueChange={(v) => setGenre(v === "all" ? "" : v)}
            >
              <SelectTrigger>
                <SelectValue placeholder="Todos" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Todos</SelectItem>
                {GENRES.map((g) => (
                  <SelectItem key={g} value={g}>
                    {g}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Tipo */}
          <div>
            <div className="text-xs text-gray-500 mb-1">Tipo</div>
            <Select
              value={type || "all"}
              onValueChange={(v) => setType((v === "all" ? "" : v) as "" | "movie" | "series")}
            >
              <SelectTrigger>
                <SelectValue placeholder="Todos" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Todos</SelectItem>
                <SelectItem value="movie">Película</SelectItem>
                <SelectItem value="series">Serie</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>
      </div>

      {/* Mensajes */}
      {!autoFetched && items.length === 0 && !loading && (
        <div className="text-center text-gray-500">Escribí al menos 2 caracteres para buscar.</div>
      )}
      {autoFetched && items.length === 0 && !loading && (
        <div className="text-center text-gray-500">😕 No se encontraron resultados.</div>
      )}

      {/* Grid de resultados */}
      <div className="grid grid-cols-[repeat(auto-fill,minmax(180px,1fr))] gap-6">
        {items.map((m) => (
          <Card key={m.imdbId} className="overflow-hidden hover:shadow-lg transition-shadow">
            <Link to={`/movies/${m.imdbId}`}>
              <CardHeader className="p-0">
                {m.poster && m.poster !== "N/A" ? (
                  <img
                    src={m.poster}
                    alt={m.title}
                    className="w-full h-60 object-cover transition-transform hover:scale-105"
                    loading="lazy"
                  />
                ) : (
                  <div className="w-full h-60 flex items-center justify-center bg-gray-100 text-gray-400">
                    Sin imagen
                  </div>
                )}
              </CardHeader>
              <CardContent className="p-3">
                <div className="font-medium text-sm line-clamp-2">{m.title}</div>
                <div className="text-xs text-gray-500 mt-1 flex items-center gap-2">
                  {m.year != null ? <span>Año {m.year}</span> : null}
                  {m.type ? <span className="uppercase tracking-wide">{m.type}</span> : null}
                </div>
              </CardContent>
              <CardFooter className="p-3 pt-0">
                <span className="text-xs underline text-gray-700">Ver detalle</span>
              </CardFooter>
            </Link>
          </Card>
        ))}
      </div>
    </div>
  );
}
