import { render } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import darkMark from '../../../../brand/kairos-mark-dark.svg?raw';
import lightMark from '../../../../brand/kairos-mark-light.svg?raw';
import monoMark from '../../../../brand/kairos-mark-mono.svg?raw';
import { KairosLogo } from './KairosLogo';
import { KairosMark, MARK_GEOMETRY } from './KairosMark';

const part = (container: HTMLElement, name: string) => container.querySelector(`[data-part="${name}"]`);

describe('KairosMark', () => {
  it.each([
    ['dark', darkMark],
    ['light', lightMark],
    ['mono', monoMark],
  ])('uses the exact geometry of the %s brand file', (_name, svg) => {
    const { viewBox, stem, leg, curve, dot } = MARK_GEOMETRY;

    expect(svg).toContain(`viewBox="${viewBox}"`);
    expect(svg).toContain(`x="${stem.x}" y="${stem.y}" width="${stem.width}" height="${stem.height}" rx="${stem.rx}"`);
    expect(svg).toContain(`d="${leg.d}"`);
    expect(svg).toContain(`stroke-width="${leg.strokeWidth}"`);
    expect(svg).toContain(`d="${curve.d}"`);
    expect(svg).toContain(`stroke-width="${curve.strokeWidth}"`);
    expect(svg).toContain(`cx="${dot.cx}" cy="${dot.cy}" r="${dot.r}"`);
  });

  it('keeps the square proportions of the symbol at any size', () => {
    const { container } = render(<KairosMark size={64} />);

    expect(container.querySelector('svg')).toHaveAttribute('width', '64');
    expect(container.querySelector('svg')).toHaveAttribute('height', '64');
  });

  it('pulses the last tick while the feed is connected', () => {
    const { container } = render(<KairosMark animation="live" connected />);

    expect(part(container, 'pulse')).toBeInTheDocument();
    expect(part(container, 'dot')).toHaveAttribute('fill', 'var(--kairos-accent)');
  });

  it('turns the last tick grey and stops pulsing when disconnected', () => {
    const { container } = render(<KairosMark animation="live" connected={false} />);

    expect(part(container, 'pulse')).not.toBeInTheDocument();
    expect(part(container, 'dot')).toHaveAttribute('fill', 'var(--kairos-idle)');
  });

  it('does not pulse when it is not showing the live state', () => {
    const { container } = render(<KairosMark />);

    expect(part(container, 'pulse')).not.toBeInTheDocument();
  });

  it('can follow the text colour', () => {
    const { container } = render(<KairosMark mono />);

    expect(part(container, 'curve')).toHaveAttribute('stroke', 'currentColor');
  });
});

describe('KairosLogo', () => {
  it('sets the wordmark in text and the signature only on request', () => {
    const { rerender, queryByText, getByText } = render(<KairosLogo />);

    expect(getByText('Kairos')).toBeInTheDocument();
    expect(queryByText('Cross-asset trading desk')).not.toBeInTheDocument();

    rerender(<KairosLogo withSignature />);

    expect(getByText('Cross-asset trading desk')).toBeInTheDocument();
  });
});
