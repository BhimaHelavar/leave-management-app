import { TestBed } from '@angular/core/testing';
import { ConfigError } from './config-error';

describe('ConfigError', () => {
  it('tells the person the configuration could not be loaded, as an alert', () => {
    const fixture = TestBed.createComponent(ConfigError);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.textContent).toContain('configuration could not be loaded');
    expect(root.querySelector('[role="alert"]')).not.toBeNull();
  });
});
