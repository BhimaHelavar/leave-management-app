import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_URL } from '../../api-config';
import { ApprovalsService, ManagerInbox } from './approvals.service';

const API = 'http://api.test';

function setup() {
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(),
      provideHttpClientTesting(),
      { provide: API_URL, useValue: API },
    ],
  });
  return {
    service: TestBed.inject(ApprovalsService),
    http: TestBed.inject(HttpTestingController),
  };
}

describe('ApprovalsService', () => {
  it('asks for the page and page size and returns the inbox', () => {
    const { service, http } = setup();
    const inbox: ManagerInbox = {
      items: [
        {
          requestId: 35,
          employee: { id: 6, name: 'Sneha Iyer' },
          leaveType: { id: 1, code: 'ANNUAL', name: 'Annual leave' },
          startDate: '2026-12-01',
          endDate: '2026-12-02',
          halfDay: 'None',
          daysCount: 2,
          submittedAtUtc: '2026-11-14T11:51:50Z',
          balance: { before: 8, after: 6 },
          teamOverlap: { teamSize: 3, awayIfApproved: 1, others: [] },
        },
      ],
      page: 2,
      pageSize: 20,
      totalCount: 21,
    };

    let result: ManagerInbox | undefined;
    service.getApprovals(2, 20).subscribe((value) => (result = value));

    const req = http.expectOne((r) => r.url === `${API}/api/approvals` && r.method === 'GET');
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('20');
    req.flush(inbox);

    expect(result).toEqual(inbox);
    http.verify();
  });

  it.each([403, 500])('passes a %s on as an error', (status) => {
    const { service, http } = setup();

    let error: HttpErrorResponse | undefined;
    service.getApprovals(1, 20).subscribe({ error: (e: HttpErrorResponse) => (error = e) });

    http
      .expectOne((r) => r.url === `${API}/api/approvals`)
      .flush({ title: 'Refused' }, { status, statusText: 'Refused' });

    expect(error?.status).toBe(status);
    http.verify();
  });
});
