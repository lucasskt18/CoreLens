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
      <div echarts [options]="options" class="chart"></div>
      @if (hint) {
        <p class="hint">{{ hint }}</p>
      }
    </section>
  `,
  styles: [`
    .panel {
      background: var(--card);
      border: 1px solid var(--line);
      border-radius: 16px;
      padding: 16px 16px 8px;
      min-height: 220px;
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
      letter-spacing: 0.14em;
      text-transform: uppercase;
    }
    h2 {
      margin: 4px 0 0;
      font-size: 16px;
      font-weight: 600;
    }
    .value {
      font-family: "IBM Plex Mono", monospace;
      font-size: 28px;
      color: var(--accent);
    }
    .chart { height: 140px; }
    .hint { margin: 0 0 8px; color: var(--muted); font-size: 12px; }
  `]
})
export class MetricPanelComponent {
  @Input({ required: true }) kicker = '';
  @Input({ required: true }) title = '';
  @Input() value = '—';
  @Input() hint = '';
  @Input() options: EChartsOption = {};
}
