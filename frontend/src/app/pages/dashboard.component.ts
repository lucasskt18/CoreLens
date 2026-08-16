import { DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { EChartsOption } from 'echarts';
import { MetricPanelComponent } from '../components/metric-panel.component';
import { ApiService } from '../core/api.service';
import { formatBytes, formatPct, formatRate, formatTemp, sparkline } from '../core/chart.util';
import { MetricsService } from '../core/metrics.service';
import { AlertEventDto, ComputerSummary, InsightDto } from '../core/models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [MetricPanelComponent, RouterLink, DatePipe],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit, OnDestroy {
  private readonly api = inject(ApiService);
  readonly metrics = inject(MetricsService);

  computer?: ComputerSummary;
  waiting = true;
  error?: string;
  insights: InsightDto[] = [];
  alerts: AlertEventDto[] = [];

  cpuChart: EChartsOption = {};
  ramChart: EChartsOption = {};
  diskChart: EChartsOption = {};
  netChart: EChartsOption = {};
  tempChart: EChartsOption = {};
  gpuChart: EChartsOption = {};

  cpuValue = '—';
  ramValue = '—';
  diskValue = '—';
  netValue = '—';
  tempValue = '—';
  gpuValue = '—';
  ramHint = '';
  diskHint = '';
  netHint = '';
  gpuHint = '';

  private poll?: ReturnType<typeof setInterval>;

  async ngOnInit(): Promise<void> {
    this.metrics.tick$.subscribe(() => this.refreshCharts());
    this.metrics.alerts$.subscribe(alerts => this.alerts = alerts);
    await this.bootstrap();
    this.poll = setInterval(() => void this.bootstrap(), 5000);
  }

  ngOnDestroy(): void {
    if (this.poll) {
      clearInterval(this.poll);
    }
  }

  get connected(): boolean {
    return this.metrics.connected$.value;
  }

  private async bootstrap(): Promise<void> {
    try {
      const computers = await this.api.listComputers();
      if (computers.length === 0) {
        this.waiting = true;
        return;
      }

      const selected = computers[0];
      const first = !this.computer;
      this.computer = selected;
      this.waiting = false;
      this.error = undefined;

      if (first) {
        await this.metrics.connect(selected.id);
        this.alerts = await this.api.getAlerts(selected.id);
        this.metrics.alerts$.next(this.alerts);
        this.insights = await this.api.getInsights(selected.id);
      }
    } catch (err) {
      this.error = 'API indisponível. Suba a Core API e o TimescaleDB.';
      console.error(err);
    }
  }

  private refreshCharts(): void {
    const cpu = this.metrics.getLatest('cpu:0', 'load_pct');
    const ram = this.metrics.getLatest('ram:0', 'used_pct');
    const cpuTemp = this.metrics.getLatest('cpu:0', 'temp_c');

    this.cpuValue = formatPct(cpu);
    this.ramValue = formatPct(ram);
    this.tempValue = formatTemp(cpuTemp);
    this.ramHint = `${formatBytes(this.metrics.getLatest('ram:0', 'used_bytes'))} / ${formatBytes(this.metrics.getLatest('ram:0', 'total_bytes'))}`;

    const diskKey = this.metrics.keysByPrefix('disk:', 'used_pct')[0];
    if (diskKey) {
      this.diskValue = formatPct(this.metrics.getLatest(diskKey, 'used_pct'));
      this.diskHint = diskKey;
      this.diskChart = sparkline(this.metrics.getSeries(diskKey, 'used_pct'), '#38bdf8');
    }

    const netKey = this.metrics.keysByPrefix('net:', 'bytes_recv_per_s')[0];
    if (netKey) {
      const down = this.metrics.getLatest(netKey, 'bytes_recv_per_s');
      const up = this.metrics.getLatest(netKey, 'bytes_sent_per_s');
      this.netValue = formatRate(down);
      this.netHint = `up ${formatRate(up)} · ${netKey}`;
      this.netChart = sparkline(this.metrics.getSeries(netKey, 'bytes_recv_per_s'), '#a78bfa');
    }

    const gpuKey = this.metrics.keysByPrefix('gpu:', 'load_pct')[0] ?? this.metrics.keysByPrefix('gpu:')[0];
    if (gpuKey) {
      const load = this.metrics.getLatest(gpuKey, 'load_pct');
      this.gpuValue = load == null ? formatTemp(this.metrics.getLatest(gpuKey, 'temp_c')) : formatPct(load);
      this.gpuHint = gpuKey;
      this.gpuChart = sparkline(
        this.metrics.getSeries(gpuKey, load == null ? 'temp_c' : 'load_pct'),
        '#f472b6'
      );
    }

    this.cpuChart = sparkline(this.metrics.getSeries('cpu:0', 'load_pct'), '#5eead4');
    this.ramChart = sparkline(this.metrics.getSeries('ram:0', 'used_pct'), '#fbbf24');
    this.tempChart = sparkline(this.metrics.getSeries('cpu:0', 'temp_c'), '#fb7185');
  }
}
