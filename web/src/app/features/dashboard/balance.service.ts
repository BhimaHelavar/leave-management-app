import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../../api-config';

/** One leave type for the signed-in person. When `hasBalance` is false every number is null. */
export interface LeaveTypeBalance {
  leaveTypeId: number;
  code: string;
  name: string;
  hasBalance: boolean;
  accrued: number | null;
  used: number | null;
  pending: number | null;
  available: number | null;
  carryForwardDays: number | null;
  /** Date only, yyyy-MM-dd. */
  carryForwardExpiresOn: string | null;
}

export interface MyBalances {
  leaveYear: number;
  balances: LeaveTypeBalance[];
}

/** Typed access to GET /api/me/balances (contracts/balances-api.md). The server computes every figure. */
@Injectable({ providedIn: 'root' })
export class BalanceService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = inject(API_URL);

  getMyBalances(): Observable<MyBalances> {
    return this.http.get<MyBalances>(`${this.apiUrl}/api/me/balances`);
  }
}
