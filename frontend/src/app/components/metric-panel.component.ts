import { Component, Input } from '@angular/core';
import { NgxEchartsDirective } from 'ngx-echarts';
import { EChartsOption } from 'echarts';

@Component({
  selector: 'app-metric-panel',
  standalone: true,
  imports: [NgxEchartsDirective],
  template: `
    <section class="panel">
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
        @if (hasSeries) {
          <div echarts [options]="options" class="chart"></div>
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
      background: var(--card);
      border: 1px solid var(--line);
      border-radius: 10px;
      padding: 16px 16px 12px;
      min-height: 228px;
      display: flex;
      flex-direction: column;
    }
    header {
      display: flex;
      justify-content: space-between;
      gap: 12px;
      align-items: flex-start;
    }
    .kicker {
      margin: 0;
      color: var(--muted);
      font-size: 11px;
      letter-spacing: 0.08em;
      text-transform: uppercase;
      font-weight: 500;
    }
    h2 {
      margin: 4px 0 0;
      font-size: 14px;
      font-weight: 500;
      color: var(--text);
    }
    .value {
      font-family: "IBM Plex Mono", ui-monospace, monospace;
      font-size: 22px;
      font-weight: 500;
      color: var(--text);
      letter-spacing: -0.03em;
      line-height: 1.1;
    }
    .track {
      margin-top: 12px;
      height: 3px;
      border-radius: 99px;
      background: var(--track);
      overflow: hidden;
    }
    .fill {
      height: 100%;
      background: var(--chart);
      border-radius: 99px;
    }
    .chart-wrap {
      flex: 1;
      min-height: 108px;
      margin-top: 8px;
      display: flex;
      align-items: stretch;
    }
    .chart { width: 100%; height: 108px; }
    .empty {
      margin: auto;
      color: var(--muted);
      font-size: 12px;
    }
    .hint {
      margin: 8px 0 0;
      color: var(--muted);
      font-size: 11px;
      font-family: "IBM Plex Mono", ui-monospace, monospace;
    }
  `]
})
export class MetricPanelComponent {
  @Input({ required: true }) kicker = '';
  @Input({ required: true }) title = '';
  @Input() value = '—';
  @Input() hint = '';
  @Input() options: EChartsOption = {};
  @Input() hasSeries = false;
  @Input() progress: number | null = null;

  get clampedProgress(): number {
    if (this.progress == null) {
      return 0;
    }
    return Math.min(100, Math.max(0, this.progress));
  }
}
