import { clsx, type ClassValue } from "clsx"
import { twMerge } from "tailwind-merge"

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}


/** Genera URL absoluta para un archivo relativo */
export function fileUrl(rel?: string | null) {
  if (!rel) return null;
  const api = (import.meta.env.VITE_API_URL ?? "http://localhost:5080/api")
    .replace(/\/+$/, "");
  const origin = api.replace(/\/api$/, "");
  return `${origin}/${rel.replace(/^\/+/, "")}`;
}

