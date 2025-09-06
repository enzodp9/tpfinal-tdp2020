import { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { useAuth } from "@/context/auth";
import { z } from "zod";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  CardFooter,
} from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { toast } from "sonner";
import { Eye, EyeOff, Popcorn, UserPlus } from "lucide-react";


// Esquema de validación con Zod
const schema = z.object({
  username: z.string().min(3, "Mínimo 3 caracteres"),
  fullname: z.string().min(3, "Mínimo 3 caracteres"),
  password: z.string().min(4, "Mínimo 4 caracteres"),
});
type FormData = z.infer<typeof schema>;


/**
 * Página de registro.
 * Permite a los usuarios crear una nueva cuenta.
 * Utiliza validación de formularios con Zod y react-hook-form.
 * Muestra mensajes de error y éxito con sonner.
 */
export default function Register() {
  const nav = useNavigate(); // para redirigir
  const { register: signup } = useAuth(); // contexto de autenticación
  const [show, setShow] = useState(false); // mostrar/ocultar password

  // configuración de react-hook-form con zod
  const form = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { username: "", fullname: "", password: "" },
  }); 

  /* Maneja el envío del formulario */
  async function onSubmit(values: FormData) {
    const ok = await signup(values.username, values.fullname, values.password);
    if (ok) {
      toast.success("Cuenta creada");
      nav("/");
    } else {
      toast.error("No se pudo registrar");
    }
  }

  return (
    <div className="min-h-[calc(100vh-64px)] grid place-items-center bg-[radial-gradient(ellipse_at_top,_var(--tw-gradient-stops))] from-gray-50 via-white to-gray-50">
      <Card className="w-full max-w-md shadow-lg">
        <CardHeader className="space-y-2">
          <div className="flex justify-center">
            <div className="rounded-full bg-primary/10 p-3">
              <Popcorn className="h-10 w-10 text-primary" />
            </div>
          </div>
          <CardTitle className="text-2xl">Registrarse</CardTitle>
          <CardDescription>Crea una nueva cuenta</CardDescription>
        </CardHeader>
        <CardContent>
          <Form {...form}>
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
              {/* Usuario */}
              <FormField
                control={form.control}
                name="username"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Usuario</FormLabel>
                    <FormControl>
                      <Input placeholder="tu_usuario" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              {/* Nombre completo */}
              <FormField
                control={form.control}
                name="fullname"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Nombre completo</FormLabel>
                    <FormControl>
                      <Input placeholder="Tu nombre" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              {/* Password */}
              <FormField
                control={form.control}
                name="password"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Contraseña</FormLabel>
                    <div className="relative">
                      <FormControl>
                        <Input
                          type={show ? "text" : "password"}
                          placeholder="••••••••"
                          {...field}
                        />
                      </FormControl>
                      <button
                        type="button"
                        onClick={() => setShow((v) => !v)}
                        className="absolute inset-y-0 right-2 grid place-items-center px-2 text-gray-500 hover:text-gray-700"
                        aria-label={
                          show ? "Ocultar contraseña" : "Mostrar contraseña"
                        }
                      >
                        {show ? (
                          <EyeOff className="h-4 w-4" />
                        ) : (
                          <Eye className="h-4 w-4" />
                        )}
                      </button>
                    </div>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <Button
                className="w-full"
                type="submit"
                disabled={form.formState.isSubmitting}
              >
                {form.formState.isSubmitting ? (
                  "Creando..."
                ) : (
                  <span className="inline-flex items-center gap-2">
                    <UserPlus className="h-4 w-4" /> Crear cuenta
                  </span>
                )}
              </Button>
            </form>
          </Form>
          <Separator className="my-4" />
          <p className="text-sm text-center text-gray-600">
            ¿Ya tenés cuenta?{" "}
            <Link to="/login" className="underline underline-offset-4">
              Ingresar
            </Link>
          </p>
        </CardContent>
        <CardFooter className="text-xs text-gray-500 justify-center">
          © {new Date().getFullYear()} Taller de Programación
        </CardFooter>
      </Card>
    </div>
  );
}
