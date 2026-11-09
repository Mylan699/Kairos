import { createContext, use, useMemo, useState, type ReactNode } from 'react';

interface BookFilterValue {
  /** The selected book, or null for the whole desk. */
  bookId: string | null;
  setBookId: (bookId: string | null) => void;
}

const BookFilterContext = createContext<BookFilterValue | null>(null);

export function BookFilterProvider({ children }: { children: ReactNode }) {
  const [bookId, setBookId] = useState<string | null>(null);
  const value = useMemo(() => ({ bookId, setBookId }), [bookId]);

  return <BookFilterContext value={value}>{children}</BookFilterContext>;
}

export function useBookFilter(): BookFilterValue {
  const value = use(BookFilterContext);
  if (!value) {
    throw new Error('useBookFilter must be used within a BookFilterProvider.');
  }
  return value;
}
