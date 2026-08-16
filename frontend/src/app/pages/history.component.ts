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
    .page { max-width: 1100px; margin: 0 auto; padding: 28px 24px; }
    .back { color: var(--accent); }
    h1 { margin: 12px 0 8px; }
    .meta { color: var(--muted); }
    .controls { display: flex; gap: 16px; margin: 18px 0; }
    label { display: flex; flex-direction: column; gap: 6px; color: var(--muted); font-size: 12px; }
    select {
      background: var(--card);
      color: var(--text);
      border: 1px solid var(--line);
      border-radius: 8px;
      padding: 8px 10px;
    }
    .chart { height: 420px; background: var(--card); border: 1px solid var(--line); border-radius: 16px; padding: 8px; }
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
      ? historyChart(first[1], `${first[0]} ${this.metricName}`, '#5eead4')
      : historyChart([], this.metricName, '#5eead4');
  }
}
