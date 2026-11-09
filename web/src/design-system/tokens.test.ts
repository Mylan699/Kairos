import { describe, expect, it } from 'vitest';
import css from './tokens.css?raw';

const [darkBlock = '', lightBlock = ''] = css.split(":root[data-theme='light']");

function token(block: string, name: string): string {
  const match = new RegExp(`--kairos-${name}:\\s*(#[0-9a-f]{6})`).exec(block);
  if (!match?.[1]) {
    throw new Error(`Token ${name} not found`);
  }
  return match[1];
}

function luminance(hex: string): number {
  const channels = [1, 3, 5].map((start) => parseInt(hex.slice(start, start + 2), 16) / 255);
  const [red = 0, green = 0, blue = 0] = channels.map((value) =>
    value <= 0.03928 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4,
  );
  return 0.2126 * red + 0.7152 * green + 0.0722 * blue;
}

function contrast(first: string, second: string): number {
  const [light, dark] = [luminance(first), luminance(second)].sort((a, b) => b - a) as [number, number];
  return (light + 0.05) / (dark + 0.05);
}

const WCAG_AA_TEXT = 4.5;
const surfaces = ['canvas', 'surface-1', 'surface-2', 'surface-3'];

describe.each([
  ['dark', darkBlock],
  ['light', lightBlock],
])('%s theme', (_name, block) => {
  it.each(['ink', 'ink-2', 'gain', 'loss', 'caution'])('keeps %s readable on every surface', (name) => {
    for (const surface of surfaces) {
      expect(contrast(token(block, name), token(block, surface))).toBeGreaterThanOrEqual(WCAG_AA_TEXT);
    }
  });

  it('keeps the quietest text readable on the main surfaces', () => {
    for (const surface of ['canvas', 'surface-1', 'surface-2']) {
      expect(contrast(token(block, 'ink-3'), token(block, surface))).toBeGreaterThanOrEqual(WCAG_AA_TEXT);
    }
  });

  it('keeps button labels readable on the accent', () => {
    expect(contrast(token(block, 'on-accent'), token(block, 'accent'))).toBeGreaterThanOrEqual(WCAG_AA_TEXT);
  });
});

describe('brand values', () => {
  it('uses the exact colours of the identity', () => {
    expect(token(darkBlock, 'canvas')).toBe('#0b0e14');
    expect(token(darkBlock, 'surface-2')).toBe('#151a24');
    expect(token(darkBlock, 'ink')).toBe('#e8ecf4');
    expect(token(darkBlock, 'ink-2')).toBe('#8a93a6');
    expect(token(darkBlock, 'accent')).toBe('#4f7bff');
    expect(token(lightBlock, 'accent')).toBe('#3461f0');
  });
});
