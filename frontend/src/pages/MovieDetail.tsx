import { useEffect, useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { api } from "@/lib/axios";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { Card, CardContent } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import { toast } from "sonner";
import { useAuth } from "@/context/auth";
import StarPicker from "@/components/StarPicker";
import { fileUrl } from "@/lib/utils";
import { Star } from "lucide-react";

/* Tipo que representa el detalle de una película */
type MovieDetailDto = {
  imdbId: string;
  title: string;
  type: "movie" | "series";
  genre?: string | null;
  released?: string | null;          // "yyyy-MM-dd"
  runtimeMinutes?: number | null;
  poster?: string | null;
  country?: string | null;
  imdbRating?: number | null;
  director?: string | null;
  writer?: string | null;
  actors?: string | null;
  year?: number | null;
};

/* Tipo que representa el resumen de calificaciones de una película */
type SummaryDto = {
  imdbId: string;
  title: string;
  count: number;
  average: number;
};

/* Tipo que representa una calificación */
type Rating = {
  id: string;
  imdbId: string;
  title: string;
  qualification: number;
  comment?: string | null;
  date: string;
  username: string;
  fullname: string;
  avatarUrl?: string | null;
};


/**
 * Página de detalle de película.
 * Muestra información detallada, permite agregar a watchlist y calificar.
 * Muestra reseñas de otros usuarios.
 */
export default function MovieDetail() {
  const { id } = useParams<{ id: string }>(); // imdbId
  const { user } = useAuth(); // usuario autenticado (si hay)

  const [movie, setMovie] = useState<MovieDetailDto | null>(null); // detalle
  const [summary, setSummary] = useState<SummaryDto | null>(null); // resumen
  const [reviews, setRatings] = useState<Rating[]>([]); // calificaciones
  const [loading, setLoading] = useState(true); // cargando
  const [inWatchlist, setInWatchlist] = useState(false); // en watchlist

  // mi rating
  const [myScore, setMyScore] = useState(0); // 0 = sin calificar
  const [myComment, setMyComment] = useState(""); // mi comentario
  const canRate = useMemo(() => !!user && !!movie, [user, movie]); // puede calificar si está logueado y hay película

  // géneros como array
  const genres = useMemo(
    () =>
      (movie?.genre ?? "")
        .split(",")
        .map((g) => g.trim())
        .filter(Boolean),
    [movie?.genre]
  );

  // carga inicial
  useEffect(() => {
    (async () => {
      if (!id) return;
      try {
        setLoading(true);

        const m = await api.get<MovieDetailDto>(`/movies/${id}`);
        setMovie(m.data);

        const s = await api.get<SummaryDto>(`/ratings/movie/${id}/summary`).catch(() => null);
        if (s?.data) setSummary(s.data);

        const rs = await api.get<Rating[]>(`/ratings/movie/${id}`).catch(() => null);
        if (rs?.data) setRatings(rs.data);

        if (user) {
          const wl = await api.get<any[]>("/watchlist").catch(() => ({ data: [] as any[] }));
          const found = (wl.data ?? []).some((x) => (x.imdbId ?? x.imdbID) === id);
          setInWatchlist(found);

          const mine = await api.get<Rating[]>("/ratings/me").catch(() => null);
          const r = mine?.data?.find((x) => x.imdbId === id);
          if (r) {
            setMyScore(r.qualification);
            setMyComment(r.comment ?? "");
          } else {
            setMyScore(0);
            setMyComment("");
          }
        } else {
          setInWatchlist(false);
          setMyScore(0);
          setMyComment("");
        }
      } catch (e: any) {
        toast.error(e?.response?.data?.error ?? "No se pudo cargar el detalle");
      } finally {
        setLoading(false);
      }
    })();
  }, [id, user]);

  /* Agrega la película a la watchlist */
  async function addToWatchlist() {
    try {
      await api.post("/watchlist", { imdbId: id });
      setInWatchlist(true);
      toast.success("Agregada a tu lista");
    } catch (e: any) {
      toast.error(e?.response?.data?.error ?? "No se pudo agregar");
    }
  }

  /* Quita la película de la watchlist */
  async function removeFromWatchlist() {
    try {
      await api.delete(`/watchlist/${id}`);
      setInWatchlist(false);
      toast.success("Quitada de tu lista");
    } catch (e: any) {
      toast.error(e?.response?.data?.error ?? "No se pudo quitar");
    }
  }

  /* Guarda mi calificación */
  async function saveRating() {
    if (!canRate) {
      toast.info("Iniciá sesión para calificar");
      return;
    }
    if (myScore < 1 || myScore > 5) {
      toast.error("La calificación debe estar entre 1 y 5");
      return;
    }
    try {
      await api.post("/ratings", { imdbId: id, qualification: myScore, comment: myComment });
      toast.success("Calificación guardada");

      const s = await api.get<SummaryDto>(`/ratings/movie/${id}/summary`).catch(() => null);
      if (s?.data) setSummary(s.data);

      const rs = await api.get<Rating[]>(`/ratings/movie/${id}`).catch(() => null);
      if (rs?.data) setRatings(rs.data);
    } catch (e: any) {
      toast.error(e?.response?.data?.error ?? "No se pudo guardar tu calificación");
    }
  }

  if (loading) {
    return (
      <div className="max-w-6xl mx-auto animate-pulse">
        <div className="grid grid-cols-1 md:grid-cols-[220px_1fr] gap-6">
          <div className="w-[220px] h-[320px] rounded bg-gray-200" />
          <div className="space-y-3">
            <div className="h-8 w-1/2 bg-gray-200 rounded" />
            <div className="h-5 w-1/3 bg-gray-200 rounded" />
            <div className="h-5 w-2/3 bg-gray-200 rounded" />
            <div className="h-5 w-1/4 bg-gray-200 rounded" />
          </div>
        </div>
      </div>
    );
  }

  if (!movie) return <div>No encontrada.</div>;

  const released = movie.released
    ? new Date(movie.released).toLocaleDateString("es-AR", {
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
    })
    : undefined;

  return (
    <div className="max-w-6xl mx-auto">
      <div className="grid grid-cols-1 md:grid-cols-[220px_1fr] gap-6">
        <div>
          {movie.poster ? (
            <img src={movie.poster} className="w-[220px] rounded" />
          ) : (
            <div className="w-[220px] h-[320px] bg-gray-200 rounded" />
          )}
        </div>

        <div>
          <h1 className="text-3xl font-semibold">{movie.title}</h1>

          <div className="mt-2 flex flex-wrap items-center gap-2">
            <Badge variant="secondary" className="uppercase">
              {movie.type}
            </Badge>
            {genres.map((g) => (
              <Badge key={g} variant="outline">{g}</Badge>
            ))}
          </div>

          <div className="grid md:grid-cols-2 gap-y-1 text-sm text-gray-700 mt-4">
            <div>
              <span className="text-gray-500">Lanzamiento: </span>
              {released ?? "-"}
            </div>
            <div>
              <span className="text-gray-500">Duración: </span>
              {movie.runtimeMinutes ? `${movie.runtimeMinutes} min` : "-"}
            </div>
            <div>
              <span className="text-gray-500">IMDB: </span>
              {movie.imdbRating ? <>⭐ {movie.imdbRating}</> : "-"}
            </div>
            <div>
              <span className="text-gray-500">País: </span>
              {movie.country ?? "-"}
            </div>
          </div>

          {(movie.director || movie.writer || movie.actors) && (
            <div className="mt-3 text-sm">
              {movie.director && (
                <div>
                  <span className="text-gray-500">Director: </span>
                  <span className="font-medium">{movie.director}</span>
                </div>
              )}
              {movie.writer && (
                <div className="mt-1">
                  <span className="text-gray-500">Guion: </span>
                  <span className="font-medium">{movie.writer}</span>
                </div>
              )}
              {movie.actors && (
                <div className="mt-1">
                  <span className="text-gray-500">Elenco: </span>
                  <span className="font-medium">{movie.actors}</span>
                </div>
              )}
            </div>
          )}

          <div className="mt-4">
            {user ? (
              inWatchlist ? (
                <Button variant="outline" onClick={removeFromWatchlist}>
                  Quitar de mi lista
                </Button>
              ) : (
                <Button onClick={addToWatchlist}>Agregar a mi lista</Button>
              )
            ) : (
              <Button
                variant="secondary"
                onClick={() => toast.info("Ingresá para usar tu lista")}
              >
                Ingresar para agregar
              </Button>
            )}
          </div>

          {/* Mi calificación */}
          <Card className="mt-6">
            <CardContent className="p-4 space-y-3">
              <div className="font-medium">Mi calificación</div>
              <div className="flex items-center gap-3">
                <StarPicker value={myScore} onChange={setMyScore} />
                <span className="text-sm text-gray-600">
                  {myScore || "-"}/5
                </span>
              </div>
              <div className="space-y-2">
                <label className="text-sm">Comentario (opcional)</label>
                <Textarea
                  placeholder="¿Qué te pareció?"
                  value={myComment}
                  onChange={(e) => setMyComment(e.target.value)}
                  rows={3}
                />
              </div>
              <div>
                <Button onClick={saveRating} disabled={!canRate}>
                  Guardar calificación
                </Button>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Reseñas de la comunidad */}
      <Card className="mt-6">
        <CardContent className="p-4 space-y-4">
          <div className="font-medium">
            Reseñas de la comunidad
            {summary && (
              <span className="ml-2 text-sm text-gray-600">
                ({summary.count} voto{summary.count === 1 ? "" : "s"}, promedio{" "}
                {summary.average.toFixed(1)})
              </span>
            )}
          </div>

          {reviews.length === 0 ? (
            <div className="text-sm text-gray-500">
              Aún no hay reseñas. ¡Sé el primero en calificar!
            </div>
          ) : (
            <div className="space-y-4">
              {reviews.map((r) => (
                <div key={r.id} className="flex gap-3">
                  <Avatar className="h-9 w-9">
                    <AvatarImage
                      src={fileUrl(r.avatarUrl) ?? undefined}
                      alt={r.username}
                    />
                    <AvatarFallback>
                      {(r.fullname || r.username).slice(0, 1).toUpperCase()}
                    </AvatarFallback>
                  </Avatar>

                  <div className="flex-1">
                    <div className="flex items-center justify-between">
                      <div className="text-sm font-medium">
                        {r.fullname || r.username}
                      </div>
                      <div className="text-xs text-gray-500">
                        {new Date(r.date).toLocaleDateString()}
                      </div>
                    </div>

                    <div className="mt-1 flex items-center gap-1">
                      {Array.from({ length: 5 }).map((_, i) => (
                        <Star
                          key={i}
                          className={`h-4 w-4 ${
                            i < r.qualification
                              ? "text-yellow-500 fill-yellow-500"
                              : "text-gray-300"
                          }`}
                        />
                      ))}
                      <span className="ml-2 text-xs text-gray-600">
                        {r.qualification}/5
                      </span>
                    </div>

                    {r.comment && (
                      <div className="mt-1 text-sm text-gray-700 whitespace-pre-wrap">
                        {r.comment}
                      </div>
                    )}

                    <Separator className="mt-3" />
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
