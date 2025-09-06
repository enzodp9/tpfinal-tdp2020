import { useEffect, useMemo, useRef, useState } from "react";
import { UsersApi } from "@/lib/users";
import type { UserRow, CreateUserReq } from "@/lib/users";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  DialogTrigger,
} from "@/components/ui/dialog";
import {
  AlertDialog,
  AlertDialogContent,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogCancel,
  AlertDialogAction,
} from "@/components/ui/alert-dialog";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { toast } from "sonner";
import { fileUrl } from "@/lib/utils";

/**
 * Página de administración de usuarios.
 * Permite listar, buscar y filtrar usuarios del sistema.
 * Habilita acciones de administración como crear, modificar o eliminar.
 */
export default function AdminUsers() {
  const [rows, setRows] = useState<UserRow[]>([]); // listado de usuarios
  const [loading, setLoading] = useState(true); // carga del listado


  // Búsqueda / filtro
  const [q, setQ] = useState("");
  const debounceRef = useRef<number | null>(null);

  // Creación
  const [openCreate, setOpenCreate] = useState(false);
  const [cUsername, setCUsername] = useState("");
  const [cFullname, setCFullname] = useState("");
  const [cPassword, setCPassword] = useState("");
  const [cRole, setCRole] = useState<"user" | "administrator">("user");
  const canCreate = useMemo(
    () => cUsername.trim() && cFullname.trim() && cPassword.length >= 6,
    [cUsername, cFullname, cPassword]
  );

  // Eliminación
  const [delId, setDelId] = useState<string | null>(null);

  // Carga inicial
  useEffect(() => {
    void load();
  }, []);

  // carga (con filtro opcional)
  async function load(qParam?: string) {
    try {
      setLoading(true);
      const data = await UsersApi.list(qParam);
      setRows(Array.isArray(data) ? data : []);
    } catch (e: any) {
      toast.error(e?.response?.data?.error ?? "No se pudo cargar el listado");
      setRows([]);
    } finally {
      setLoading(false);
    }
  }

  // debounce del filtro
  useEffect(() => {
    if (debounceRef.current) window.clearTimeout(debounceRef.current);
    debounceRef.current = window.setTimeout(() => {
      void load(q.trim() || undefined);
    }, 350);

    return () => {
      if (debounceRef.current) window.clearTimeout(debounceRef.current);
    };
  }, [q]);

  /* Crear usuario */
  async function onCreate() {
    if (!canCreate) return;
    const req: CreateUserReq = {
      username: cUsername.trim(),
      fullname: cFullname.trim(),
      password: cPassword,
      isAdmin: cRole === "administrator",
    };
    try {
      await UsersApi.create(req);
      toast.success("Usuario creado");
      // refrescamos listado filtrado actual
      await load(q.trim() || undefined);
      // limpiar form
      setCUsername("");
      setCFullname("");
      setCPassword("");
      setCRole("user");
      setOpenCreate(false);
    } catch (e: any) {
      toast.error(e?.response?.data?.error ?? "No se pudo crear el usuario");
    }
  }

  /* Eliminar usuario */
  async function onDelete(id: string) {
    try {
      await UsersApi.remove(id);
      toast.success("Usuario eliminado");
      await load(q.trim() || undefined);
    } catch (e: any) {
      toast.error(e?.response?.data?.error ?? "No se pudo eliminar");
    } finally {
      setDelId(null);
    }
  }

  return (
    <div className="space-y-6">
      {/* Header y acciones */}
      <div className="flex items-center justify-between flex-wrap gap-3">
        <div>
          <h1 className="text-2xl font-semibold">Administración de usuarios</h1>
          <p className="text-sm text-gray-600">
            Crear, listar y eliminar usuarios (solo administrador).
          </p>
        </div>

        <Dialog open={openCreate} onOpenChange={setOpenCreate}>
          <DialogTrigger asChild>
            <Button>Nuevo usuario</Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Nuevo usuario</DialogTitle>
            </DialogHeader>

            <div className="space-y-3">
              <div className="space-y-1">
                <div className="text-sm">Usuario</div>
                <Input
                  placeholder="usuario"
                  value={cUsername}
                  onChange={(e) => setCUsername(e.target.value)}
                />
              </div>
              <div className="space-y-1">
                <div className="text-sm">Nombre completo</div>
                <Input
                  placeholder="Nombre Apellido"
                  value={cFullname}
                  onChange={(e) => setCFullname(e.target.value)}
                />
              </div>
              <div className="space-y-1">
                <div className="text-sm">Contraseña</div>
                <Input
                  type="password"
                  placeholder="Mínimo 6 caracteres"
                  value={cPassword}
                  onChange={(e) => setCPassword(e.target.value)}
                />
              </div>
              <div className="space-y-1">
                <div className="text-sm">Rol</div>
                <Select
                  value={cRole}
                  onValueChange={(v) => setCRole(v as "user" | "administrator")}
                >
                  <SelectTrigger>
                    <SelectValue placeholder="Selecciona un rol" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="user">Usuario</SelectItem>
                    <SelectItem value="administrator">Administrador</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>

            <DialogFooter>
              <Button variant="outline" onClick={() => setOpenCreate(false)}>
                Cancelar
              </Button>
              <Button onClick={onCreate} disabled={!canCreate}>
                Crear
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      </div>

      {/* Filtro */}
      <div className="flex items-center gap-3">
        <Input
          className="w-72"
          placeholder="Buscar por usuario o nombre…"
          value={q}
          onChange={(e) => setQ(e.target.value)}
        />
        <div className="text-xs text-gray-500">
          {rows.length} resultado{rows.length === 1 ? "" : "s"}
        </div>
      </div>

      {/* Tabla */}
      <div className="overflow-auto rounded border bg-white">
        <table className="w-full text-sm">
          <thead className="bg-gray-50">
            <tr>
              <th className="text-left p-3">Usuario</th>
              <th className="text-left p-3">Nombre completo</th>
              <th className="text-left p-3">Rol</th>
              <th className="text-right p-3">Acciones</th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={4} className="p-6 text-center text-gray-500">
                  Cargando…
                </td>
              </tr>
            ) : rows.length === 0 ? (
              <tr>
                <td colSpan={4} className="p-6 text-center text-gray-500">
                  No hay usuarios que coincidan.
                </td>
              </tr>
            ) : (
              rows.map((u) => (
                <tr key={u.id} className="border-t">
                  <td className="p-3">
                    <div className="flex items-center gap-2">
                      {u.avatarUrl ? (
                        <img
                          src={`${fileUrl(u.avatarUrl)}?v=${encodeURIComponent(
                            u.avatarUrl
                          )}`}
                          alt={u.username}
                          className="h-7 w-7 rounded-full object-cover bg-gray-200"
                          onError={(e) => {
                            (e.currentTarget as HTMLImageElement).style.display =
                              "none";
                          }}
                        />
                      ) : (
                        <div className="h-7 w-7 rounded-full bg-gray-200" />
                      )}
                      <div className="font-medium">{u.username}</div>
                    </div>
                  </td>
                  <td className="p-3">{u.fullname}</td>
                  <td className="p-3">
                    <Badge
                      variant={u.role === "administrator" ? "default" : "secondary"}
                    >
                      {u.role === "administrator" ? "Admin" : "Usuario"}
                    </Badge>
                  </td>
                  <td className="p-3 text-right">
                    <Button
                      variant="destructive"
                      size="sm"
                      onClick={() => setDelId(u.id)}
                    >
                      Eliminar
                    </Button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* Confirmación eliminar */}
      <AlertDialog open={!!delId} onOpenChange={(o) => !o && setDelId(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Eliminar usuario</AlertDialogTitle>
            <AlertDialogDescription>
              Esta acción no se puede deshacer. ¿Deseás eliminar el usuario?
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel onClick={() => setDelId(null)}>
              Cancelar
            </AlertDialogCancel>
            <AlertDialogAction
              onClick={() => delId && onDelete(delId)}
            >
              Eliminar
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
