import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { AuthService } from '../../auth/auth.service';
import { DevUser } from '../../auth/models';
import { Login } from './login';

const PEOPLE: DevUser[] = [
  { id: 1, fullName: 'Meera Rao', email: 'meera.rao@example.test', roles: ['Employee', 'Manager'] },
  { id: 2, fullName: 'Hema Shetty', email: 'hema.shetty@example.test', roles: ['Employee', 'HR'] },
];

async function setup(auth: Partial<AuthService>) {
  TestBed.configureTestingModule({
    imports: [Login],
    providers: [provideRouter([]), { provide: AuthService, useValue: auth }],
  });
  const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
  const fixture: ComponentFixture<Login> = TestBed.createComponent(Login);
  fixture.detectChanges();
  await fixture.whenStable();
  fixture.detectChanges();
  return { fixture, root: fixture.nativeElement as HTMLElement, navigate };
}

describe('Login', () => {
  it('lists the demo people with their roles', async () => {
    const { root } = await setup({ devUsers: () => Promise.resolve(PEOPLE) });

    const buttons = Array.from(root.querySelectorAll('ul.people button'));
    expect(buttons.map((b) => b.getAttribute('aria-label'))).toEqual([
      'Sign in as Meera Rao',
      'Sign in as Hema Shetty',
    ]);
    expect(root.textContent).toContain('Employee, Manager');
  });

  it('signs in the picked person and goes to the dashboard', async () => {
    const signInAs = vi.fn().mockResolvedValue(true);
    const { fixture, root, navigate } = await setup({
      devUsers: () => Promise.resolve(PEOPLE),
      signInAs,
    });

    (root.querySelector('ul.people button') as HTMLButtonElement).click();
    await fixture.whenStable();

    expect(signInAs).toHaveBeenCalledWith(1);
    expect(navigate).toHaveBeenCalledWith('/dashboard');
  });

  it('shows an error and stays on the page when sign-in is refused', async () => {
    const signInAs = vi.fn().mockResolvedValue(false);
    const { fixture, root, navigate } = await setup({
      devUsers: () => Promise.resolve(PEOPLE),
      signInAs,
    });

    (root.querySelector('ul.people button') as HTMLButtonElement).click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(navigate).not.toHaveBeenCalled();
    expect(root.querySelector('[role="alert"]')?.textContent).toContain('Sign-in failed');
  });

  it('shows "could not load people" with a retry button when the API is unreachable', async () => {
    const devUsers = vi.fn().mockRejectedValueOnce(new Error('down')).mockResolvedValueOnce(PEOPLE);
    const { fixture, root } = await setup({ devUsers });

    expect(root.querySelector('[role="alert"]')?.textContent).toContain('Could not load people');

    (root.querySelector('button') as HTMLButtonElement).click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(root.querySelectorAll('ul.people button').length).toBe(2);
  });
});
