import { EChartsOption } from 'echarts';
import { SeriesPoint } from '../core/models';

export function sparkline(points: SeriesPoint[], color: string): EChartsOption {
  return {
    animation: false,
    grid: { left: 8, right: 8, top: 8, bottom: 8 },
    xAxis: { type: 'time', show: false },
    yAxis: { type: 'value', show: false, scale: true },
    tooltip: {
      trigger: 'axis',
      backgroundColor: '#10182a',
      borderColor: 'rgba(148,163,184,0.2)',
      textStyle: { color: '#e8eefc' }
    },
    series: [
      {
        type: 'line',
        showSymbol: false,
        smooth: true,
        data: points.map(p => [p.time, p.value]),
        lineStyle: { width: 2, color },
        areaStyle: { color: `${color}33` }
      }
    ]
  };
}

export function historyChart(points: SeriesPoint[], label: string, color: string): EChartsOption {
  return {
    backgroundColor: 'transparent',
    tooltip: { trigger: 'axis' },
    grid: { left: 48, right: 16, top: 24, bottom: 32 },
    xAxis: { type: 'time', axisLabel: { color: '#8ea0c0' } },
    yAxis: { type: 'value', axisLabel: { color: '#8ea0c0' }, splitLine: { lineStyle: { color: 'rgba(148,163,184,0.12)' } } },
    series: [
      {
        name: label,
        type: 'line',
        showSymbol: false,
        smooth: true,
        data: points.map(p => [p.time, p.value]),
        lineStyle: { width: 2, color },
        areaStyle: { color: `${color}22` }
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
