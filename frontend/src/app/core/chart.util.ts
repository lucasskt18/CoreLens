import { EChartsOption } from 'echarts';
import { SeriesPoint } from '../core/models';

const LINE = '#7a93b2';
const FILL = 'rgba(122, 147, 178, 0.12)';
const TOOLTIP_BG = '#16181d';
const TOOLTIP_TEXT = '#e6e8ee';
const AXIS = '#6b7380';
const GRID = 'rgba(255, 255, 255, 0.06)';

export type SparklineKind = 'percent' | 'series' | 'temp';

export function sparkline(points: SeriesPoint[], kind: SparklineKind = 'series'): EChartsOption {
  const yAxis: EChartsOption['yAxis'] =
    kind === 'percent'
      ? { type: 'value', min: 0, max: 100, show: false }
      : kind === 'temp'
        ? { type: 'value', min: 0, max: 100, show: false }
        : { type: 'value', min: 0, scale: true, show: false };

  return {
    animation: false,
    backgroundColor: 'transparent',
    grid: { left: 0, right: 0, top: 8, bottom: 0 },
    xAxis: { type: 'time', show: false },
    yAxis,
    tooltip: {
      trigger: 'axis',
      backgroundColor: TOOLTIP_BG,
      borderWidth: 0,
      padding: [6, 10],
      textStyle: { color: TOOLTIP_TEXT, fontSize: 12, fontFamily: 'IBM Plex Sans, sans-serif' },
      extraCssText: 'box-shadow: 0 8px 24px rgba(0,0,0,.35); border-radius: 6px;'
    },
    series: [
      {
        type: 'line',
        showSymbol: false,
        smooth: false,
        data: points.map(p => [p.time, p.value]),
        lineStyle: { width: 1.5, color: LINE },
        areaStyle: { color: FILL }
      }
    ]
  };
}

export function historyChart(points: SeriesPoint[], label: string): EChartsOption {
  return {
    animationDuration: 300,
    backgroundColor: 'transparent',
    tooltip: {
      trigger: 'axis',
      backgroundColor: TOOLTIP_BG,
      borderWidth: 0,
      textStyle: { color: TOOLTIP_TEXT, fontSize: 12 }
    },
    grid: { left: 44, right: 12, top: 16, bottom: 28 },
    xAxis: {
      type: 'time',
      axisLine: { lineStyle: { color: GRID } },
      axisLabel: { color: AXIS, fontSize: 11 },
      splitLine: { show: false }
    },
    yAxis: {
      type: 'value',
      axisLabel: { color: AXIS, fontSize: 11 },
      splitLine: { lineStyle: { color: GRID, type: 'dashed' } },
      axisLine: { show: false },
      axisTick: { show: false }
    },
    series: [
      {
        name: label,
        type: 'line',
        showSymbol: false,
        smooth: false,
        data: points.map(p => [p.time, p.value]),
        lineStyle: { width: 1.6, color: LINE },
        areaStyle: { color: FILL }
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
