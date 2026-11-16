import { DEFAULT_RETURN_URL, safeReturnUrl } from './return-url';

describe('safeReturnUrl', () => {
  it.each(['/approvals/42', '/dashboard', '/approvals/42?x=1'])('keeps %s', (value) => {
    expect(safeReturnUrl(value)).toBe(value);
  });

  it.each([
    '//example.com',
    'https://example.com',
    '/\\evil',
    '',
    'javascript:alert(1)',
    'approvals/42',
    '/a b',
    '/login',
    '/login?returnUrl=/x',
    null,
    undefined,
  ])('sends %s to the dashboard', (value) => {
    expect(safeReturnUrl(value)).toBe(DEFAULT_RETURN_URL);
  });
});
