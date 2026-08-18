import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { NgxEchartsDirective } from 'ngx-echarts';
import { EChartsOption } from 'echarts';
import { METRIC_COLORS } from '../core/chart.util';

@Component({
  selector: 'app-metric-panel',
  standalone: true,
  imports: [NgxEchartsDirective],
  template: `
    <section class="panel" [style.--metric]="accent">
      <header>
        <div>
          <p class="kicker">{{ kicker }}</p>
          <h2>{{ title }}</h2>
        </div>
        <div class="value">{{ value }}</div>
      </header>

      @if (progress != null) {
        <div class="track" aria-hidden="true">
          <div class="fill" [style.width.%]="clampedProgress"></div>
        </div>
      }

      <div class="chart-wrap">
        @if (hasSeries && initOptions) {
          <div echarts [options]="initOptions" [merge]="options" class="chart"></div>
        } @else {
          <p class="empty">Sem série no momento</p>
        }
      </div>

      @if (hint) {
        <p class="hint">{{ hint }}</p>
      }
    </section>
  `,
  styles: [`
    .panel {
      --metric: ${METRIC_COLORS.cpu};
      position: relative;
      background:
        linear-gradient(180deg, rgba(255, 255, 255, 0.028), transparent 42%),
        var(--card);
      border: 1px solid var(--line);
      border-radius: 14px;
      padding: 16px 16px 12px;
      min-height: 244px;
      display: flex;
      flex-direction: column;
      overflow: hidden;
      transition: border-color 0.28s ease, transform 0.28s ease, box-shadow 0.28s ease;
    }
    .panel::before {
      content: "";
      position: absolute;
      inset: 0 auto auto 0;
      width: 100%;
      height: 2px;
      background: linear-gradient(90deg, var(--metric), transparent 72%);
      opacity: 0.7;
    }
    .panel:hover {
      border-color: color-mix(in srgb, var(--metric) 42%, var(--line));
      transform: translateY(-2px);
      box-shadow:
        0 14px 36px rgba(0, 0, 0, 0.28),
        0 0 0 1px color-mix(in srgb, var(--metric) 18%, transparent);
    }
    header {
      display: flex;
      justify-content: space-between;
      gap: 12px;
      align-items: flex-start;
    }
    .kicker {
      margin: 0;
      color: var(--metric);
      font-size: 11px;
      letter-spacing: 0.12em;
      text-transform: uppercase;
      font-weight: 600;
    }
    h2 {
      margin: 5px 0 0;
      font-size: 14px;
      font-weight: 500;
      color: var(--text);
      letter-spacing: -0.01em;
    }
    .value {
      font-family: "IBM Plex Mono", ui-monospace, monospace;
      font-size: 26px;
      font-weight: 500;
      color: var(--text);
      letter-spacing: -0.05em;
      line-height: 1.05;
      font-variant-numeric: tabular-nums;
      text-shadow: 0 0 24px color-mix(in srgb, var(--metric) 35%, transparent);
    }
    .track {
      margin-top: 14px;
      height: 4px;
      border-radius: 99px;
      background: var(--track);
      overflow: hidden;
    }
    .fill {
      height: 100%;
      background: linear-gradient(90deg, color-mix(in srgb, var(--metric) 72%, #1a1d22), var(--metric));
      border-radius: 99px;
      box-shadow: 0 0 12px color-mix(in srgb, var(--metric) 45%, transparent);
      transition: width 0.7s cubic-bezier(0.22, 1, 0.36, 1);
    }
    .chart-wrap {
      flex: 1;
      min-height: 118px;
      margin-top: 10px;
      display: flex;
      align-items: stretch;
    }
    .chart { width: 100%; height: 118px; }
    .empty {
      margin: auto;
      color: var(--muted);
      font-size: 12px;
      letter-spacing: 0.01em;
    }
    .hint {
      margin: 8px 0 0;
      color: var(--muted);
      font-size: 11px;
      font-family: "IBM Plex Mono", ui-monospace, monospace;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
    @media (prefers-reduced-motion: reduce) {
      .panel,
      .panel:hover,
      .fill {
        transition: none;
        transform: none;
      }
    }
  `]
})
export class MetricPanelComponent implements OnChanges {
  @Input({ required: true }) kicker = '';
  @Input({ required: true }) title = '';
  @Input() value = '—';
  @Input() hint = '';
  @Input() options: EChartsOption = {};
  @Input() hasSeries = false;
  @Input() progress: number | null = null;
  @Input() accent: string = METRIC_COLORS.cpu;

  initOptions: EChartsOption | null = null;

  get clampedProgress(): number {
    if (this.progress == null) {
      return 0;
    }
    return Math.min(100, Math.max(0, this.progress));
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['hasSeries'] && !this.hasSeries) {
      this.initOptions = null;
      return;
    }
    if (this.hasSeries && !this.initOptions && this.hasChartOption()) {
      this.initOptions = this.options;
    }
  }

  private hasChartOption(): boolean {
    return !!this.options && Object.keys(this.options).length > 0;
  }
}
