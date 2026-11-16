import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { NEVER, Observable, of, throwError } from 'rxjs';
import { ApprovalDetailPage } from './approval-detail';
import { ApprovalDetail, ApprovalsService } from './approvals.service';

const PENDING: ApprovalDetail = {
  requestId: 35,
  status: 'Pending',
  employee: { id: 6, name: 'Sneha Iyer' },
  leaveType: { id: 1, code: 'ANNUAL', name: 'Annual leave' },
  startDate: '2026-12-01',
  endDate: '2026-12-02',
  halfDay: 'None',
  daysCount: 2,
  submittedAtUtc: '2026-11-14T11:51:50Z',
  balance: { before: 8, after: 6 },
  teamOverlap: { teamSize: 3, awayIfApproved: 1, others: [] },
  decision: null,
  allowedActions: ['approve', 'reject'],
};

const REJECTED: ApprovalDetail = {
  ...PENDING,
  status: 'Rejected',
  balance: null,
  teamOverlap: null,
  decision: {
    outcome: 'Rejected',
    decidedAtUtc: '2026-11-16T09:00:00Z',
    comment: '<b>Please</b> move it',
  },
  allowedActions: [],
};

function refusal(status: number, detail?: string): Observable<never> {
  return throwError(
    () => new HttpErrorResponse({ status, error: detail ? { detail } : { title: 'x' } }),
  );
}

function setup(service: Partial<ApprovalsService>, id = '35') {
  TestBed.configureTestingModule({
    imports: [ApprovalDetailPage],
    providers: [
      provideRouter([]),
      { provide: ApprovalsService, useValue: service },
      {
        provide: ActivatedRoute,
        useValue: { snapshot: { paramMap: convertToParamMap({ requestId: id }) } },
      },
    ],
  });
  const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
  const fixture: ComponentFixture<ApprovalDetailPage> = TestBed.createComponent(ApprovalDetailPage);
  fixture.detectChanges();
  const root = fixture.nativeElement as HTMLElement;
  const buttons = () =>
    Array.from(root.querySelectorAll('button')).map((b) => b.textContent?.trim());
  const click = (label: string) => {
    const button = Array.from(root.querySelectorAll('button')).find(
      (b) => b.textContent?.trim() === label,
    );
    button?.click();
    fixture.detectChanges();
  };
  const type = (text: string) => {
    const box = root.querySelector('textarea') as HTMLTextAreaElement;
    box.value = text;
    box.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  };
  return { fixture, root, navigate, buttons, click, type };
}

describe('ApprovalDetailPage', () => {
  it('shows a loading message while the request loads', () => {
    const { root } = setup({ getApproval: () => NEVER });

    expect(root.querySelector('[role="status"]')?.textContent).toContain('Loading request...');
  });

  it('shows a pending request with both buttons', () => {
    const { root, buttons } = setup({ getApproval: () => of(PENDING) });

    expect(root.textContent).toContain('Sneha Iyer');
    expect(root.textContent).toContain('Before 8.0, after 6.0');
    expect(root.textContent).toContain('No one else in your team is away');
    expect(buttons()).toEqual(['Approve', 'Reject']);
  });

  it('shows a decided request with the comment as plain text and no buttons', () => {
    const { root, buttons } = setup({ getApproval: () => of(REJECTED) });

    expect(root.textContent).toContain('Rejected on 2026-11-16');
    expect(root.querySelector('.comment')?.textContent).toBe('<b>Please</b> move it');
    expect(root.querySelector('.comment b')).toBeNull();
    expect(buttons()).toEqual([]);
  });

  it('sends nothing when a rejection has no comment', () => {
    const reject = vi.fn();
    const { root, click, type } = setup({ getApproval: () => of(PENDING), reject });

    click('Reject');
    click('Send rejection');
    expect(root.querySelector('[role="alert"]')?.textContent).toContain('Please write a comment');

    type('   ');
    click('Send rejection');

    expect(reject).not.toHaveBeenCalled();
  });

  it('rejects with the comment and then shows the new state', () => {
    const reject = vi.fn().mockReturnValue(of(REJECTED));
    const { root, buttons, click, type } = setup({ getApproval: () => of(PENDING), reject });

    click('Reject');
    type('  Please move it  ');
    click('Send rejection');

    expect(reject).toHaveBeenCalledWith(35, 'Please move it');
    expect(root.textContent).toContain('Rejected on');
    expect(buttons()).toEqual([]);
  });

  it('approves without a comment', () => {
    const approve = vi
      .fn()
      .mockReturnValue(of({ ...PENDING, status: 'Approved', allowedActions: [] }));
    const { root, click } = setup({ getApproval: () => of(PENDING), approve });

    click('Approve');

    expect(approve).toHaveBeenCalledWith(35, null);
    expect(root.textContent).toContain('Approved');
  });

  it('shows the server message on a 409 and reloads the real state', () => {
    const getApproval = vi.fn().mockReturnValueOnce(of(PENDING)).mockReturnValue(of(REJECTED));
    const approve = vi.fn().mockReturnValue(refusal(409, 'This request has already been decided.'));
    const { root, buttons, click } = setup({ getApproval, approve });

    click('Approve');

    expect(root.querySelector('.refusal')?.textContent).toContain('already been decided');
    expect(buttons()).toEqual([]);
  });

  it('shows the server message on a 422 and keeps the buttons', () => {
    const reject = vi.fn().mockReturnValue(refusal(422, 'A rejection needs a comment.'));
    const { root, click, type } = setup({ getApproval: () => of(PENDING), reject });

    click('Reject');
    type('text');
    click('Send rejection');

    expect(root.querySelector('.refusal')?.textContent).toContain('needs a comment');
  });

  it('goes to the not-found page on a 404', () => {
    const { navigate } = setup({ getApproval: () => refusal(404) });

    expect(navigate).toHaveBeenCalledWith('/not-found');
  });

  it('goes to the not-found page for an id that is not a number', () => {
    const getApproval = vi.fn();
    const { navigate } = setup({ getApproval }, 'abc');

    expect(getApproval).not.toHaveBeenCalled();
    expect(navigate).toHaveBeenCalledWith('/not-found');
  });

  it('shows an error with Try again when loading fails', () => {
    const getApproval = vi.fn().mockReturnValueOnce(refusal(500)).mockReturnValue(of(PENDING));
    const { root, click } = setup({ getApproval });

    expect(root.querySelector('[role="alert"]')?.textContent).toContain('could not be loaded');

    click('Try again');

    expect(root.textContent).toContain('Sneha Iyer');
  });
});
