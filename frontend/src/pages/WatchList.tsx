import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { api } from "@/lib/axios";
import { useAuth } from "@/context/auth";
import {
  Card,
  CardContent,
  CardFooter,
  CardHeader,
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { toast } from "sonner";
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Skeleton } from "@/components/ui/skeleton";
import { ChevronUp, ChevronDown, Trash2 } from "lucide-react";

/* Tipo que representa un ítem en la watchlist */
type Item = {
  imdbId: string;
  title: string;
  poster?: string | null;
  position: number;
};

/**
 * Página de la watchlist del usuario.
 * Muestra las películas agregadas a la lista.
 * Permite reordenar y quitar películas.
 * Maneja estados de carga y vacíos.
 */
export default function Watchlist() {
  const { user } = useAuth(); // contexto de auth
  const [items, setItems] = useState<Item[]>([]); // ítems en la watchlist
  const [loading, setLoading] = useState(true); // estado de carga
  const [pendingRemove, setPendingRemove] = useState<string | null>(null); // ítem a quitar

  /* Carga la watchlist desde el servidor */
  async function load() {
    try {
      setLoading(true);
      const { data } = await api.get<Item[]>("/watchlist");
      setItems([...data].sort((a, b) => a.position - b.position));
    } catch (e: any) {
      toast.error(e?.response?.data?.error ?? "No se pudo cargar tu lista");
    } finally {
      setLoading(false);
    }
  }

  /* Carga la lista al montar o cambiar usuario */
  useEffect(() => {
    if (user) void load();
  }, [user]);

  /* Quita una película de la watchlist */
  async function remove(id: string) {
    try {
      setItems((prev) =>
        prev
          .filter((i) => i.imdbId !== id)
          .map((i, idx) => ({ ...i, position: idx + 1 }))
      );
      await api.delete(`/watchlist/${id}`);
      toast.success("Quitada de tu lista");
    } catch (e: any) {
      toast.error(e?.response?.data?.error ?? "No se pudo quitar");
      void load();
    } finally {
      setPendingRemove(null);
    }
  }

  /* Mueve una película hacia arriba o abajo en la watchlist */
  async function move(id: string, dir: "up" | "down") {
    const idx = items.findIndex((i) => i.imdbId === id);
    if (idx === -1) return;

    const targetIdx = dir === "up" ? idx - 1 : idx + 1;
    if (targetIdx < 0 || targetIdx >= items.length) return;

    const swapped = [...items];
    const [a, b] = [swapped[idx], swapped[targetIdx]];
    [swapped[idx], swapped[targetIdx]] = [
      { ...b, position: a.position },
      { ...a, position: b.position },
    ];
    setItems(swapped);

    try {
      await api.patch("/watchlist/reorder", {
        imdbId: a.imdbId,
        newPosition: targetIdx + 1,
      });
    } catch (e: any) {
      toast.error(e?.response?.data?.error ?? "No se pudo reordenar");
      void load(); // rollback
    }
  }

  if (!user) {
    return (
      <div className="max-w-sm">
        <p className="text-sm text-gray-600">
          Ingresá para ver tu lista.{" "}
          <Link to="/login" className="underline">
            Ir a login
          </Link>
        </p>
      </div>
    );
  }

  return (
    <TooltipProvider delayDuration={250}>
      <div>
        <div className="mb-6">
          <h1 className="text-2xl font-semibold">Mi lista</h1>
          <p className="text-sm text-gray-600">
            Administrá las películas que querés ver.
          </p>
        </div>

        {/* Skeletons mientras carga */}
        {loading ? (
          <div className="grid grid-cols-[repeat(auto-fill,minmax(220px,1fr))] gap-6">
            {Array.from({ length: 8 }).map((_, i) => (
              <Card key={i} className="overflow-hidden">
                <CardHeader className="p-0">
                  <Skeleton className="w-full h-52" />
                </CardHeader>
                <CardContent className="p-3 space-y-2">
                  <Skeleton className="h-4 w-3/4" />
                  <Skeleton className="h-3 w-1/3" />
                </CardContent>
                <CardFooter className="p-3 pt-0 flex items-center justify-between gap-2">
                  <Skeleton className="h-9 w-20" />
                  <Skeleton className="h-9 w-20" />
                </CardFooter>
              </Card>
            ))}
          </div>
        ) : items.length === 0 ? (
          // Estado vacío
          <div className="rounded border bg-white p-8 text-center">
            <div className="text-lg font-medium mb-1">Tu lista está vacía</div>
            <p className="text-sm text-gray-600">
              Buscá una película y agregala a tu Watchlist desde el detalle.
            </p>
            <div className="mt-4">
              <Link to="/">
                <Button>Buscar películas</Button>
              </Link>
            </div>
          </div>
        ) : (
          <div className="grid grid-cols-[repeat(auto-fill,minmax(220px,1fr))] gap-6">
            {items.map((it, i) => (
              <Card
                key={it.imdbId}
                className="overflow-hidden transition hover:shadow-lg"
              >
                <Link to={`/movies/${it.imdbId}`}>
                  <CardHeader className="relative p-0">
                    {it.poster ? (
                      <img
                        src={it.poster}
                        alt={it.title}
                        className="w-full aspect-[2/3] object-cover"
                        loading="lazy"
                      />
                    ) : (
                      <div className="w-full aspect-[2/3] bg-gray-200" />
                    )}

                    {/* Chip de posición */}
                    <div className="absolute left-2 top-2 rounded-full bg-white/90 backdrop-blur px-2 py-0.5 text-xs font-medium shadow">
                      #{it.position}
                    </div>
                  </CardHeader>
                  <CardContent className="p-3">
                    <div className="font-medium text-sm line-clamp-2">
                      {it.title}
                    </div>
                  </CardContent>
                </Link>

                <CardFooter className="p-3 pt-0 flex items-center justify-between gap-2">
                  <div className="flex gap-1">
                    <Tooltip>
                      <TooltipTrigger asChild>
                        <Button
                          variant="outline"
                          size="icon"
                          onClick={() => move(it.imdbId, "up")}
                          disabled={i === 0}
                        >
                          <ChevronUp className="h-4 w-4" />
                        </Button>
                      </TooltipTrigger>
                      <TooltipContent>Mover arriba</TooltipContent>
                    </Tooltip>

                    <Tooltip>
                      <TooltipTrigger asChild>
                        <Button
                          variant="outline"
                          size="icon"
                          onClick={() => move(it.imdbId, "down")}
                          disabled={i === items.length - 1}
                        >
                          <ChevronDown className="h-4 w-4" />
                        </Button>
                      </TooltipTrigger>
                      <TooltipContent>Mover abajo</TooltipContent>
                    </Tooltip>
                  </div>

                  <Tooltip>
                    <TooltipTrigger asChild>
                      <Button
                        variant="destructive"
                        size="sm"
                        onClick={() => setPendingRemove(it.imdbId)}
                      >
                        <Trash2 className="h-4 w-4 mr-1" />
                        Quitar
                      </Button>
                    </TooltipTrigger>
                    <TooltipContent>Quitar de mi lista</TooltipContent>
                  </Tooltip>
                </CardFooter>
              </Card>
            ))}
          </div>
        )}

        {/* Confirmación Quitar */}
        <AlertDialog
          open={!!pendingRemove}
          onOpenChange={(o) => !o && setPendingRemove(null)}
        >
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>Quitar película</AlertDialogTitle>
              <AlertDialogDescription>
                ¿Seguro que querés quitarla de tu Watchlist?
              </AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <AlertDialogCancel onClick={() => setPendingRemove(null)}>
                Cancelar
              </AlertDialogCancel>
              <AlertDialogAction
                onClick={() => pendingRemove && remove(pendingRemove)}
              >
                Quitar
              </AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      </div>
    </TooltipProvider>
  );
}
