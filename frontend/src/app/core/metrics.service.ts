import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject } from 'rxjs';
import { AlertEventDto, MetricsBroadcastDto, SeriesPoint } from './models';

const MAX_POINTS = 180;

@Injectable({ providedIn: 'root' })
export class MetricsService {
  private connection?: signalR.HubConnection;
  private computerId?: string;
  private readonly series = new Map<string, SeriesPoint[]>();

  readonly connected$ = new BehaviorSubject(false);
  readonly tick$ = new BehaviorSubject<MetricsBroadcastDto | null>(null);
  readonly alerts$ = new BehaviorSubject<AlertEventDto[]>([]);
  readonly latest = new Map<string, number>();

  async connect(computerId: string): Promise<void> {
    if (this.computerId === computerId && this.connection) {
      return;
    }

    await this.disconnect();
    this.computerId = computerId;
    this.series.clear();
    this.latest.clear();

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/metrics')
      .withAutomaticReconnect()
      .build();

    this.connection.on('metrics', (batch: MetricsBroadcastDto) => this.onMetrics(batch));
    this.connection.on('alert', (alert: AlertEventDto) => {
      this.alerts$.next([alert, ...this.alerts$.value].slice(0, 40));
    });

    this.connection.onreconnected(async () => {
      this.connected$.next(true);
      await this.connection?.invoke('JoinComputer', computerId);
    });
    this.connection.onclose(() => this.connected$.next(false));

    await this.connection.start();
    await this.connection.invoke('JoinComputer', computerId);
    this.connected$.next(true);
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = undefined;
    }
    this.connected$.next(false);
  }

  getSeries(componentKey: string, name: string): SeriesPoint[] {
    return this.series.get(this.key(componentKey, name)) ?? [];
  }

  getLatest(componentKey: string, name: string): number | undefined {
    return this.latest.get(this.key(componentKey, name));
  }

  keysByPrefix(prefix: string, name?: string): string[] {
    const keys: string[] = [];
    for (const key of this.latest.keys()) {
      if (!key.startsWith(prefix)) {
        continue;
      }
      if (name && !key.endsWith(`|${name}`)) {
        continue;
      }
      keys.push(key.split('|')[0]);
    }
    return [...new Set(keys)];
  }

  private onMetrics(batch: MetricsBroadcastDto): void {
    const time = new Date(batch.timestamp).getTime();
    for (const sample of batch.samples) {
      const key = this.key(sample.componentStableKey, sample.name);
      this.latest.set(key, sample.value);
      const points = this.series.get(key) ?? [];
      points.push({ time, value: sample.value });
      if (points.length > MAX_POINTS) {
        points.splice(0, points.length - MAX_POINTS);
      }
      this.series.set(key, points);
    }
    this.tick$.next(batch);
  }

  private key(componentKey: string, name: string): string {
    return `${componentKey}|${name}`;
  }
}
