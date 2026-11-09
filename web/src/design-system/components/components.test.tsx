import { act, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { RollingNumber, FlashValue } from './LiveFigures';
import { Drawer } from './Overlays';
import { SegmentedControl } from './SegmentedControl';
import { Select } from './Select';
import { buildPoints, Sparkline } from './Sparkline';
import { ToastProvider, useToast } from './Toast';
import { TextField } from './Field';

const options = [
  { value: 'a', label: 'Alpha' },
  { value: 'b', label: 'Beta' },
  { value: 'c', label: 'Gamma' },
] as const;

describe('Select', () => {
  it('opens on click and reports the chosen value', async () => {
    const onChange = vi.fn();
    render(<Select label="Book" value="a" options={options} onChange={onChange} />);

    await userEvent.click(screen.getByRole('combobox'));
    await userEvent.click(screen.getByRole('option', { name: 'Beta' }));

    expect(onChange).toHaveBeenCalledWith('b');
  });

  it('can be used with the keyboard alone', async () => {
    const onChange = vi.fn();
    render(<Select label="Book" value="a" options={options} onChange={onChange} />);

    screen.getByRole('combobox').focus();
    await userEvent.keyboard('{Enter}{ArrowDown}{ArrowDown}{Enter}');

    expect(onChange).toHaveBeenCalledWith('c');
  });

  it('shows the placeholder when nothing is selected', () => {
    render(<Select label="Book" value={null} options={options} onChange={vi.fn()} placeholder="Choisir" />);

    expect(screen.getByRole('combobox')).toHaveTextContent('Choisir');
  });

  it('closes on Escape without choosing', async () => {
    const onChange = vi.fn();
    render(<Select label="Book" value="a" options={options} onChange={onChange} />);

    await userEvent.click(screen.getByRole('combobox'));
    await userEvent.keyboard('{Escape}');

    expect(screen.getByRole('combobox')).toHaveAttribute('aria-expanded', 'false');
    expect(onChange).not.toHaveBeenCalled();
  });
});

describe('TextField', () => {
  it('links its label to the input and announces an error', () => {
    render(<TextField label="Quantité" error="Valeur requise" />);

    expect(screen.getByLabelText('Quantité')).toHaveAttribute('aria-invalid', 'true');
    expect(screen.getByRole('alert')).toHaveTextContent('Valeur requise');
  });
});

describe('SegmentedControl', () => {
  it('marks the active segment and reports changes', async () => {
    const onChange = vi.fn();
    render(<SegmentedControl label="Vue" value="a" segments={options} onChange={onChange} />);

    expect(screen.getByRole('radio', { name: 'Alpha' })).toBeChecked();

    await userEvent.click(screen.getByRole('radio', { name: 'Gamma' }));

    expect(onChange).toHaveBeenCalledWith('c');
  });
});

describe('Drawer', () => {
  it('closes on Escape and moves focus inside when opened', async () => {
    const onClose = vi.fn();
    render(<Drawer open onClose={onClose} title="Nouveau trade"><input aria-label="Champ" /></Drawer>);

    expect(screen.getByRole('dialog', { name: 'Nouveau trade' })).toContainElement(document.activeElement as HTMLElement);

    await userEvent.keyboard('{Escape}');

    expect(onClose).toHaveBeenCalledOnce();
  });

  it('renders nothing while closed', () => {
    render(<Drawer open={false} onClose={vi.fn()} title="Nouveau trade">Contenu</Drawer>);

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });
});

describe('Toast', () => {
  function Trigger() {
    const { notify } = useToast();
    return <button onClick={() => notify({ tone: 'error', title: 'Refusé' })}>Notifier</button>;
  }

  it('announces an error as an alert', async () => {
    render(<ToastProvider><Trigger /></ToastProvider>);

    await userEvent.click(screen.getByRole('button', { name: 'Notifier' }));

    expect(screen.getByRole('alert')).toHaveTextContent('Refusé');
  });

  it('needs its provider', () => {
    const silence = vi.spyOn(console, 'error').mockImplementation(() => undefined);

    expect(() => render(<Trigger />)).toThrow('ToastProvider');

    silence.mockRestore();
  });
});

describe('RollingNumber', () => {
  it('exposes the whole figure to screen readers', () => {
    render(<RollingNumber text="+1 234,50 €" />);

    expect(screen.getByRole('text', { name: '+1 234,50 €' })).toBeInTheDocument();
  });
});

describe('FlashValue', () => {
  it('flashes only when the value changes', () => {
    const animate = vi.fn();
    HTMLElement.prototype.animate = animate;
    const { rerender } = render(<FlashValue value={10}>10</FlashValue>);

    expect(animate).not.toHaveBeenCalled();

    act(() => rerender(<FlashValue value={11}>11</FlashValue>));

    expect(animate).toHaveBeenCalledOnce();
    expect(animate.mock.calls[0]?.[0][0]).toEqual({ backgroundColor: 'var(--kairos-gain-soft)' });

    act(() => rerender(<FlashValue value={9}>9</FlashValue>));

    expect(animate.mock.calls[1]?.[0][0]).toEqual({ backgroundColor: 'var(--kairos-loss-soft)' });
  });
});

describe('Sparkline', () => {
  it('maps the highest value to the top and the lowest to the bottom', () => {
    const points = buildPoints([0, 10], 100, 40);

    expect(points[0]).toEqual([4, 36]);
    expect(points[1]).toEqual([96, 4]);
  });

  it('keeps a flat series inside the chart', () => {
    const points = buildPoints([5, 5, 5], 100, 40);

    expect(points.every(([, y]) => y === 36)).toBe(true);
  });

  it('describes the trend to screen readers', () => {
    render(<Sparkline values={[1, 2, 3]} label="P&L sur 30 jours" />);

    expect(screen.getByRole('img', { name: 'P&L sur 30 jours' })).toBeInTheDocument();
  });

  it('renders an empty space when there is no data', () => {
    render(<Sparkline values={[]} label="Vide" />);

    expect(screen.queryByRole('img')).not.toBeInTheDocument();
  });
});
