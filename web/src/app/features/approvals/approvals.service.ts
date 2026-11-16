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

export type RequestStatus = 'Pending' | 'Approved' | 'Rejected' | 'Cancelled';

export type AllowedAction = 'approve' | 'reject';

/** Present once a request is decided. The comment is user text: show it with interpolation only. */
export interface DecisionInfo {
  outcome: string;
  decidedAtUtc: string;
  comment: string | null;
}

/** One request (contracts/decision-api.md). `balance` and `teamOverlap` are null once decided. */
export interface ApprovalDetail {
  requestId: number;
  status: RequestStatus;
  employee: PersonRef;
  leaveType: LeaveTypeRef;
  startDate: string;
  endDate: string;
  halfDay: HalfDay;
  daysCount: number;
  submittedAtUtc: string;
  balance: BalanceBeforeAfter | null;
  teamOverlap: TeamOverlapInfo | null;
  decision: DecisionInfo | null;
  /** The server says which buttons exist; the screen copies no rule. */
  allowedActions: AllowedAction[];
}

/** Typed access to /api/approvals. The server decides which requests a manager may see or decide. */
@Injectable({ providedIn: 'root' })
export class ApprovalsService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = inject(API_URL);

  getApprovals(page: number, pageSize: number): Observable<ManagerInbox> {
    return this.http.get<ManagerInbox>(`${this.apiUrl}/api/approvals`, {
      params: { page: String(page), pageSize: String(pageSize) },
    });
  }

  getApproval(requestId: number): Observable<ApprovalDetail> {
    return this.http.get<ApprovalDetail>(`${this.apiUrl}/api/approvals/${requestId}`);
  }

  approve(requestId: number, comment: string | null): Observable<ApprovalDetail> {
    return this.http.post<ApprovalDetail>(`${this.apiUrl}/api/approvals/${requestId}/approve`, {
      comment,
    });
  }

  reject(requestId: number, comment: string): Observable<ApprovalDetail> {
    return this.http.post<ApprovalDetail>(`${this.apiUrl}/api/approvals/${requestId}/reject`, {
      comment,
    });
  }
}
