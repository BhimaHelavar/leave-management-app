import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../../api-config';

export type HalfDay = 'None' | 'FirstHalf' | 'SecondHalf';

export interface PersonRef {
  id: number;
  name: string;
}

export interface LeaveTypeRef {
  id: number;
  code: string;
  name: string;
}

/** `after` is the requester's available days now, already net of this request; `before` adds its days back. */
export interface BalanceBeforeAfter {
  before: number;
  after: number;
}

export interface OverlapPerson {
  employeeId: number;
  name: string;
  /** The shared dates, yyyy-MM-dd, ascending. */
  dates: string[];
}

/** `awayIfApproved` counts the requester too. The server computes it; the screen never recounts. */
export interface TeamOverlapInfo {
  teamSize: number;
  awayIfApproved: number;
  others: OverlapPerson[];
}

/** One pending request in the manager inbox (contracts/manager-inbox-api.md). No leave reason is ever sent. */
export interface ApprovalItem {
  requestId: number;
  employee: PersonRef;
  leaveType: LeaveTypeRef;
  /** Date only, yyyy-MM-dd. */
  startDate: string;
  /** Date only, yyyy-MM-dd. */
  endDate: string;
  halfDay: HalfDay;
  daysCount: number;
  submittedAtUtc: string;
  /** Null for a leave type with no balance (Unpaid). */
  balance: BalanceBeforeAfter | null;
  teamOverlap: TeamOverlapInfo;
}

export interface ManagerInbox {
  items: ApprovalItem[];
  page: number;
  pageSize: number;
  totalCount: number;
}

/** Typed access to GET /api/approvals. The server decides which requests a manager may see. */
@Injectable({ providedIn: 'root' })
export class ApprovalsService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = inject(API_URL);

  getApprovals(page: number, pageSize: number): Observable<ManagerInbox> {
    return this.http.get<ManagerInbox>(`${this.apiUrl}/api/approvals`, {
      params: { page: String(page), pageSize: String(pageSize) },
    });
  }
}
