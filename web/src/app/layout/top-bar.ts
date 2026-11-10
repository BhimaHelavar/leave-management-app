import { BreakpointObserver } from '@angular/cdk/layout';
import { DOCUMENT } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButton } from '@angular/material/button';
import { MatMenu, MatMenuItem, MatMenuTrigger } from '@angular/material/menu';
import { MatToolbar } from '@angular/material/toolbar';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { map } from 'rxjs';
import { LOGIN_PATH } from '../auth/auth.interceptor';
import { AuthService } from '../auth/auth.service';
import { visibleItems } from './nav-items';

/** Phase 3a breakpoint: below 768 px the links collapse into one menu button. */
const NARROW_QUERY = '(max-width: 767.98px)';

/** Header: app name, role-based links and a user menu. Link visibility is a convenience only. */
@Component({
  selector: 'app-top-bar',
  imports: [
    RouterLink,
    RouterLinkActive,
    MatToolbar,
    MatButton,
    MatMenu,
    MatMenuItem,
    MatMenuTrigger,
  ],
  templateUrl: './top-bar.html',
  styleUrl: './top-bar.scss',
})
export class TopBar {
  private readonly auth = inject(AuthService);
  private readonly document = inject(DOCUMENT);
  private readonly router = inject(Router);

  protected readonly person = this.auth.person;
  protected readonly items = computed(() => visibleItems(this.auth.roles()));
  protected readonly narrow = toSignal(
    inject(BreakpointObserver)
      .observe(NARROW_QUERY)
      .pipe(map((state) => state.matches)),
    { initialValue: false },
  );

  protected async signOut(): Promise<void> {
    this.auth.signOut();
    await this.router.navigateByUrl(LOGIN_PATH);
  }

  /**
   * A plain "#main" link would navigate to the base URL, so move focus ourselves.
   * The element with id "main" is in the app shell template and has tabindex="-1".
   */
  protected skipToMain(event: Event): void {
    event.preventDefault();
    this.document.getElementById('main')?.focus();
  }
}
