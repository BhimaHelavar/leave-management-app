import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../../api-config';

export type HalfDay = 'None' | 'FirstHalf' | 'SecondHalf';

/** What the person chooses. There is no employee, status or day count: the server sets those. */
export interface ApplyLeaveBody {
  leaveTypeId: number;
  /** Date only, yyyy-MM-dd. */
  startDate: string;
  /** Date only, yyyy-MM-dd. */
  endDate: string;
  halfDay: HalfDay;
  /** Up to 500 characters. Optional. */
  reason?: string | null;
}

/** The saved request. `daysCount` is the server's count; the screen never counts days itself. */
export interface LeaveRequestSaved {
  id: number;
  leaveTypeId: number;
  startDate: string;
  endDate: string;
  halfDay: HalfDay;
  daysCount: number;
  status: string;
  leaveYear: number;
}

/** What the form asks the server to count. The same fields as an application, without a reason. */
export interface PreviewQuery {
  leaveTypeId: number;
  startDate: string;
  endDate: string;
  halfDay: HalfDay;
}

/** The server's count for the dates, before the person submits. Information only. */
export interface LeavePreview {
  daysCount: number;
  /** The counted dates, yyyy-MM-dd. */
  workingDays: string[];
  /** Weekday holidays inside the range that were not counted. */
  skippedHolidays: string[];
}

/** The ProblemDetails body of a refused request (contracts/leave-requests-api.md). */
export interface RefusalProblem {
  title?: string;
  detail?: string;
  /** Stable machine code such as `notice.short`. Present on 409 and 422. */
  code?: string;
}

/** Typed access to POST /api/me/leave-requests and GET /api/me/leave-requests/preview. */
@Injectable({ providedIn: 'root' })
export class LeaveRequestService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = inject(API_URL);

  /**
   * Applies for leave. The same idempotency key with the same body returns the same answer, so a retry
   * or a double click never saves twice.
   */
  apply(body: ApplyLeaveBody, idempotencyKey: string): Observable<LeaveRequestSaved> {
    return this.http.post<LeaveRequestSaved>(`${this.apiUrl}/api/me/leave-requests`, body, {
      headers: { 'Idempotency-Key': idempotencyKey },
    });
  }

  /**
   * Asks the server how many working days these dates count. The screen never counts days itself, so this
   * is the only way the form can show a number, and it is the number a submit would save.
   */
  preview(query: PreviewQuery): Observable<LeavePreview> {
    return this.http.get<LeavePreview>(`${this.apiUrl}/api/me/leave-requests/preview`, {
      params: {
        leaveTypeId: String(query.leaveTypeId),
        startDate: query.startDate,
        endDate: query.endDate,
        halfDay: query.halfDay,
      },
    });
  }
}
