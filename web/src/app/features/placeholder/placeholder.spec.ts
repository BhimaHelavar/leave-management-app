import { TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { Placeholder } from './placeholder';

function render(data: Record<string, unknown>): HTMLElement {
  TestBed.configureTestingModule({
    providers: [{ provide: ActivatedRoute, useValue: { data: of(data) } }],
  });
  const fixture = TestBed.createComponent(Placeholder);
  fixture.detectChanges();
  return fixture.nativeElement as HTMLElement;
}

describe('Placeholder', () => {
  it('shows the section name from the route and a coming soon message', () => {
    const root = render({ title: 'Approvals' });

    expect(root.querySelector('h1')?.textContent).toBe('Approvals');
    expect(root.textContent).toContain('Coming soon');
  });

  it('falls back to a generic heading when the route has no title', () => {
    const root = render({});

    expect(root.querySelector('h1')?.textContent).toBe('This section');
  });

  it('ignores a title that is not text', () => {
    const root = render({ title: 42 });

    expect(root.querySelector('h1')?.textContent).toBe('This section');
  });
});
