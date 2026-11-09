import { useEffect, useRef, useState } from 'react';

/** Follows a fast-changing value at most once per interval, always ending on its latest state. */
export function useThrottledValue<T>(value: T, milliseconds: number): T {
  const [throttled, setThrottled] = useState(value);
  const latest = useRef(value);
  const timer = useRef<number | null>(null);

  useEffect(() => {
    latest.current = value;

    if (timer.current === null) {
      timer.current = window.setTimeout(() => {
        timer.current = null;
        setThrottled(latest.current);
      }, milliseconds);
    }
  }, [value, milliseconds]);

  useEffect(
    () => () => {
      if (timer.current !== null) {
        window.clearTimeout(timer.current);
      }
    },
    [],
  );

  return throttled;
}
