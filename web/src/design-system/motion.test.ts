import { describe, expect, it } from 'vitest';
import { applyMotionTokens, duration, ease } from './motion';

describe('motion tokens', () => {
  it('keeps micro-interactions between 150 and 250 ms', () => {
    for (const value of [duration.fast, duration.base, duration.slow]) {
      expect(value).toBeGreaterThanOrEqual(0.15);
      expect(value).toBeLessThanOrEqual(0.25);
    }
  });

  it('exposes the curves and durations to CSS', () => {
    const element = document.createElement('div');

    applyMotionTokens(element);

    expect(element.style.getPropertyValue('--motion-ease-standard')).toBe(`cubic-bezier(${ease.standard.join(', ')})`);
    expect(element.style.getPropertyValue('--motion-base')).toBe('200ms');
    expect(element.style.getPropertyValue('--motion-flash')).toBe('700ms');
  });
});
