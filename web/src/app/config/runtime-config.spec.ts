import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_URL } from '../api-config';
import { RuntimeConfigService, parseRuntimeConfig } from './runtime-config';

describe('parseRuntimeConfig', () => {
  it('accepts an absolute http URL and defaults devStub to false', () => {
    expect(parseRuntimeConfig({ apiUrl: 'https://api.example.org' })).toEqual({
      apiUrl: 'https://api.example.org',
      devStub: false,
    });
  });

  it('keeps devStub only when it is exactly true', () => {
    expect(parseRuntimeConfig({ apiUrl: 'http://localhost:5000', devStub: true })?.devStub).toBe(true);
    expect(parseRuntimeConfig({ apiUrl: 'http://localhost:5000', devStub: 'yes' })?.devStub).toBe(false);
  });

  it.each([
    ['null', null],
    ['a string', 'http://localhost:5000'],
    ['missing apiUrl', {}],
    ['empty apiUrl', { apiUrl: '' }],
    ['relative apiUrl', { apiUrl: '/api' }],
    ['trailing slash', { apiUrl: 'http://localhost:5000/' }],
    ['non-http scheme', { apiUrl: 'ftp://example.org' }],
    ['non-string apiUrl', { apiUrl: 5000 }],
  ])('rejects %s', (_name, raw) => {
    expect(parseRuntimeConfig(raw)).toBeNull();
  });
});

describe('RuntimeConfigService', () => {
  let service: RuntimeConfigService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(RuntimeConfigService);
    http = TestBed.inject(HttpTestingController);
  });

  it('starts in the loading state', () => {
    expect(service.state()).toBe('loading');
  });

  it('becomes ready and feeds API_URL when the file is valid', async () => {
    const loading = service.load();
    http.expectOne('config.json').flush({ apiUrl: 'http://localhost:5000', devStub: true });
    await loading;

    expect(service.state()).toBe('ready');
    expect(service.config.devStub).toBe(true);
    expect(TestBed.inject(API_URL)).toBe('http://localhost:5000');
  });

  it('fails when the file is missing', async () => {
    const loading = service.load();
    http.expectOne('config.json').flush('Not found', { status: 404, statusText: 'Not Found' });
    await loading;

    expect(service.state()).toBe('failed');
    expect(() => service.config).toThrow();
  });

  it('fails when the file holds an invalid apiUrl', async () => {
    const loading = service.load();
    http.expectOne('config.json').flush({ apiUrl: 'not a url' });
    await loading;

    expect(service.state()).toBe('failed');
  });
});
