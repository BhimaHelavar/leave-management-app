import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { BalanceService, LeaveTypeBalance } from '../dashboard/balance.service';
import {
  HalfDay,
  LeavePreview,
  LeaveRequestSaved,
  LeaveRequestService,
  RefusalProblem,
} from './leave-request.service';

const REASON_MAX_LENGTH = 500;

/**
 * What to say for each refusal code of contracts/leave-requests-api.md. The server sends the same wording as
 * `detail` and fills in the numbers of the leave type, so `detail` wins when it is there. This map is the
 * fallback, and the two entries with a number in the server text have no number here.
 */
const REFUSAL_MESSAGES: Record<string, string> = {
  'date.range': 'The end date cannot be before the start date.',
  'date.past': 'The start date cannot be in the past.',
  'year.cross': 'A request cannot cross two leave years. Submit one request for each year.',
  'days.none': 'The dates you chose have no working days. Weekends and holidays are not counted.',
  'halfday.invalid':
    'A half day must be a single working day, and this leave type must allow half days.',
  'type.unknown': 'This leave type is not available.',
  'notice.short': 'You gave less notice than this leave type needs.',
  'max.consecutive': 'This is longer than this leave type allows. Longer leave needs HR.',
  'leave.overlap':
    'You already have a pending or approved leave request on one or more of these days.',
  'balance.insufficient': 'You do not have enough balance for these days.',
  'idempotency.mismatch': 'This request key was already used for a different request.',
};

/** A fresh random key (36 characters: letters, digits and hyphens, which the API accepts). */
const newKey = () => crypto.randomUUID();

const valueOf = (event: Event) =>
  (event.target as HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement).value;

/**
 * Apply for leave. The server counts the working days, applies the policy rules and saves the request; this
 * screen only collects the choices and shows the answer. It never counts days itself (the server decides).
 */
@Component({
  selector: 'app-apply-leave',
  templateUrl: './apply-leave.html',
  styleUrl: './apply-leave.scss',
})
export class ApplyLeave {
  private readonly balances = inject(BalanceService);
  private readonly requests = inject(LeaveRequestService);

  protected readonly reasonMaxLength = REASON_MAX_LENGTH;

  protected readonly loadingTypes = signal(true);
  protected readonly typesError = signal(false);
  protected readonly leaveTypes = signal<LeaveTypeBalance[]>([]);

  protected readonly leaveTypeId = signal('');
  protected readonly startDate = signal('');
  protected readonly endDate = signal('');
  protected readonly halfDay = signal<HalfDay>('None');
  protected readonly reason = signal('');

  protected readonly sending = signal(false);
  protected readonly saved = signal<LeaveRequestSaved | null>(null);
  protected readonly errorMessage = signal<string | null>(null);

  // The count the server gives for the chosen dates. Information only: a submit recounts and that is saved.
  protected readonly preview = signal<LeavePreview | null>(null);
  protected readonly previewNote = signal<string | null>(null);
  protected readonly previewing = signal(false);

  /**
   * A half day is a single day, so the choice only appears when start and end are the same day. Whether the
   * type allows half days is decided by the server and explained in its refusal.
   */
  protected readonly canChooseHalfDay = computed(
    () => this.startDate() !== '' && this.startDate() === this.endDate(),
  );

  protected readonly canSubmit = computed(
    () =>
      !this.sending() &&
      this.leaveTypeId() !== '' &&
      this.startDate() !== '' &&
      this.endDate() !== '' &&
      this.reason().length <= REASON_MAX_LENGTH,
  );

  // A retry of the same form reuses the key, so a double click cannot save twice. Any change to the form is a
  // different request, so it gets a new key (the same key with another body would be refused).
  private key = newKey();

  // Every preview call gets a number. An answer that arrives after a newer call was made is stale and is dropped,
  // so a slow reply for old dates can never overwrite the count for the dates now on screen.
  private previewCall = 0;

  constructor() {
    this.loadTypes();
  }

  protected loadTypes(): void {
    this.loadingTypes.set(true);
    this.typesError.set(false);
    this.balances.getMyBalances().subscribe({
      next: (response) => {
        this.leaveTypes.set(response.balances);
        this.loadingTypes.set(false);
      },
      error: () => {
        this.leaveTypes.set([]);
        this.typesError.set(true);
        this.loadingTypes.set(false);
      },
    });
  }

  protected onLeaveType(event: Event): void {
    this.leaveTypeId.set(valueOf(event));
    this.formChanged();
    this.refreshPreview();
  }

  protected onStartDate(event: Event): void {
    this.startDate.set(valueOf(event));
    this.afterDatesChanged();
  }

  protected onEndDate(event: Event): void {
    this.endDate.set(valueOf(event));
    this.afterDatesChanged();
  }

  protected onHalfDay(event: Event): void {
    this.halfDay.set(valueOf(event) as HalfDay);
    this.formChanged();
    this.refreshPreview();
  }

  protected onReason(event: Event): void {
    this.reason.set(valueOf(event));
    this.formChanged();
  }

  protected submit(event: Event): void {
    event.preventDefault();
    if (!this.canSubmit()) {
      return;
    }

    this.sending.set(true);
    this.errorMessage.set(null);
    this.saved.set(null);

    const reason = this.reason().trim();
    this.requests
      .apply(
        {
          leaveTypeId: Number(this.leaveTypeId()),
          startDate: this.startDate(),
          endDate: this.endDate(),
          // A half day only means something for a single day; send None otherwise.
          halfDay: this.canChooseHalfDay() ? this.halfDay() : 'None',
          reason: reason === '' ? null : reason,
        },
        this.key,
      )
      .subscribe({
        next: (result) => {
          this.saved.set(result);
          this.sending.set(false);
          this.resetForm();
        },
        error: (error: HttpErrorResponse) => {
          this.errorMessage.set(this.messageFor(error));
          this.sending.set(false);
        },
      });
  }

  private afterDatesChanged(): void {
    // A half day needs a single day. Drop the choice when the dates no longer match.
    if (!this.canChooseHalfDay()) {
      this.halfDay.set('None');
    }

    this.formChanged();
    this.refreshPreview();
  }

  private refreshPreview(): void {
    const call = ++this.previewCall;
    this.preview.set(null);
    this.previewNote.set(null);

    const typeId = this.leaveTypeId();
    const start = this.startDate();
    const end = this.endDate();
    if (typeId === '' || start === '' || end === '') {
      this.previewing.set(false);
      return;
    }

    this.previewing.set(true);
    this.requests
      .preview({
        leaveTypeId: Number(typeId),
        startDate: start,
        endDate: end,
        halfDay: this.canChooseHalfDay() ? this.halfDay() : 'None',
      })
      .subscribe({
        next: (result) => {
          if (call !== this.previewCall) {
            return;
          }

          this.preview.set(result);
          this.previewing.set(false);
        },
        error: (error: HttpErrorResponse) => {
          if (call !== this.previewCall) {
            return;
          }

          this.previewNote.set(this.messageFor(error));
          this.previewing.set(false);
        },
      });
  }

  private formChanged(): void {
    this.key = newKey();
    this.errorMessage.set(null);
  }

  private resetForm(): void {
    this.leaveTypeId.set('');
    this.startDate.set('');
    this.endDate.set('');
    this.halfDay.set('None');
    this.reason.set('');
    this.key = newKey();

    // Forget the preview and any answer still on its way.
    this.previewCall++;
    this.preview.set(null);
    this.previewNote.set(null);
    this.previewing.set(false);
  }

  private messageFor(error: HttpErrorResponse): string {
    if (error.status === 0) {
      return 'The server could not be reached. Check your connection and try again.';
    }

    const problem = error.error as RefusalProblem | null;
    const known = problem?.code !== undefined ? REFUSAL_MESSAGES[problem.code] : undefined;
    if (known !== undefined) {
      return problem?.detail ?? known;
    }

    // A code this screen does not know shows the server's title. With no code at all (a 400), the detail says more.
    const fallback =
      problem?.code !== undefined ? problem.title : (problem?.detail ?? problem?.title);
    return fallback ?? 'Your request could not be saved. Please try again.';
  }
}
