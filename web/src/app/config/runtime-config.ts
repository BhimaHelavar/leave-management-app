import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface RuntimeConfig {
  /** Absolute http(s) URL of the API, without a trailing slash. */
  apiUrl: string;
  /** True only in the local file; never in a deployed build. */
  devStub: boolean;
}

export type ConfigState = 'loading' | 'ready' | 'failed';

/**
 * Checks the untyped JSON from `config.json`. Returns null for anything that is not
 * a usable configuration, so the caller shows the error page instead of a half-working app.
 */
export function parseRuntimeConfig(raw: unknown): RuntimeConfig | null {
  if (typeof raw !== 'object' || raw === null) {
    return null;
  }
  const { apiUrl, devStub } = raw as Record<string, unknown>;
  if (typeof apiUrl !== 'string' || !isAbsoluteHttpUrl(apiUrl)) {
    return null;
  }
  return { apiUrl, devStub: devStub === true };
}

function isAbsoluteHttpUrl(value: string): boolean {
  if (value.endsWith('/')) {
    return false;
  }
  try {
    const url = new URL(value);
    return url.protocol === 'http:' || url.protocol === 'https:';
  } catch {
    return false;
  }
}

@Injectable({ providedIn: 'root' })
export class RuntimeConfigService {
  private readonly http = inject(HttpClient);
  private readonly loaded = signal<RuntimeConfig | null>(null);
  private readonly failed = signal(false);

  readonly state = computed<ConfigState>(() =>
    this.loaded() ? 'ready' : this.failed() ? 'failed' : 'loading',
  );

  /** Throws if read before a successful load; callers check `state()` first. */
  get config(): RuntimeConfig {
    const value = this.loaded();
    if (!value) {
      throw new Error('Runtime configuration has not been loaded.');
    }
    return value;
  }

  /**
   * Loads config.json once, before the first render. It never rejects: a failure sets the
   * state to 'failed' so the app can show a clear message instead of a blank page.
   */
  async load(): Promise<void> {
    try {
      const raw = await firstValueFrom(this.http.get<unknown>('config.json'));
      const parsed = parseRuntimeConfig(raw);
      if (parsed) {
        this.loaded.set(parsed);
        return;
      }
    } catch {
      // Falls through to the failed state. Details are not logged: the file holds no secret,
      // but the message would add nothing the error page does not already say.
    }
    this.failed.set(true);
  }
}
