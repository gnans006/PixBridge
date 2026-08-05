import { useEffect, useRef, useState } from 'react';

/**
 * Animates a numeric value toward `end` using a cubic ease-out curve.
 *
 * Key behaviour:
 * - Initialises at `end` so cached data renders immediately without a
 *   0-flash on every component remount (e.g. navigating back to the dashboard).
 * - Only triggers an animation when `end` actually changes value.
 * - Animates from the last rendered value to the new target (smooth on polls).
 */
export function useCountUp(end: number, duration = 1000): number {
  // Start AT the target so a remount with cached data shows the correct
  // number on the very first frame with no visible reset to 0.
  const [value, setValue] = useState(end);
  const rafRef = useRef<number>(0);
  // Track the previous target so we only animate on genuine changes.
  const prevEndRef = useRef<number>(end);

  useEffect(() => {
    if (end === prevEndRef.current) return; // nothing changed, skip

    const startVal = value; // capture at the moment the new target arrives
    prevEndRef.current = end;
    const startTime = performance.now();

    const animate = (now: number) => {
      const elapsed = now - startTime;
      const progress = Math.min(elapsed / duration, 1);
      const eased = 1 - Math.pow(1 - progress, 3);
      setValue(Math.round(startVal + (end - startVal) * eased));
      if (progress < 1) {
        rafRef.current = requestAnimationFrame(animate);
      }
    };

    rafRef.current = requestAnimationFrame(animate);
    return () => cancelAnimationFrame(rafRef.current);
    // `value` intentionally omitted from deps — the stale closure is correct
    // here so `startVal` is frozen to the moment the effect first runs.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [end, duration]);

  return value;
}
