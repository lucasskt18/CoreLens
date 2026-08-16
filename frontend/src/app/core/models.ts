export interface ComputerSummary {
  id: string;
  hostname: string;
  osVersion: string;
  agentVersion: string;
  lastSeenAt: string;
}

export interface ComponentDto {
  id: string;
  stableKey: string;
  type: string;
  manufacturer?: string | null;
  model?: string | null;
  specs?: Record<string, string> | null;
}

export interface InventoryDto {
  computer: ComputerSummary;
  components: ComponentDto[];
}

export interface MetricSampleDto {
  componentStableKey: string;
  name: string;
  value: number;
}

export interface MetricsBroadcastDto {
  computerId: string;
  timestamp: string;
  samples: MetricSampleDto[];
}

export interface HistoryPointDto {
  time: string;
  componentStableKey: string;
  name: string;
  value: number;
}

export interface HistoryResponseDto {
  computerId: string;
  bucket: string;
  points: HistoryPointDto[];
}

export interface AlertEventDto {
  id: string;
  computerId: string;
  componentId: string;
  componentStableKey: string;
  time: string;
  message: string;
  severity: string;
  value: number;
  metricName: string;
}

export interface InsightDto {
  title: string;
  summary: string;
  provider: string;
}

export interface SeriesPoint {
  time: number;
  value: number;
}
