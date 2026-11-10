import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { API_URL } from '../api-config';
import { DevUser, Person, Role, TokenResponse, normalizeRoles } from './models';

interface Session {
  token: string;
  person: Person;
}

/**
 * The one place the app asks "who is signed in?".
 *
 * The access token is kept in a signal, which lives in memory only, so a page reload signs the
 * person out (spec 4c, R8). It is never written to localStorage, sessionStorage or a cookie.
 * Hiding a link based on these roles is a convenience only; the server decides (security rule 1).
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = inject(API_URL);
  private readonly session = signal<Session | null>(null);

  readonly person = computed(() => this.session()?.person ?? null);
  readonly roles = computed<readonly Role[]>(() => this.session()?.person.roles ?? []);
  readonly isSignedIn = computed(() => this.session() !== null);

  /** Read by the HTTP interceptor only. Never log or display it. */
  accessToken(): string | null {
    return this.session()?.token ?? null;
  }

  /** The demo people for the sign-in screen. Rejects if the API is unreachable or not in Dev mode. */
  devUsers(): Promise<DevUser[]> {
    return firstValueFrom(this.http.get<DevUser[]>(`${this.apiUrl}/api/dev/users`));
  }

  /** Asks the API for a token for this person. Returns false when the API refuses. */
  async signInAs(employeeId: number): Promise<boolean> {
    try {
      const response = await firstValueFrom(
        this.http.post<TokenResponse>(`${this.apiUrl}/api/dev/token`, { employeeId }),
      );
      this.session.set({
        token: response.accessToken,
        person: {
          id: String(response.user.id),
          name: response.user.name,
          roles: normalizeRoles(response.user.roles),
        },
      });
      return true;
    } catch {
      return false;
    }
  }

  signOut(): void {
    this.session.set(null);
  }
}
