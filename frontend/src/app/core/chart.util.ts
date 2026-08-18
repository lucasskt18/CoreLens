import * as echarts from 'echarts';
import { EChartsOption } from 'echarts';
import { SeriesPoint } from '../core/models';

export const METRIC_COLORS = {
  cpu: '#6eb5d8',
  ram: '#9b8fd4',
  disk: '#d4b07a',
  net: '#5ec4b6',
  temp: '#d4897a',
  gpu: '#7ec99a'
} as const;

export type MetricTone = keyof typeof METRIC_COLORS;

const TOOLTIP_BG = '#12151c';
const TOOLTIP_TEXT = '#e8eaed';
const AXIS = '#7a828c';
const GRID = 'rgba(255, 255, 255, 0.055)';
const DEFAULT_LINE = METRIC_COLORS.cpu;

export type SparklineKind = 'percent' | 'series' | 'temp';

function hexToRgba(hex: string, alpha: number): string {
  const raw = hex.replace('#', '');
  const normalized = raw.length === 3 ? raw.split('').map(c => c + c).join('') : raw;
  const n = parseInt(normalized, 16);
  const r = (n >> 16) & 255;
  const g = (n >> 8) & 255;
  const b = n & 255;
  return `rgba(${r}, ${g}, ${b}, ${alpha})`;
}

function areaFill(color: string): echarts.graphic.LinearGradient {
  return new echarts.graphic.LinearGradient(0, 0, 0, 1, [
    { offset: 0, color: hexToRgba(color, 0.38) },
    { offset: 0.55, color: hexToRgba(color, 0.12) },
    { offset: 1, color: hexToRgba(color, 0.01) }
  ]);
}

function prefersReducedMotion(): boolean {
  return typeof window !== 'undefined'
    && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
}

function animationBlock() {
  const reduced = prefersReducedMotion();
  return {
    animation: !reduced,
    animationDuration: reduced ? 0 : 780,
    animationDurationUpdate: reduced ? 0 : 420,
    animationEasing: 'cubicOut' as const,
    animationEasingUpdate: 'linear' as const,
    animationThreshold: 4000
  };
}

function tooltipBase(): EChartsOption['tooltip'] {
  return {
    trigger: 'axis',
    backgroundColor: TOOLTIP_BG,
    borderWidth: 1,
    borderColor: 'rgba(255, 255, 255, 0.08)',
    padding: [8, 12],
    textStyle: { color: TOOLTIP_TEXT, fontSize: 12, fontFamily: 'IBM Plex Sans, sans-serif' },
    extraCssText: 'box-shadow: 0 12px 32px rgba(0,0,0,.45); border-radius: 10px;',
    axisPointer: {
      type: 'line',
      lineStyle: { color: 'rgba(255, 255, 255, 0.18)', width: 1, type: 'dashed' }
    },
    valueFormatter: (value) => typeof value === 'number' ? value.toFixed(1) : String(value ?? '')
  };
}

function seriesData(points: SeriesPoint[], color: string) {
  const last = points.length - 1;
  return points.map((p, i) => ({
    value: [p.time, p.value],
    symbol: i === last ? 'circle' : 'none',
    symbolSize: i === last ? 7 : 0,
    itemStyle: {
      color,
      shadowBlur: i === last ? 14 : 0,
      shadowColor: hexToRgba(color, 0.85)
    }
  }));
}

export function sparkline(
  points: SeriesPoint[],
  kind: SparklineKind = 'series',
  color: string = DEFAULT_LINE
): EChartsOption {
  const yAxis: EChartsOption['yAxis'] =
    kind === 'percent' || kind === 'temp'
      ? { type: 'value', min: 0, max: 100, show: false }
      : { type: 'value', min: 0, scale: true, show: false };

  return {
    ...animationBlock(),
    backgroundColor: 'transparent',
    grid: { left: 2, right: 6, top: 12, bottom: 2 },
    xAxis: { type: 'time', show: false },
    yAxis,
    tooltip: tooltipBase(),
    series: [
      {
        id: 'spark',
        type: 'line',
        showSymbol: true,
        smooth: 0.28,
        smoothMonotone: 'x',
        sampling: 'lttb',
        connectNulls: true,
        data: seriesData(points, color),
        lineStyle: {
          width: 2.2,
          color,
          shadowColor: hexToRgba(color, 0.45),
          shadowBlur: 10
        },
        areaStyle: { color: areaFill(color) },
        emphasis: {
          scale: false,
          lineStyle: {
            width: 2.6,
            shadowBlur: 16,
            shadowColor: hexToRgba(color, 0.55)
          }
        }
      }
    ]
  };
}

export function historyChart(points: SeriesPoint[], label: string, color: string = DEFAULT_LINE): EChartsOption {
  return {
    ...animationBlock(),
    backgroundColor: 'transparent',
    tooltip: {
      ...tooltipBase(),
      extraCssText: 'box-shadow: 0 12px 32px rgba(0,0,0,.45); border-radius: 10px;'
    },
    grid: { left: 48, right: 16, top: 20, bottom: 32 },
    xAxis: {
      type: 'time',
      axisLine: { lineStyle: { color: GRID } },
      axisLabel: { color: AXIS, fontSize: 11, fontFamily: 'IBM Plex Sans, sans-serif' },
      splitLine: { show: false }
    },
    yAxis: {
      type: 'value',
      axisLabel: { color: AXIS, fontSize: 11, fontFamily: 'IBM Plex Sans, sans-serif' },
      splitLine: { lineStyle: { color: GRID, type: 'dashed' } },
      axisLine: { show: false },
      axisTick: { show: false }
    },
    series: [
      {
        id: 'history',
        name: label,
        type: 'line',
        showSymbol: false,
        smooth: 0.22,
        smoothMonotone: 'x',
        sampling: 'lttb',
        data: points.map(p => [p.time, p.value]),
        lineStyle: {
          width: 2.2,
          color,
          shadowColor: hexToRgba(color, 0.35),
          shadowBlur: 8
        },
        areaStyle: { color: areaFill(color) }
      }
    ]
  };
}

export function formatBytes(value?: number): string {
  if (value == null || Number.isNaN(value)) {
    return '—';
  }
  const units = ['B', 'KB', 'MB', 'GB', 'TB'];
  let size = value;
  let unit = 0;
  while (size >= 1024 && unit < units.length - 1) {
    size /= 1024;
    unit++;
  }
  return `${size.toFixed(size >= 10 || unit === 0 ? 0 : 1)} ${units[unit]}`;
}

export function formatPct(value?: number): string {
  return value == null ? '—' : `${value.toFixed(1)}%`;
}

export function formatTemp(value?: number): string {
  return value == null ? '—' : `${value.toFixed(0)}°C`;
}

export function formatRate(value?: number): string {
  return value == null ? '—' : `${formatBytes(value)}/s`;
}
