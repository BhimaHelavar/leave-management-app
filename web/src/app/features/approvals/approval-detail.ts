import { HttpErrorResponse } from '@angular/common/http';
import { DecimalPipe } from '@angular/common';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable, Subscription } from 'rxjs';
import { NOT_FOUND_PATH } from '../../auth/role-guard';
import { ApprovalDetail, ApprovalsService, HalfDay } from './approvals.service';

/** Same limit as the server (spec FR-002). The server checks it again. */
export const MAX_COMMENT_LENGTH = 500;

/**
 * The request page (screen S-06), opened from the inbox or from a link: one request, with Approve and Reject.
 * The server says which buttons exist (`allowedActions`) and decides every rule; this page only shows the
 * answer. A request the manager may not see looks like one that does not exist: both open the not-found page.
 * The class is called ApprovalDetailPage because ApprovalDetail is the data type it shows.
 */
@Component({
  selector: 'app-approval-detail',
  imports: [DecimalPipe],
  templateUrl: './approval-detail.html',
  styleUrl: './approval-detail.scss',
})
export class ApprovalDetailPage {
  private readonly service = inject(ApprovalsService);
  private readonly router = inject(Router);
  private readonly requestId = Number(inject(ActivatedRoute).snapshot.paramMap.get('requestId'));

  protected readonly maxLength = MAX_COMMENT_LENGTH;
  protected readonly loading = signal(true);
  protected readonly loadFailed = signal(false);
  protected readonly detail = signal<ApprovalDetail | null>(null);
  protected readonly busy = signal(false);
  /** The server's refusal (409, 422) or a general failure of a decision. */
  protected readonly message = signal<string | null>(null);
  protected readonly rejecting = signal(false);
  protected readonly comment = signal('');
  protected readonly commentMissing = signal(false);

  protected readonly canApprove = computed(() =>
    (this.detail()?.allowedActions ?? []).includes('approve'),
  );
  protected readonly canReject = computed(() =>
    (this.detail()?.allowedActions ?? []).includes('reject'),
  );

  private current?: Subscription;

  constructor() {
    inject(DestroyRef).onDestroy(() => this.current?.unsubscribe());
    if (!Number.isInteger(this.requestId) || this.requestId < 1) {
      void this.router.navigateByUrl(NOT_FOUND_PATH);
      return;
    }
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.run(this.service.getApproval(this.requestId), () => this.loadFailed.set(true), true);
  }

  protected approve(): void {
    this.decide(this.service.approve(this.requestId, null));
  }

  protected startReject(): void {
    this.message.set(null);
    this.rejecting.set(true);
  }

  protected cancelReject(): void {
    this.rejecting.set(false);
    this.commentMissing.set(false);
  }

  protected setComment(value: string): void {
    this.comment.set(value);
    if (value.trim().length > 0) {
      this.commentMissing.set(false);
    }
  }

  protected sendRejection(): void {
    const text = this.comment().trim();
    if (text.length === 0) {
      // Nothing is sent: the server would refuse it with 422 anyway.
      this.commentMissing.set(true);
      return;
    }
    this.decide(this.service.reject(this.requestId, text));
  }

  protected dates(item: ApprovalDetail): string {
    return item.startDate === item.endDate
      ? item.startDate
      : `${item.startDate} to ${item.endDate}`;
  }

  protected halfDayLabel(halfDay: HalfDay): string | null {
    switch (halfDay) {
      case 'FirstHalf':
        return 'First half';
      case 'SecondHalf':
        return 'Second half';
      default:
        return null;
    }
  }

  /** Words, never colour only. The server computes both figures; the screen only phrases them. */
  protected overlapText(item: ApprovalDetail): string | null {
    const overlap = item.teamOverlap;
    if (overlap === null) {
      return null;
    }
    if (overlap.others.length === 0) {
      return 'No one else in your team is away on these days.';
    }

    const people = overlap.others.map((o) => `${o.name} (${o.dates.join(', ')})`).join('; ');
    return `Also away: ${people}. ${overlap.awayIfApproved} of ${overlap.teamSize} of your team would be away if you approve.`;
  }

  private decide(call: Observable<ApprovalDetail>): void {
    this.busy.set(true);
    this.message.set(null);
    this.current?.unsubscribe();
    this.current = call.subscribe({
      next: (result) => {
        this.detail.set(result);
        this.rejecting.set(false);
        this.busy.set(false);
      },
      error: (error: unknown) => {
        this.busy.set(false);
        const status = error instanceof HttpErrorResponse ? error.status : 0;
        if (status === 404) {
          void this.router.navigateByUrl(NOT_FOUND_PATH);
          return;
        }
        if ((status === 409 || status === 422) && error instanceof HttpErrorResponse) {
          const detail: unknown = error.error?.detail;
          this.message.set(
            typeof detail === 'string'
              ? detail
              : 'The decision was refused. Please check the request.',
          );
          if (status === 409) {
            // Someone else decided first: show the real state so no stale buttons remain.
            this.refresh();
          }
          return;
        }
        this.message.set('The decision could not be saved. Please try again.');
      },
    });
  }

  private refresh(): void {
    this.run(this.service.getApproval(this.requestId), () => undefined, false);
  }

  private run(call: Observable<ApprovalDetail>, onError: () => void, showLoading: boolean): void {
    this.current?.unsubscribe();
    this.current = call.subscribe({
      next: (result) => {
        this.detail.set(result);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        if (error instanceof HttpErrorResponse && error.status === 404) {
          void this.router.navigateByUrl(NOT_FOUND_PATH);
          return;
        }
        if (showLoading) {
          this.loading.set(false);
        }
        onError();
      },
    });
  }
}
