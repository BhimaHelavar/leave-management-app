import { Component, inject, signal } from '@angular/core';
import { MatButton } from '@angular/material/button';
import { Router } from '@angular/router';
import { AuthService } from '../../auth/auth.service';
import { DevUser } from '../../auth/models';

type LoadState = 'loading' | 'ready' | 'failed';

/** Development sign-in: pick a demo person. The real sign-in (Entra ID) replaces this in 4g. */
@Component({
  selector: 'app-login',
  imports: [MatButton],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly state = signal<LoadState>('loading');
  protected readonly people = signal<DevUser[]>([]);
  protected readonly signInFailed = signal(false);
  protected readonly busy = signal(false);

  constructor() {
    void this.load();
  }

  protected async load(): Promise<void> {
    this.state.set('loading');
    try {
      this.people.set(await this.auth.devUsers());
      this.state.set('ready');
    } catch {
      this.state.set('failed');
    }
  }

  protected async pick(person: DevUser): Promise<void> {
    this.busy.set(true);
    this.signInFailed.set(false);
    const ok = await this.auth.signInAs(person.id);
    this.busy.set(false);
    if (ok) {
      await this.router.navigateByUrl('/dashboard');
    } else {
      this.signInFailed.set(true);
    }
  }
}
