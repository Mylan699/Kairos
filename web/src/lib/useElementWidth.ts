import { useEffect, useRef, useState, type RefObject } from 'react';

/** Measures an element, for charts that have to draw at an exact size. */
export function useElementWidth<T extends HTMLElement>(): [RefObject<T | null>, number] {
  const elementRef = useRef<T>(null);
  const [width, setWidth] = useState(0);

  useEffect(() => {
    const element = elementRef.current;
    if (!element) {
      return;
    }

    const observer = new ResizeObserver(([entry]) => {
      if (entry) {
        setWidth(Math.floor(entry.contentRect.width));
      }
    });

    observer.observe(element);
    return () => observer.disconnect();
  }, []);

  return [elementRef, width];
}
