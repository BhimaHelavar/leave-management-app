import { Component, inject, signal } from '@angular/core';
import { BalanceService, LeaveTypeBalance } from './balance.service';

/**
 * The dashboard: one card per leave type with accrued, used, pending and available days.
 * The server computes every figure; this screen only shows them (FR-010). A failed load shows an
 * error, never zeros, so nobody mistakes "could not load" for "no leave left".
 */
@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard {
  private readonly service = inject(BalanceService);

  protected readonly loading = signal(true);
  protected readonly error = signal(false);
  protected readonly leaveYear = signal<number | null>(null);
  protected readonly balances = signal<LeaveTypeBalance[]>([]);

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(false);
    this.service.getMyBalances().subscribe({
      next: (response) => {
        this.leaveYear.set(response.leaveYear);
        this.balances.set(response.balances);
        this.loading.set(false);
      },
      error: () => {
        this.balances.set([]);
        this.error.set(true);
        this.loading.set(false);
      },
    });
  }
}
