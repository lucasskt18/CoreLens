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
          <div
            class="fill"
            [class.warn]="progressLevel === 'warn'"
            [class.hot]="progressLevel === 'hot'"
            [style.width.%]="clampedProgress">
          </div>
        </div>
      }

      <div class="chart-wrap">
        @if (hasSeries && initOptions) {
          <div echarts [options]="initOptions" [merge]="options" class="chart"></div>
        } @else {
          <div class="empty-state">
            <svg viewBox="0 0 160 48" class="ghost" aria-hidden="true">
              <path d="M0 34 C18 34 22 18 40 22 C58 26 62 10 82 16 C102 22 108 30 128 24 C142 20 150 18 160 14" />
            </svg>
            <p class="empty">{{ emptyText }}</p>
          </div>
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
      background: var(--card);
      border: 1px solid var(--line);
      border-radius: var(--radius);
      padding: 18px 18px 14px;
      min-height: 248px;
      display: flex;
      flex-direction: column;
      overflow: hidden;
      transition: border-color 0.22s ease, background 0.22s ease;
    }
    .panel::before {
      content: "";
      position: absolute;
      inset: 0 auto auto 0;
      width: 100%;
      height: 1px;
      background: linear-gradient(90deg, var(--metric), transparent 64%);
      opacity: 0.55;
    }
    .panel:hover {
      border-color: var(--line-strong);
      background: var(--card-elevated);
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
      letter-spacing: 0.14em;
      text-transform: uppercase;
      font-weight: 600;
    }
    h2 {
      margin: 6px 0 0;
      font-size: 14px;
      font-weight: 500;
      color: var(--text);
      letter-spacing: -0.01em;
    }
    .value {
      font-family: "IBM Plex Mono", ui-monospace, monospace;
      font-size: 28px;
      font-weight: 500;
      color: var(--text);
      letter-spacing: -0.04em;
      line-height: 1.05;
      font-variant-numeric: tabular-nums;
    }
    .track {
      margin-top: 14px;
      height: 3px;
      border-radius: 99px;
      background: var(--track);
      overflow: hidden;
    }
    .fill {
      height: 100%;
      background: var(--metric);
      border-radius: 99px;
      transition: width 0.7s cubic-bezier(0.22, 1, 0.36, 1), background 0.3s ease;
    }
    .fill.warn { background: var(--warning); }
    .fill.hot { background: var(--critical); }
    .chart-wrap {
      flex: 1;
      min-height: 118px;
      margin-top: 12px;
      display: flex;
      align-items: stretch;
    }
    .chart { width: 100%; height: 118px; }
    .empty-state {
      flex: 1;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 8px;
    }
    .ghost {
      width: 100%;
      max-width: 180px;
      height: 40px;
      opacity: 0.28;
    }
    .ghost path {
      fill: none;
      stroke: var(--muted);
      stroke-width: 1.4;
      stroke-linecap: round;
    }
    .empty {
      margin: 0;
      color: var(--muted);
      font-size: 12px;
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
      .fill {
        transition: none;
      }
    }
  `]
})
export class MetricPanelComponent implements OnChanges {
  @Input({ required: true }) kicker = '';
  @Input({ required: true }) title = '';
  @Input() value = '—';
  @Input() hint = '';
  @Input() emptyText = 'Aguardando amostras';
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

  get progressLevel(): 'ok' | 'warn' | 'hot' | null {
    if (this.progress == null) {
      return null;
    }
    if (this.progress >= 90) {
      return 'hot';
    }
    if (this.progress >= 75) {
      return 'warn';
    }
    return 'ok';
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
