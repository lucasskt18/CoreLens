import { DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { NgxEchartsDirective } from 'ngx-echarts';
import { EChartsOption } from 'echarts';
import { ApiService } from '../core/api.service';
import { historyChart } from '../core/chart.util';
import { ComputerSummary, SeriesPoint } from '../core/models';

@Component({
  selector: 'app-history',
  standalone: true,
  imports: [RouterLink, FormsModule, NgxEchartsDirective, DatePipe],
  template: `
    <div class="page">
      <a routerLink="/" class="back">← Dashboard</a>
      <h1>Histórico</h1>
      <p class="meta">{{ computer?.hostname }} · bucket {{ bucket }}</p>

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

      <div echarts [options]="chart" class="chart"></div>
    </div>
  `,
  styles: [`
    .page { max-width: 1100px; margin: 0 auto; padding: 32px 24px 56px; }
    .back { color: var(--muted); font-size: 13px; }
    .back:hover { color: var(--text); }
    h1 { margin: 14px 0 6px; font-size: 26px; font-weight: 600; letter-spacing: -0.03em; }
    .meta { color: var(--muted); margin: 0; font-size: 13px; }
    .controls { display: flex; gap: 16px; margin: 20px 0; }
    label { display: flex; flex-direction: column; gap: 6px; color: var(--muted); font-size: 11px; letter-spacing: 0.06em; text-transform: uppercase; }
    select {
      background: var(--card);
      color: var(--text);
      border: 1px solid var(--line);
      border-radius: 6px;
      padding: 8px 10px;
    }
    .chart { height: 420px; background: var(--card); border: 1px solid var(--line); border-radius: 10px; padding: 8px 4px 0; }
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
    this.chart = first
      ? historyChart(first[1], `${first[0]} ${this.metricName}`)
      : historyChart([], this.metricName);
  }
}
