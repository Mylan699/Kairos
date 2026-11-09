import { motion } from 'motion/react';
import { NavLink } from 'react-router';
import { cx, KairosLogo, KairosMark, spring } from '@/design-system';
import { useLive } from '@/realtime/live';
import { NAVIGATION } from './navigation';

/** Full width on large screens, icons only on tablets, hidden on phones in favour of the bottom bar. */
export function Sidebar() {
  const { status } = useLive();
  const connected = status === 'connected';

  return (
    <aside className="sticky top-0 hidden h-dvh w-14 shrink-0 flex-col border-r border-line-subtle bg-surface-1 md:flex xl:w-52">
      <div className="flex h-12 shrink-0 items-center justify-center border-b border-line-subtle xl:justify-start xl:px-3">
        <span className="xl:hidden">
          <KairosMark size={26} animation="live" connected={connected} />
        </span>
        <span className="hidden xl:inline-flex">
          <KairosLogo markSize={26} animation="live" connected={connected} withSignature wordmarkClassName="text-lg leading-5" />
        </span>
      </div>

      <nav aria-label="Navigation principale" className="flex flex-1 flex-col gap-0.5 p-1.5">
        {NAVIGATION.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            end={item.to === '/app'}
            title={item.label}
            className={({ isActive }) =>
              cx(
                'relative flex h-8 items-center justify-center gap-2.5 rounded-md text-sm transition-colors duration-[var(--motion-fast)] xl:justify-start xl:px-2.5',
                isActive ? 'text-ink' : 'text-ink-2 hover:text-ink',
              )
            }
          >
            {({ isActive }) => (
              <>
                {isActive && (
                  <motion.span layoutId="nav-marker" transition={spring.layout} className="absolute inset-0 rounded-md bg-surface-3" />
                )}
                <item.icon className="relative shrink-0" />
                <span className="relative hidden xl:inline">{item.label}</span>
              </>
            )}
          </NavLink>
        ))}
      </nav>

      <p className="hidden p-3 text-2xs leading-4 text-ink-3 xl:block">
        Projet de démonstration sur données historiques, aucune donnée réelle de client, pas de conseil en investissement.
      </p>
    </aside>
  );
}

export function BottomNavigation() {
  return (
    <nav aria-label="Navigation principale" className="fixed inset-x-0 bottom-0 z-30 grid h-14 grid-cols-6 border-t border-line-subtle bg-surface-1 md:hidden">
      {NAVIGATION.map((item) => (
        <NavLink
          key={item.to}
          to={item.to}
          end={item.to === '/app'}
          className={({ isActive }) =>
            cx('flex flex-col items-center justify-center gap-0.5 text-[10px]', isActive ? 'text-ink' : 'text-ink-2')
          }
        >
          <item.icon />
          {item.shortLabel}
        </NavLink>
      ))}
    </nav>
  );
}
