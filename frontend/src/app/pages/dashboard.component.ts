import { DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { EChartsOption } from 'echarts';
import { MetricPanelComponent } from '../components/metric-panel.component';
import { ApiService } from '../core/api.service';
import { METRIC_COLORS, formatBytes, formatPct, formatRate, formatTemp, friendlyComponentLabel, sparkline, sparklinePair } from '../core/chart.util';
import { MetricsService } from '../core/metrics.service';
import { AlertEventDto, ComputerSummary, InsightDto } from '../core/models';
import { PopupWindowService } from '../core/popup-window.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [MetricPanelComponent, RouterLink, DatePipe],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit, OnDestroy {
  private readonly api = inject(ApiService);
  private readonly popupWindow = inject(PopupWindowService);
  readonly metrics = inject(MetricsService);

  computer?: ComputerSummary;
  waiting = true;
  error?: string;
  insights: InsightDto[] = [];
  showInsights = false;
  alerts: AlertEventDto[] = [];

  cpuChart: EChartsOption = {};
  netChart: EChartsOption = {};
  gpuChart: EChartsOption = {};

  cpuValue = '—';
  ramValue = '—';
  diskValue = '—';
  netValue = '—';
  tempValue = '—';
  gpuValue = '—';
  gpuTemp = '';
  ramCapacity = '—';
  diskCapacity = '—';
  diskHint = '';
  netHint = '';
  gpuHint = '';
  tempHint = '';
  cpuPct: number | null = null;
  ramPct: number | null = null;
  diskPct: number | null = null;
  gpuPct: number | null = null;
  cpuHasSeries = false;
  netHasSeries = false;
  gpuHasSeries = false;
  readonly colors = METRIC_COLORS;

  private poll?: ReturnType<typeof setInterval>;

  async ngOnInit(): Promise<void> {
    this.metrics.tick$.subscribe(() => {
      try {
        this.refreshCharts();
      } catch (err) {
        console.error(err);
      }
    });
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

  openPopup(): void {
    const opened = this.popupWindow.open();
    if (!opened) {
      this.error = 'O navegador bloqueou o pop-up. Permita pop-ups para localhost:4200.';
    }
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
        await this.hydrateFromHistory(selected.id);
        this.alerts = await this.api.getAlerts(selected.id);
        this.metrics.alerts$.next(this.alerts);
        const insights = await this.api.getInsights(selected.id);
        this.showInsights = insights.some(insight => insight.provider !== 'none');
        this.insights = this.showInsights ? insights : [];
      }
    } catch (err) {
      this.error = 'API indisponível. Suba a Core API e o TimescaleDB.';
      console.error(err);
    }
  }

  private async hydrateFromHistory(computerId: string): Promise<void> {
    try {
      const to = new Date();
      const from = new Date(to.getTime() - 30 * 60_000);
      const history = await this.api.getHistory(computerId, from, to);
      this.metrics.seed(history.points);
    } catch (err) {
      console.error(err);
    }
  }

  private refreshCharts(): void {
    const cpu = this.metrics.getLatest('cpu:0', 'load_pct');
    const ram = this.metrics.getLatest('ram:0', 'used_pct');
    const cpuTemp = this.metrics.getLatest('cpu:0', 'temp_c');
    const cpuSeries = this.metrics.getSeries('cpu:0', 'load_pct');

    this.cpuValue = formatPct(cpu);
    this.ramValue = formatPct(ram);
    this.tempValue = formatTemp(cpuTemp);
    this.cpuPct = cpu ?? null;
    this.ramPct = ram ?? null;
    this.ramCapacity = `${formatBytes(this.metrics.getLatest('ram:0', 'used_bytes'))} / ${formatBytes(this.metrics.getLatest('ram:0', 'total_bytes'))}`;
    this.cpuHasSeries = cpuSeries.length > 1;
    this.tempHint = cpuTemp == null ? 'Temp requer o agent em modo elevado' : '';
    this.cpuChart = sparkline(cpuSeries, 'percent', METRIC_COLORS.cpu);

    const diskKey = this.metrics.keysByPrefix('disk:', 'used_pct')[0];
    if (diskKey) {
      this.diskPct = this.metrics.getLatest(diskKey, 'used_pct') ?? null;
      this.diskValue = formatPct(this.diskPct ?? undefined);
      this.diskCapacity = `${formatBytes(this.metrics.getLatest(diskKey, 'used_bytes'))} / ${formatBytes(this.metrics.getLatest(diskKey, 'total_bytes'))}`;
      this.diskHint = friendlyComponentLabel(diskKey);
    }

    const netKey = this.metrics.keysByPrefix('net:', 'bytes_recv_per_s')[0];
    if (netKey) {
      const downSeries = this.metrics.getSeries(netKey, 'bytes_recv_per_s');
      const upSeries = this.metrics.getSeries(netKey, 'bytes_sent_per_s');
      const down = this.metrics.getLatest(netKey, 'bytes_recv_per_s');
      const up = this.metrics.getLatest(netKey, 'bytes_sent_per_s');
      this.netValue = formatRate(down);
      this.netHint = `envio ${formatRate(up)} · ${friendlyComponentLabel(netKey)}`;
      this.netHasSeries = downSeries.length > 1;
      this.netChart = sparklinePair(downSeries, upSeries, METRIC_COLORS.net);
    }

    const gpuKey = this.metrics.keysByPrefix('gpu:', 'load_pct')[0] ?? this.metrics.keysByPrefix('gpu:')[0];
    if (gpuKey) {
      const load = this.metrics.getLatest(gpuKey, 'load_pct');
      const gpuTempValue = this.metrics.getLatest(gpuKey, 'temp_c');
      const gpuMetric = load == null ? 'temp_c' : 'load_pct';
      const gpuSeries = this.metrics.getSeries(gpuKey, gpuMetric);
      this.gpuValue = load == null ? formatTemp(gpuTempValue) : formatPct(load);
      this.gpuPct = load ?? null;
      this.gpuTemp = gpuTempValue == null ? '' : formatTemp(gpuTempValue);
      this.gpuHint = friendlyComponentLabel(gpuKey);
      this.gpuHasSeries = gpuSeries.length > 1;
      this.gpuChart = sparkline(gpuSeries, load == null ? 'temp' : 'percent', METRIC_COLORS.gpu);
    }
  }
}
