import { AllCommunityModule, ModuleRegistry, themeQuartz, type GridOptions } from 'ag-grid-community';
import { AgGridReact } from 'ag-grid-react';
import type { Ref } from 'react';

ModuleRegistry.registerModules([AllCommunityModule]);

/** The grid drawn entirely from the brand tokens, so that it follows the theme by itself. */
const kairosGridTheme = themeQuartz.withParams({
  accentColor: 'var(--kairos-accent)',
  backgroundColor: 'var(--kairos-surface-2)',
  foregroundColor: 'var(--kairos-ink)',
  textColor: 'var(--kairos-ink)',
  subtleTextColor: 'var(--kairos-ink-3)',
  borderColor: 'var(--kairos-line-subtle)',
  rowBorder: { color: 'var(--kairos-line-subtle)', width: 1 },
  headerBackgroundColor: 'var(--kairos-surface-2)',
  headerTextColor: 'var(--kairos-ink-3)',
  headerFontFamily: 'var(--font-mono)',
  headerFontSize: 11,
  headerFontWeight: 400,
  headerRowBorder: { color: 'var(--kairos-line)', width: 1 },
  headerColumnBorder: false,
  headerColumnResizeHandleColor: 'var(--kairos-line)',
  oddRowBackgroundColor: 'transparent',
  rowHoverColor: 'var(--kairos-surface-3)',
  selectedRowBackgroundColor: 'var(--kairos-accent-soft)',
  fontFamily: 'var(--font-sans)',
  fontSize: 12,
  rowHeight: 32,
  headerHeight: 32,
  cellHorizontalPadding: 12,
  columnBorder: false,
  wrapperBorder: false,
  wrapperBorderRadius: 0,
  browserColorScheme: 'inherit',
  iconColor: 'var(--kairos-ink-3)',
  focusShadow: 'none',
});

interface KairosGridProps<T> extends GridOptions<T> {
  gridRef?: Ref<AgGridReact<T>>;
  /** What the grid is about, for screen readers. */
  label: string;
}

export function KairosGrid<T>({ gridRef, label, ...options }: KairosGridProps<T>) {
  return (
    <div className="h-full w-full" role="region" aria-label={label}>
      <AgGridReact<T>
        ref={gridRef}
        theme={kairosGridTheme}
        animateRows
        suppressCellFocus={false}
        suppressDragLeaveHidesColumns
        {...options}
      />
    </div>
  );
}
