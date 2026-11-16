import { DecimalPipe } from '@angular/common';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import { ApprovalItem, ApprovalsService, HalfDay } from './approvals.service';

/** The manager inbox shows 20 requests a page. The server accepts up to 100. */
export const PAGE_SIZE = 20;

/**
 * The manager inbox (screen S-05): the pending requests of the manager's direct reports, soonest start
 * first. The server decides who the reports are and computes every figure; this screen only shows them.
 * A failed load shows an error, never an empty list, so nobody mistakes "could not load" for "nothing waiting".
 * There are no Approve or Reject buttons yet: deciding is the next story.
 */
@Component({
  selector: 'app-approvals',
  imports: [DecimalPipe],
  templateUrl: './approvals.html',
  styleUrl: './approvals.scss',
})
export class Approvals {
  private readonly service = inject(ApprovalsService);

  protected readonly loading = signal(true);
  protected readonly error = signal(false);
  protected readonly items = signal<ApprovalItem[]>([]);
  protected readonly page = signal(1);
  protected readonly totalCount = signal(0);

  protected readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.totalCount() / PAGE_SIZE)),
  );
  protected readonly hasPrevious = computed(() => this.page() > 1);
  protected readonly hasNext = computed(() => this.page() < this.totalPages());

  private current?: Subscription;

  constructor() {
    inject(DestroyRef).onDestroy(() => this.current?.unsubscribe());
    this.load(1);
  }

  protected load(page: number = this.page()): void {
    // Cancel the previous call, so a late response of an older request can never overwrite a newer one.
    this.current?.unsubscribe();
    this.loading.set(true);
    this.error.set(false);
    this.current = this.service.getApprovals(page, PAGE_SIZE).subscribe({
      next: (inbox) => {
        // The page vanished (requests were decided meanwhile): reload the last valid page, not "nothing waiting".
        const lastPage = Math.ceil(inbox.totalCount / PAGE_SIZE);
        if (inbox.items.length === 0 && inbox.totalCount > 0 && inbox.page > lastPage) {
          this.load(lastPage);
          return;
        }
        this.items.set(inbox.items);
        this.page.set(inbox.page);
        this.totalCount.set(inbox.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.items.set([]);
        this.error.set(true);
        this.loading.set(false);
      },
    });
  }

  protected previous(): void {
    if (this.hasPrevious()) {
      this.load(this.page() - 1);
    }
  }

  protected next(): void {
    if (this.hasNext()) {
      this.load(this.page() + 1);
    }
  }

  protected dates(item: ApprovalItem): string {
    return item.startDate === item.endDate
      ? item.startDate
      : `${item.startDate} to ${item.endDate}`;
  }

  /** Words, never colour only. The server computes both figures; the screen only phrases them. */
  protected overlapText(item: ApprovalItem): string {
    const overlap = item.teamOverlap;
    if (overlap.others.length === 0) {
      return 'No one else in your team is away on these days.';
    }

    const people = overlap.others.map((o) => `${o.name} (${o.dates.join(', ')})`).join('; ');
    return `Also away: ${people}. ${overlap.awayIfApproved} of ${overlap.teamSize} of your team would be away if you approve.`;
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
}
