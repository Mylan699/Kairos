import type { ComponentType, SVGProps } from 'react';
import { BlotterIcon, DashboardIcon, MarketsIcon, PositionsIcon, ReplayIcon, RiskIcon } from '@/design-system';

export interface NavigationItem {
  to: string;
  label: string;
  shortLabel: string;
  icon: ComponentType<SVGProps<SVGSVGElement>>;
}

export const NAVIGATION: readonly NavigationItem[] = [
  { to: '/app', label: 'Tableau de bord', shortLabel: 'Accueil', icon: DashboardIcon },
  { to: '/app/blotter', label: 'Blotter', shortLabel: 'Blotter', icon: BlotterIcon },
  { to: '/app/positions', label: 'Positions et P&L', shortLabel: 'Positions', icon: PositionsIcon },
  { to: '/app/markets', label: 'Marchés', shortLabel: 'Marchés', icon: MarketsIcon },
  { to: '/app/risk', label: 'Risques', shortLabel: 'Risques', icon: RiskIcon },
  { to: '/app/replay', label: 'Rejeu', shortLabel: 'Rejeu', icon: ReplayIcon },
];
