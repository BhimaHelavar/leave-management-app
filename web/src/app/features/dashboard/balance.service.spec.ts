import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_URL } from '../../api-config';
import { BalanceService, MyBalances } from './balance.service';

const API = 'http://api.test';

function setup() {
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(),
      provideHttpClientTesting(),
      { provide: API_URL, useValue: API },
    ],
  });
  return { service: TestBed.inject(BalanceService), http: TestBed.inject(HttpTestingController) };
}

describe('BalanceService', () => {
  it('gets my balances from the API', () => {
    const { service, http } = setup();
    const body: MyBalances = {
      leaveYear: 2026,
      balances: [
        {
          leaveTypeId: 1,
          code: 'ANNUAL',
          name: 'Annual leave',
          hasBalance: true,
          accrued: 10,
          used: 2,
          pending: 3,
          available: 5,
          carryForwardDays: null,
          carryForwardExpiresOn: null,
        },
      ],
    };
    let result: MyBalances | undefined;

    service.getMyBalances().subscribe((value) => (result = value));
    const request = http.expectOne(`${API}/api/me/balances`);
    expect(request.request.method).toBe('GET');
    request.flush(body);

    expect(result).toEqual(body);
    http.verify();
  });

  it('passes an API error on to the caller', () => {
    const { service, http } = setup();
    let status: number | undefined;

    service.getMyBalances().subscribe({ error: (e) => (status = e.status) });
    http
      .expectOne(`${API}/api/me/balances`)
      .flush({ title: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    expect(status).toBe(401);
    http.verify();
  });
});
