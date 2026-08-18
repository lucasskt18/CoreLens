import { DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { NgxEchartsDirective } from 'ngx-echarts';
import { EChartsOption } from 'echarts';
import { ApiService } from '../core/api.service';
import { METRIC_COLORS, historyChart } from '../core/chart.util';
import { ComputerSummary, SeriesPoint } from '../core/models';

@Component({
  selector: 'app-history',
  standalone: true,
  imports: [RouterLink, FormsModule, NgxEchartsDirective, DatePipe],
  template: `
    <div class="page">
      <a routerLink="/" class="back">← Dashboard</a>
      <header class="hero">
        <div>
          <p class="kicker">CoreLens</p>
          <h1>Histórico</h1>
          <p class="meta">{{ computer?.hostname || 'Aguardando agent' }} · bucket {{ bucket }}</p>
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
    .page { max-width: 1100px; margin: 0 auto; padding: 32px 24px 64px; }
    .back {
      color: var(--muted);
      font-size: 13px;
      transition: color 0.2s ease;
    }
    .back:hover { color: var(--text); }
    .hero {
      margin: 14px 0 8px;
      padding-bottom: 18px;
      border-bottom: 1px solid var(--line);
    }
    .kicker {
      margin: 0;
      color: var(--accent);
      letter-spacing: 0.16em;
      text-transform: uppercase;
      font-size: 11px;
      font-weight: 600;
    }
    h1 { margin: 8px 0 6px; font-size: 30px; font-weight: 600; letter-spacing: -0.04em; }
    .meta { color: var(--muted); margin: 0; font-size: 13px; }
    .controls { display: flex; gap: 16px; margin: 20px 0; }
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
      border-radius: 10px;
      padding: 9px 12px;
      min-width: 200px;
      outline: none;
      transition: border-color 0.2s ease;
    }
    select:hover, select:focus {
      border-color: rgba(142, 171, 200, 0.35);
    }
    .chart-card {
      background:
        linear-gradient(180deg, rgba(255, 255, 255, 0.025), transparent 28%),
        var(--card);
      border: 1px solid var(--line);
      border-radius: 14px;
      padding: 10px 8px 4px;
    }
    .chart { height: 420px; }
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
