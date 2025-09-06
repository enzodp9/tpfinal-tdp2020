import { useEffect, useMemo, useRef, useState } from "react";
import { useAuth } from "@/context/auth";
import { api } from "@/lib/axios";
import { fileUrl } from "@/lib/utils";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { toast } from "sonner";
import { Pencil, Plus, Star, User as UserIcon, Check, X } from "lucide-react";

/* Tipo que representa el resumen del usuario */
type Summary = { watchlistCount: number; ratingsCount: number };

export default function Profile() {
  const { user, refreshMe } = useAuth(); // contexto de auth

  const [summary, setSummary] = useState<Summary>({
    watchlistCount: 0,
    ratingsCount: 0,
  }); // resumen de usuario

  // nombre (precargado)
  const [fullname, setFullname] = useState(user?.fullname ?? "");
  const originalFullname = user?.fullname ?? "";

  // edición de campos
  const [editingName, setEditingName] = useState(false);

  // avatar
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [bust, setBust] = useState(0); // cache-busting para imagen

  const [saving, setSaving] = useState(false);

  useEffect(() => {
    // refrescar summary
    (async () => {
      try {
        const { data } = await api.get<Summary>("/users/me/summary");
        setSummary(data);
      } catch {
        toast.error("No se pudo cargar el resumen de usuario");
      }
    })();
  }, []);

  // si cambia user (post refresh), actualizo estados dependientes
  useEffect(() => {
    setFullname(user?.fullname ?? "");
    setEditingName(false);
    setSelectedFile(null);
    setPreviewUrl(null);
    setBust(Date.now());
  }, [user?.fullname, user?.avatarUrl]);

  // habilitar Guardar si cambió el nombre o hay archivo
  const canSave = useMemo(() => {
    const nameChanged = fullname.trim() !== originalFullname.trim();
    return nameChanged || !!selectedFile;
  }, [fullname, originalFullname, selectedFile]);


  /* Maneja la selección de un nuevo avatar */
  function onAvatarPick(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.currentTarget.files?.[0] ?? null;
    setSelectedFile(file);
    if (previewUrl) URL.revokeObjectURL(previewUrl);
    setPreviewUrl(file ? URL.createObjectURL(file) : null);
  }

  /* Guarda los cambios del perfil */
  async function onSave() {
    try {
      setSaving(true);

      // 1) si hay archivo, subir avatar
      if (selectedFile) {
        const fd = new FormData();
        fd.append("file", selectedFile);
        await api.post("/users/me/avatar", fd, {
          headers: { "Content-Type": "multipart/form-data" },
        });
      }

      // 2) si cambió el nombre, patch
      if (fullname.trim() !== originalFullname.trim()) {
        await api.patch("/users/me", { fullname });
      }

      // 3) refrescar contexto y UI
      await refreshMe();
      setBust(Date.now());
      toast.success("Perfil actualizado");
    } catch (e: any) {
      toast.error(e?.response?.data?.error ?? "No se pudo actualizar el perfil");
    } finally {
      setSaving(false);
      // limpiar selección local del archivo
      if (fileInputRef.current) fileInputRef.current.value = "";
      setSelectedFile(null);
      if (previewUrl) {
        URL.revokeObjectURL(previewUrl);
        setPreviewUrl(null);
      }
      setEditingName(false);
    }
  }

  /* Cancela la edición del nombre */
  function cancelNameEdit() {
    setFullname(originalFullname);
    setEditingName(false);
  }

  return (
    <div className="space-y-8">
      {/* Header de usuario */}
      <div className="flex items-start gap-6">
        <div className="relative">
          <Avatar className="h-20 w-20">
            <AvatarImage
              src={
                previewUrl ??
                (user?.avatarUrl
                  ? `${fileUrl(user.avatarUrl)}?v=${bust}`
                  : undefined)
              }
              alt={user?.username}
            />
            <AvatarFallback>
              <UserIcon className="h-6 w-6" />
            </AvatarFallback>
          </Avatar>

          {/* Botón lápiz sobre el avatar */}
          <button
            type="button"
            onClick={() => fileInputRef.current?.click()}
            className="absolute -bottom-2 -right-2 rounded-full bg-white shadow p-1 hover:bg-gray-50"
            title="Cambiar foto"
          >
            <Pencil className="h-4 w-4" />
          </button>

          {/* input file oculto */}
          <input
            ref={fileInputRef}
            id="avatar-file"
            type="file"
            accept="image/*"
            onChange={onAvatarPick}
            hidden
          />
        </div>

        <div className="flex-1 space-y-2">
          <div className="text-sm text-gray-600">Usuario</div>

          {/* username (solo lectura, con lápiz deshabilitado por ahora) */}
          <div className="flex items-center gap-2">
            <div className="font-medium">{user?.username}</div>
          </div>

          {/* nombre completo, editable con lápiz */}
          <div className="flex items-center gap-2">
            <Input
              className="w-72"
              placeholder="Tu nombre completo"
              value={fullname}
              onChange={(e) => setFullname(e.target.value)}
              disabled={!editingName}
            />
            {!editingName ? (
              <Button
                type="button"
                variant="outline"
                size="icon"
                onClick={() => setEditingName(true)}
                title="Editar nombre"
              >
                <Pencil className="h-4 w-4" />
              </Button>
            ) : (
              <div className="flex items-center gap-1">
                <Button
                  type="button"
                  size="icon"
                  variant="outline"
                  onClick={() => setEditingName(false)}
                  title="Aceptar edición (también podés usar Guardar)"
                >
                  <Check className="h-4 w-4" />
                </Button>
                <Button
                  type="button"
                  size="icon"
                  variant="ghost"
                  onClick={cancelNameEdit}
                  title="Cancelar cambios en nombre"
                >
                  <X className="h-4 w-4" />
                </Button>
              </div>
            )}

            <Button onClick={onSave} disabled={!canSave || saving}>
              {saving ? "Guardando..." : "Guardar"}
            </Button>
          </div>

          {/* Indicador de archivo seleccionado */}
          {selectedFile && (
            <div className="text-xs text-gray-500">{selectedFile.name}</div>
          )}
        </div>
      </div>

      {/* Dashboard en español */}
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-6">
        <Card>
          <CardHeader className="flex flex-row items-start justify-between">
            <div className="font-medium">Películas en mi lista</div>
            <Plus className="h-4 w-4 text-gray-500" />
          </CardHeader>
          <CardContent>
            <div className="text-4xl font-semibold">
              {summary.watchlistCount}
            </div>
            <div className="text-sm text-gray-500 mt-1">
              Películas que querés ver
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-start justify-between">
            <div className="font-medium">Películas calificadas</div>
            <Star className="h-4 w-4 text-gray-500" />
          </CardHeader>
          <CardContent>
            <div className="text-4xl font-semibold">{summary.ratingsCount}</div>
            <div className="text-sm text-gray-500 mt-1">
              Películas que valoraste
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
