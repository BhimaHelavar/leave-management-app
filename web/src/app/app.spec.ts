import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_URL } from './api-config';
import { App } from './app';

describe('App', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  it('shows the API status when the health call succeeds', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    http.expectOne(`${API_URL}/health`).flush({
      status: 'Healthy',
      authMode: 'Dev',
      database: 'Connected',
      environment: 'Development',
      timeUtc: '2026-10-22T12:00:00Z',
    });
    await fixture.whenStable();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('API is Healthy');
    expect(text).toContain('Dev');
    expect(text).toContain('Connected');
  });

  it('shows an error message when the health call fails', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    http.expectOne(`${API_URL}/health`).error(new ProgressEvent('error'));
    await fixture.whenStable();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('could not be reached');
  });
});
