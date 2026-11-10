import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { NotFound } from './not-found';

describe('NotFound', () => {
  it('shows a safe message and a link back to the dashboard', () => {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
    const fixture = TestBed.createComponent(NotFound);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const link = root.querySelector('a');
    expect(root.querySelector('h1')?.textContent).toBe('Page not found');
    expect(link?.textContent).toBe('Back to dashboard');
    expect(link?.getAttribute('href')).toBe('/dashboard');
  });

  it('does not reveal whether the page exists or which role it needs', () => {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
    const fixture = TestBed.createComponent(NotFound);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).not.toMatch(/Admin|Manager|HR|role/i);
  });
});
