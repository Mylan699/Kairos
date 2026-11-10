import {
  CandlestickSeries,
  ColorType,
  createChart,
  LineSeries,
  type IChartApi,
  type ISeriesApi,
  type UTCTimestamp,
} from 'lightweight-charts';
import { motion, useReducedMotion } from 'motion/react';
import { useEffect, useRef } from 'react';
import type { AssetClass, DailyBar, IntradayPrice } from '@/api/types';
import { duration, ease, useTheme } from '@/design-system';
import { formatPrice } from '@/lib/format';

const PRICE_DECIMALS: Record<AssetClass, number> = { Equity: 2, Fx: 5, Bond: 3 };

const token = (name: string) => getComputedStyle(document.documentElement).getPropertyValue(`--kairos-${name}`).trim();

export const minuteToTimestamp = (date: string, minute: number): UTCTimestamp =>
  (Date.parse(`${date}T00:00:00Z`) / 1000 + minute * 60) as UTCTimestamp;

function themedOptions() {
  return {
    layout: {
      background: { type: ColorType.Solid, color: 'transparent' },
      textColor: token('ink-3'),
      fontFamily: "'Geist Mono Variable', ui-monospace, monospace",
      fontSize: 11,
      attributionLogo: false,
    },
    grid: {
      vertLines: { visible: false },
      horzLines: { color: token('line-subtle') },
    },
    rightPriceScale: { borderColor: token('line-subtle') },
    // Fixing both edges makes the library keep the first and last labels inside the chart.
    timeScale: { borderColor: token('line-subtle'), timeVisible: true, secondsVisible: false, fixLeftEdge: true, fixRightEdge: true },
    crosshair: {
      vertLine: { color: token('line-strong'), labelBackgroundColor: token('surface-4') },
      horzLine: { color: token('line-strong'), labelBackgroundColor: token('surface-4') },
    },
  };
}

/** The series lives and dies with its chart: removing the chart is enough to release both. */
function newChart(container: HTMLDivElement, assetClass: AssetClass): IChartApi {
  return createChart(container, {
    autoSize: true,
    // The price axis writes numbers like the rest of the application: a comma, not a point.
    localization: { locale: 'fr-FR', priceFormatter: (price: number) => formatPrice(price, assetClass) },
    ...themedOptions(),
  });
}

/** The chart reveals itself from left to right when it appears, then stays put. */
function Reveal({ containerRef }: { containerRef: React.RefObject<HTMLDivElement | null> }) {
  const reducedMotion = useReducedMotion();

  return (
    <motion.div
      ref={containerRef}
      className="h-full w-full"
      initial={reducedMotion ? false : { clipPath: 'inset(0 100% 0 0)' }}
      animate={{ clipPath: 'inset(0 0% 0 0)' }}
      transition={{ duration: duration.draw, ease: ease.standard }}
    />
  );
}

interface IntradayChartProps {
  date: string;
  assetClass: AssetClass;
  prices: readonly IntradayPrice[];
  /** The latest live price and its minute. */
  last: IntradayPrice | null;
}

export function IntradayChart({ date, assetClass, prices, last }: IntradayChartProps) {
  const theme = useTheme();
  const containerRef = useRef<HTMLDivElement>(null);
  const chartRef = useRef<IChartApi | null>(null);
  const seriesRef = useRef<ISeriesApi<'Line'> | null>(null);
  const lastTimeRef = useRef<number>(0);

  useEffect(() => {
    const chart = newChart(containerRef.current!, assetClass);
    const series = chart.addSeries(LineSeries, {
      color: token('accent'),
      lineWidth: 2,
      priceLineVisible: false,
      priceFormat: { type: 'price', precision: PRICE_DECIMALS[assetClass], minMove: 10 ** -PRICE_DECIMALS[assetClass] },
    });

    chartRef.current = chart;
    seriesRef.current = series;

    return () => chart.remove();
  }, [assetClass]);

  useEffect(() => {
    chartRef.current?.applyOptions(themedOptions());
    seriesRef.current?.applyOptions({ color: token('accent') });
  }, [theme]);

  useEffect(() => {
    const data = prices.map((point) => ({ time: minuteToTimestamp(date, point.minute), value: point.price }));
    seriesRef.current?.setData(data);
    lastTimeRef.current = data[data.length - 1]?.time ?? 0;
    chartRef.current?.timeScale().fitContent();
  }, [date, prices, assetClass]);

  useEffect(() => {
    if (!last) {
      return;
    }

    const time = minuteToTimestamp(date, last.minute);

    // The chart library refuses a point older than the last one, as after a jump back in the replay.
    if (time >= lastTimeRef.current) {
      seriesRef.current?.update({ time, value: last.price });
      lastTimeRef.current = time;
    }
  }, [date, last]);

  return <Reveal containerRef={containerRef} />;
}

export function DailyChart({ assetClass, bars }: { assetClass: AssetClass; bars: readonly DailyBar[] }) {
  const theme = useTheme();
  const containerRef = useRef<HTMLDivElement>(null);
  const chartRef = useRef<IChartApi | null>(null);
  const seriesRef = useRef<ISeriesApi<'Candlestick'> | null>(null);

  useEffect(() => {
    const chart = newChart(containerRef.current!, assetClass);
    const series = chart.addSeries(CandlestickSeries, {
      priceLineVisible: false,
      borderVisible: false,
      priceFormat: { type: 'price', precision: PRICE_DECIMALS[assetClass], minMove: 10 ** -PRICE_DECIMALS[assetClass] },
    });

    chartRef.current = chart;
    seriesRef.current = series;

    return () => chart.remove();
  }, [assetClass]);

  useEffect(() => {
    chartRef.current?.applyOptions(themedOptions());
    seriesRef.current?.applyOptions({
      upColor: token('gain'),
      downColor: token('loss'),
      wickUpColor: token('gain'),
      wickDownColor: token('loss'),
    });
  }, [theme]);

  useEffect(() => {
    seriesRef.current?.setData(bars.map((bar) => ({ time: bar.date, open: bar.open, high: bar.high, low: bar.low, close: bar.close })));
    chartRef.current?.timeScale().fitContent();
  }, [bars, assetClass]);

  return <Reveal containerRef={containerRef} />;
}
