import { Component, HostBinding, OnDestroy, OnInit, inject } from '@angular/core';
import { ApiService } from '../core/api.service';
import { formatPct, formatRate, formatTemp, METRIC_COLORS } from '../core/chart.util';
import { MetricsService } from '../core/metrics.service';

interface PopupRow {
  label: string;
  value: string;
  progress: number | null;
  color: string;
}

@Component({
  selector: 'app-popup',
  standalone: true,
  template: `
    <div class="mini">
      <header>
        <div>
          <p class="kicker">CoreLens</p>
          <h1>{{ hostname }}</h1>
        </div>
        <span class="live">
          <span class="dot" [class.on]="connected"></span>
          {{ connected ? 'Ao vivo' : 'Off' }}
        </span>
      </header>

      <ul>
        @for (row of rows; track row.label) {
          <li>
            <div class="row">
              <span>{{ row.label }}</span>
              <strong>{{ row.value }}</strong>
            </div>
            @if (row.progress != null) {
              <div class="track">
                <div class="fill" [style.width.%]="row.progress" [style.--metric]="row.color"></div>
              </div>
            }
          </li>
        }
      </ul>

      <p class="hint">Pode minimizar o dashboard. Este painel continua recebendo métricas.</p>
    </div>
  `,
  styles: [`
    :host {
      display: block;
      min-height: 100vh;
      background: var(--bg);
    }
    .mini {
      padding: 16px 16px 14px;
    }
    header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      gap: 10px;
      margin-bottom: 14px;
      padding-bottom: 12px;
      border-bottom: 1px solid var(--line);
    }
    .kicker {
      margin: 0;
      color: var(--accent);
      font-size: 10px;
      letter-spacing: 0.14em;
      text-transform: uppercase;
      font-weight: 600;
    }
    h1 {
      margin: 4px 0 0;
      font-size: 15px;
      font-weight: 600;
      letter-spacing: -0.02em;
    }
    .live {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      color: var(--muted);
      font-size: 11px;
    }
    .dot {
      width: 6px;
      height: 6px;
      border-radius: 50%;
      background: var(--critical);
    }
    .dot.on { background: var(--ok); }
    ul {
      list-style: none;
      margin: 0;
      padding: 0;
      display: flex;
      flex-direction: column;
      gap: 12px;
    }
    .row {
      display: flex;
      justify-content: space-between;
      gap: 12px;
      font-size: 12px;
      color: var(--muted);
    }
    strong {
      color: var(--text);
      font-family: "IBM Plex Mono", ui-monospace, monospace;
      font-weight: 500;
      font-variant-numeric: tabular-nums;
    }
    .track {
      margin-top: 6px;
      height: 3px;
      border-radius: 99px;
      background: var(--track);
      overflow: hidden;
    }
    .fill {
      height: 100%;
      --metric: #6eb5d8;
      background: linear-gradient(90deg, color-mix(in srgb, var(--metric) 70%, #1a1d22), var(--metric));
      transition: width 0.7s cubic-bezier(0.22, 1, 0.36, 1);
    }
    .dot.on {
      box-shadow: 0 0 0 0 rgba(110, 165, 138, 0.55);
      animation: live-pulse 1.8s ease-out infinite;
    }
    @keyframes live-pulse {
      0% { box-shadow: 0 0 0 0 rgba(110, 165, 138, 0.55); }
      70% { box-shadow: 0 0 0 6px rgba(110, 165, 138, 0); }
      100% { box-shadow: 0 0 0 0 rgba(110, 165, 138, 0); }
    }
    .hint {
      margin: 16px 0 0;
      color: var(--muted);
      font-size: 11px;
      line-height: 1.4;
    }
  `]
})
export class PopupComponent implements OnInit, OnDestroy {
  private readonly api = inject(ApiService);
  private readonly metrics = inject(MetricsService);

  @HostBinding('class.popup-shell') readonly popupShell = true;

  hostname = 'CoreLens';
  connected = false;
  rows: PopupRow[] = [];

  private tickSub?: { unsubscribe(): void };
  private connSub?: { unsubscribe(): void };

  async ngOnInit(): Promise<void> {
    document.title = 'CoreLens';
    this.connSub = this.metrics.connected$.subscribe(value => this.connected = value);
    this.tickSub = this.metrics.tick$.subscribe(() => this.refresh());

    try {
      const computers = await this.api.listComputers();
      const computer = computers[0];
      if (!computer) {
        this.hostname = 'Aguardando agent';
        return;
      }
      this.hostname = computer.hostname;
      await this.metrics.connect(computer.id);
    } catch {
      this.hostname = 'API offline';
    }
  }

  ngOnDestroy(): void {
    this.tickSub?.unsubscribe();
    this.connSub?.unsubscribe();
  }

  private refresh(): void {
    const cpu = this.metrics.getLatest('cpu:0', 'load_pct');
    const ram = this.metrics.getLatest('ram:0', 'used_pct');
    const temp = this.metrics.getLatest('cpu:0', 'temp_c');
    const diskKey = this.metrics.keysByPrefix('disk:', 'used_pct')[0];
    const disk = diskKey ? this.metrics.getLatest(diskKey, 'used_pct') : undefined;
    const netKey = this.metrics.keysByPrefix('net:', 'bytes_recv_per_s')[0];
    const down = netKey ? this.metrics.getLatest(netKey, 'bytes_recv_per_s') : undefined;
    const gpuKey = this.metrics.keysByPrefix('gpu:', 'load_pct')[0] ?? this.metrics.keysByPrefix('gpu:')[0];
    const gpu = gpuKey ? this.metrics.getLatest(gpuKey, 'load_pct') : undefined;

    this.rows = [
      { label: 'CPU', value: formatPct(cpu), progress: cpu ?? null, color: METRIC_COLORS.cpu },
      { label: 'RAM', value: formatPct(ram), progress: ram ?? null, color: METRIC_COLORS.ram },
      { label: 'GPU', value: formatPct(gpu), progress: gpu ?? null, color: METRIC_COLORS.gpu },
      { label: 'Disco', value: formatPct(disk), progress: disk ?? null, color: METRIC_COLORS.disk },
      { label: 'Rede', value: formatRate(down), progress: null, color: METRIC_COLORS.net },
      { label: 'Temp', value: formatTemp(temp), progress: temp ?? null, color: METRIC_COLORS.temp }
    ];
  }
}
