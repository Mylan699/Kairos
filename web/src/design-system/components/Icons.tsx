import type { ReactNode, SVGProps } from 'react';

/** A small set of icons drawn on a 16 px grid. */
function Icon({ children, ...props }: SVGProps<SVGSVGElement> & { children: ReactNode }) {
  return (
    <svg
      width={16}
      height={16}
      viewBox="0 0 16 16"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.5}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      style={{ flexShrink: 0 }}
      {...props}
    >
      {children}
    </svg>
  );
}

type IconProps = SVGProps<SVGSVGElement>;

export const CloseIcon = (props: IconProps) => <Icon {...props}><path d="M4 4l8 8M12 4l-8 8" /></Icon>;
export const ChevronDownIcon = (props: IconProps) => <Icon {...props}><path d="M4 6l4 4 4-4" /></Icon>;
export const CheckIcon = (props: IconProps) => <Icon {...props}><path d="M3 8.5l3.5 3.5L13 5" /></Icon>;
export const SearchIcon = (props: IconProps) => <Icon {...props}><circle cx="7" cy="7" r="4" /><path d="M10.5 10.5L13 13" /></Icon>;
export const PlayIcon = (props: IconProps) => <Icon {...props}><path d="M5 3.5v9l7-4.5z" fill="currentColor" /></Icon>;
export const PauseIcon = (props: IconProps) => <Icon {...props}><path d="M5.5 3.5v9M10.5 3.5v9" /></Icon>;
export const AlertIcon = (props: IconProps) => <Icon {...props}><path d="M8 2.5L14 13H2z" /><path d="M8 6.5v3M8 11.5v.01" /></Icon>;
export const InfoIcon = (props: IconProps) => <Icon {...props}><circle cx="8" cy="8" r="5.5" /><path d="M8 7.5v3M8 5.5v.01" /></Icon>;
export const SunIcon = (props: IconProps) => <Icon {...props}><circle cx="8" cy="8" r="2.75" /><path d="M8 1.5v1.5M8 13v1.5M1.5 8H3M13 8h1.5M3.4 3.4l1 1M11.6 11.6l1 1M12.6 3.4l-1 1M4.4 11.6l-1 1" /></Icon>;
export const MoonIcon = (props: IconProps) => <Icon {...props}><path d="M13 9.5A5.5 5.5 0 016.5 3a5.5 5.5 0 106.5 6.5z" /></Icon>;
export const DownloadIcon = (props: IconProps) => <Icon {...props}><path d="M8 2.5v8M4.5 7L8 10.5 11.5 7M3 13.5h10" /></Icon>;
export const DashboardIcon = (props: IconProps) => <Icon {...props}><path d="M2.5 2.5h4.5v6h-4.5zM9 2.5h4.5v3.5H9zM9 8h4.5v5.5H9zM2.5 10.5h4.5v3h-4.5z" /></Icon>;
export const BlotterIcon = (props: IconProps) => <Icon {...props}><path d="M2.5 4h11M2.5 8h11M2.5 12h11" /></Icon>;
export const PositionsIcon = (props: IconProps) => <Icon {...props}><path d="M3 13.5V8M8 13.5v-11M13 13.5V5.5" /></Icon>;
export const MarketsIcon = (props: IconProps) => <Icon {...props}><path d="M2 11l3.5-3.5 3 2L14 3.5" /><circle cx="14" cy="3.5" r=".5" /></Icon>;
export const RiskIcon = (props: IconProps) => <Icon {...props}><path d="M8 2l5 2v4c0 3-2.2 5-5 6-2.8-1-5-3-5-6V4z" /></Icon>;
export const ReplayIcon = (props: IconProps) => <Icon {...props}><path d="M3 8a5 5 0 105-5H5" /><path d="M6.5 1L4.5 3l2 2" /></Icon>;
export const KeyboardIcon = (props: IconProps) => <Icon {...props}><rect x="1.5" y="4" width="13" height="8" rx="1" /><path d="M4 7h.01M6.5 7h.01M9 7h.01M11.5 7h.01M5 9.5h6" /></Icon>;
