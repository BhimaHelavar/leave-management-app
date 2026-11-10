import { InjectionToken, inject } from '@angular/core';
import { RuntimeConfigService } from './config/runtime-config';

/**
 * Base URL of the LMS API as seen from the browser. It comes from `config.json`
 * (loaded before the first render), so one build runs locally and on Azure.
 */
export const API_URL = new InjectionToken<string>('API_URL', {
  factory: () => inject(RuntimeConfigService).config.apiUrl,
});
