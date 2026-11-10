import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from './api-config';

export interface Health {
  status: string;
  authMode: string;
  database: string;
  environment: string;
  timeUtc: string;
}

@Injectable({ providedIn: 'root' })
export class HealthService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = inject(API_URL);

  check(): Observable<Health> {
    return this.http.get<Health>(`${this.apiUrl}/health`);
  }
}
