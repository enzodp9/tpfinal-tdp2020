import { useMemo } from "react";

/** * Componente para seleccionar una calificación en estrellas.
 * @param value Calificación actual.
 * @param onChange Función llamada al cambiar la calificación.
 * @param max Número máximo de estrellas (por defecto 5).
 * @param size Tamaño de las estrellas en píxeles (por defecto 24).
 */
export default function StarPicker({
  value,
  onChange,
  max = 5,
  size = 24,
}: {
  value: number;
  onChange: (v: number) => void;
  max?: number;
  size?: number;
}) {
  const stars = useMemo(() => Array.from({ length: max }, (_, i) => i + 1), [max]); // Array [1, 2, ..., max]

  return (
    <div className="flex items-center gap-1">
      {stars.map((n) => (
        <button
          key={n}
          type="button"
          onClick={() => onChange(n)}
          className="p-0"
          aria-label={`Rate ${n}`}
          title={`${n} estrella${n > 1 ? "s" : ""}`}
        >
          <svg
            width={size}
            height={size}
            viewBox="0 0 24 24"
            fill={n <= value ? "currentColor" : "none"}
            stroke="currentColor"
            className={n <= value ? "text-yellow-500" : "text-gray-400"}
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth="1.5"
              d="M11.48 3.499a.562.562 0 011.04 0l2.08 4.216a.563.563 0 00.424.308l4.652.675c.497.072.696.684.336 1.034l-3.366 3.28a.563.563 0 00-.162.498l.794 4.624a.562.562 0 01-.815.592l-4.15-2.18a.562.562 0 00-.524 0l-4.15 2.18a.562.562 0 01-.815-.592l.794-4.624a.563.563 0 00-.162-.498L3.99 9.732a.563.563 0 01.336-1.034l4.652-.675a.563.563 0 00.424-.308l2.08-4.216z"
            />
          </svg>
        </button>
      ))}
    </div>
  );
}
