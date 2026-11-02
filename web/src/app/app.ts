import { Component, OnInit, inject, signal } from '@angular/core';
import { Health, HealthService } from './health.service';

type ApiState =
  | { kind: 'loading' }
  | { kind: 'ok'; health: Health }
  | { kind: 'error' };

@Component({
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App implements OnInit {
  private readonly healthService = inject(HealthService);

  protected readonly state = signal<ApiState>({ kind: 'loading' });

  ngOnInit(): void {
    this.check();
  }

  protected check(): void {
    this.state.set({ kind: 'loading' });
    this.healthService.check().subscribe({
      next: (health) => this.state.set({ kind: 'ok', health }),
      error: () => this.state.set({ kind: 'error' }),
    });
  }
}
