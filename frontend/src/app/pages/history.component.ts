import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgxEchartsDirective } from 'ngx-echarts';
import { EChartsOption } from 'echarts';
import { ApiService } from '../core/api.service';
import { METRIC_COLORS, historyChart } from '../core/chart.util';
import { ComputerSummary, SeriesPoint } from '../core/models';

@Component({
  selector: 'app-history',
  standalone: true,
  imports: [FormsModule, NgxEchartsDirective],
  template: `
    <div class="page">
      <header class="hero">
        <div>
          <p class="kicker">{{ computer?.hostname || 'Aguardando agent' }}</p>
          <h1>Histórico</h1>
          <p class="meta">Agregação {{ bucket }}</p>
        </div>
      </header>

      <div class="controls">
        <label>Métrica
          <select [(ngModel)]="metricName" (change)="load()">
            <option value="load_pct">CPU / GPU load %</option>
            <option value="used_pct">RAM / disco usado %</option>
            <option value="temp_c">Temperatura</option>
            <option value="bytes_recv_per_s">Rede download</option>
          </select>
        </label>
        <label>Janela
          <select [(ngModel)]="hours" (change)="load()">
            <option [ngValue]="1">1 hora</option>
            <option [ngValue]="6">6 horas</option>
            <option [ngValue]="24">24 horas</option>
            <option [ngValue]="168">7 dias</option>
          </select>
        </label>
      </div>

      <div class="chart-card">
        <div echarts [options]="chart" class="chart"></div>
      </div>
    </div>
  `,
  styles: [`
    .page { padding-top: 24px; }
    .hero { margin-bottom: 20px; }
    .kicker {
      margin: 0;
      color: var(--muted);
      letter-spacing: 0.14em;
      text-transform: uppercase;
      font-size: 11px;
      font-weight: 600;
    }
    h1 {
      margin: 6px 0 8px;
      font-size: 26px;
      font-weight: 600;
      letter-spacing: -0.04em;
    }
    .meta { color: var(--muted); margin: 0; font-size: 13px; }
    .controls { display: flex; flex-wrap: wrap; gap: 16px; margin: 0 0 18px; }
    label {
      display: flex;
      flex-direction: column;
      gap: 6px;
      color: var(--muted);
      font-size: 11px;
      letter-spacing: 0.08em;
      text-transform: uppercase;
      font-weight: 600;
    }
    select {
      background: var(--card);
      color: var(--text);
      border: 1px solid var(--line);
      border-radius: var(--radius-sm);
      padding: 9px 12px;
      min-width: 200px;
      outline: none;
      transition: border-color 0.2s ease;
    }
    select:hover, select:focus {
      border-color: var(--line-strong);
    }
    .chart-card {
      background: var(--card);
      border: 1px solid var(--line);
      border-radius: var(--radius);
      padding: 12px 10px 6px;
    }
    .chart { height: 440px; }
  `]
})
export class HistoryComponent implements OnInit {
  private readonly api = inject(ApiService);
  computer?: ComputerSummary;
  metricName = 'load_pct';
  hours = 1;
  bucket = '1s';
  chart: EChartsOption = {};

  async ngOnInit(): Promise<void> {
    const computers = await this.api.listComputers();
    this.computer = computers[0];
    await this.load();
  }

  async load(): Promise<void> {
    if (!this.computer) {
      return;
    }

    const to = new Date();
    const from = new Date(to.getTime() - this.hours * 3600_000);
    const history = await this.api.getHistory(this.computer.id, from, to, this.metricName);
    this.bucket = history.bucket;

    const grouped = new Map<string, SeriesPoint[]>();
    for (const point of history.points) {
      const list = grouped.get(point.componentStableKey) ?? [];
      list.push({ time: new Date(point.time).getTime(), value: point.value });
      grouped.set(point.componentStableKey, list);
    }

    const first = [...grouped.entries()][0];
    const color =
      this.metricName === 'temp_c' ? METRIC_COLORS.temp
      : this.metricName === 'used_pct' ? METRIC_COLORS.ram
      : this.metricName === 'bytes_recv_per_s' ? METRIC_COLORS.net
      : METRIC_COLORS.cpu;
    this.chart = first
      ? historyChart(first[1], `${first[0]} ${this.metricName}`, color)
      : historyChart([], this.metricName, color);
  }
}
