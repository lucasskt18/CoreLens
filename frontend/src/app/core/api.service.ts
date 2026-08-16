import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  AlertEventDto,
  ComputerSummary,
  HistoryResponseDto,
  InsightDto,
  InventoryDto
} from './models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  constructor(private readonly http: HttpClient) {}

  listComputers(): Promise<ComputerSummary[]> {
    return firstValueFrom(this.http.get<ComputerSummary[]>('/api/computers'));
  }

  getInventory(computerId: string): Promise<InventoryDto> {
    return firstValueFrom(this.http.get<InventoryDto>(`/api/computers/${computerId}`));
  }

  getHistory(computerId: string, from: Date, to: Date, name?: string): Promise<HistoryResponseDto> {
    let params = new HttpParams()
      .set('from', from.toISOString())
      .set('to', to.toISOString());
    if (name) {
      params = params.set('name', name);
    }

    return firstValueFrom(
      this.http.get<HistoryResponseDto>(`/api/computers/${computerId}/history`, { params })
    );
  }

  getAlerts(computerId: string): Promise<AlertEventDto[]> {
    return firstValueFrom(this.http.get<AlertEventDto[]>(`/api/computers/${computerId}/alerts`));
  }

  getInsights(computerId: string): Promise<InsightDto[]> {
    return firstValueFrom(this.http.get<InsightDto[]>(`/api/computers/${computerId}/insights`));
  }
}
