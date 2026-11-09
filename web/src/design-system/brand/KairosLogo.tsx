import { KairosMark, type MarkAnimation } from './KairosMark';

interface KairosLogoProps {
  markSize?: number;
  animation?: MarkAnimation;
  connected?: boolean;
  /** Shows "CROSS-ASSET TRADING DESK" under the wordmark. */
  withSignature?: boolean;
  wordmarkClassName?: string;
}

/** The symbol and the wordmark, set in text as the identity requires. */
export function KairosLogo({
  markSize = 24,
  animation = 'still',
  connected = true,
  withSignature = false,
  wordmarkClassName = 'text-lg',
}: KairosLogoProps) {
  return (
    <span className="inline-flex items-center gap-2">
      <KairosMark size={markSize} animation={animation} connected={connected} title="" />
      <span className="flex flex-col">
        <span className={`font-sans font-semibold tracking-[-0.04em] text-ink ${wordmarkClassName}`}>Kairos</span>
        {withSignature && (
          <span className="font-mono text-[9px] uppercase leading-3 tracking-[0.32em] text-ink-2">
            Cross-asset trading desk
          </span>
        )}
      </span>
    </span>
  );
}
