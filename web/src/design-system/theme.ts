import { useSyncExternalStore } from 'react';

export type Theme = 'dark' | 'light';

const STORAGE_KEY = 'kairos-theme';
const listeners = new Set<() => void>();

function readTheme(): Theme {
  return document.documentElement.dataset.theme === 'light' ? 'light' : 'dark';
}

function subscribe(listener: () => void) {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

export function setTheme(theme: Theme) {
  if (theme === 'light') {
    document.documentElement.dataset.theme = 'light';
  } else {
    delete document.documentElement.dataset.theme;
  }

  try {
    localStorage.setItem(STORAGE_KEY, theme);
  } catch {
    // The choice simply will not be remembered.
  }

  listeners.forEach((listener) => listener());
}

/** Reads the theme set on the document, which charts also need to follow. */
export function useTheme(): Theme {
  return useSyncExternalStore(subscribe, readTheme);
}
