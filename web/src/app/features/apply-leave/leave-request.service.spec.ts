import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_URL } from '../../api-config';
import {
  ApplyLeaveBody,
  LeavePreview,
  LeaveRequestSaved,
  LeaveRequestService,
  RefusalProblem,
} from './leave-request.service';

const API = 'http://api.test';

const body: ApplyLeaveBody = {
  leaveTypeId: 1,
  startDate: '2026-11-27',
  endDate: '2026-12-01',
  halfDay: 'None',
  reason: null,
};

function setup() {
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(),
      provideHttpClientTesting(),
      { provide: API_URL, useValue: API },
    ],
  });
  return {
    service: TestBed.inject(LeaveRequestService),
    http: TestBed.inject(HttpTestingController),
  };
}

describe('LeaveRequestService', () => {
  it('posts the application with the idempotency key and returns the saved request', () => {
    const { service, http } = setup();
    const saved: LeaveRequestSaved = {
      id: 5,
      leaveTypeId: 1,
      startDate: '2026-11-27',
      endDate: '2026-12-01',
      halfDay: 'None',
      daysCount: 2,
      status: 'Pending',
      leaveYear: 2026,
    };
    let result: LeaveRequestSaved | undefined;

    service.apply(body, 'key-1').subscribe((value) => (result = value));
    const request = http.expectOne(`${API}/api/me/leave-requests`);
    expect(request.request.method).toBe('POST');
    expect(request.request.headers.get('Idempotency-Key')).toBe('key-1');
    expect(request.request.body).toEqual(body);
    request.flush(saved, { status: 201, statusText: 'Created' });

    expect(result).toEqual(saved);
    http.verify();
  });

  it('asks the server for the preview with the chosen dates as query parameters', () => {
    const { service, http } = setup();
    const preview: LeavePreview = {
      daysCount: 2,
      workingDays: ['2026-11-27', '2026-12-01'],
      skippedHolidays: ['2026-11-30'],
    };
    let result: LeavePreview | undefined;

    service
      .preview({ leaveTypeId: 1, startDate: '2026-11-27', endDate: '2026-12-01', halfDay: 'None' })
      .subscribe((value) => (result = value));
    const request = http.expectOne((r) => r.url === `${API}/api/me/leave-requests/preview`);
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('leaveTypeId')).toBe('1');
    expect(request.request.params.get('startDate')).toBe('2026-11-27');
    expect(request.request.params.get('endDate')).toBe('2026-12-01');
    expect(request.request.params.get('halfDay')).toBe('None');
    request.flush(preview);

    expect(result).toEqual(preview);
    http.verify();
  });

  it('passes a preview refusal on to the caller', () => {
    const { service, http } = setup();
    let problem: RefusalProblem | undefined;

    service
      .preview({ leaveTypeId: 1, startDate: '2026-12-05', endDate: '2026-12-06', halfDay: 'None' })
      .subscribe({ error: (e: HttpErrorResponse) => (problem = e.error as RefusalProblem) });
    http
      .expectOne((r) => r.url === `${API}/api/me/leave-requests/preview`)
      .flush({ title: 'Request refused', code: 'days.none' }, { status: 422, statusText: 'Unprocessable Entity' });

    expect(problem?.code).toBe('days.none');
    http.verify();
  });

  it('passes a refusal with its code on to the caller', () => {
    const { service, http } = setup();
    let problem: RefusalProblem | undefined;
    let status: number | undefined;

    service.apply(body, 'key-2').subscribe({
      error: (e: HttpErrorResponse) => {
        status = e.status;
        problem = e.error as RefusalProblem;
      },
    });
    http.expectOne(`${API}/api/me/leave-requests`).flush(
      { title: 'Request refused', detail: 'You do not have enough balance for these days.', code: 'balance.insufficient' },
      { status: 422, statusText: 'Unprocessable Entity' },
    );

    expect(status).toBe(422);
    expect(problem?.code).toBe('balance.insufficient');
    http.verify();
  });
});
