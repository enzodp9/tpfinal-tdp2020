import { api } from "@/lib/axios";

/** Tipo que representa una fila de usuario */
export type UserRow = {
  id: string;
  username: string;
  fullname: string;
  role: "administrator" | "user";
  avatarUrl?: string | null;
};

/** Tipo para crear un usuario */
export type CreateUserReq = {
  username: string;
  fullname: string;
  password: string;
  isAdmin: boolean;
};

/** API para gestionar usuarios */
export const UsersApi = {
  async list(q?: string): Promise<UserRow[]> {
    const { data } = await api.get("/users", { params: q ? { q } : undefined });
    return Array.isArray(data) ? data : [];
  },
  async create(req: CreateUserReq): Promise<UserRow> {
    const { data } = await api.post("/users", {
      username: req.username,
      fullname: req.fullname,
      password: req.password,
      role: req.isAdmin ? "administrator" : "user",
    });
    return data;
  },
  async remove(id: string): Promise<void> {
    await api.delete(`/users/${id}`);
  },
};
