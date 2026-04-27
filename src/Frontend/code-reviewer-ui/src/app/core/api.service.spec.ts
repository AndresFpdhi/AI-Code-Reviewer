import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApiService } from './api.service';
import { environment } from '../../environments/environment';

describe('ApiService', () => {
  let api: ApiService;
  let httpMock: HttpTestingController;
  const base = environment.apiBase;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ApiService, provideHttpClient(), provideHttpClientTesting()]
    });
    api = TestBed.inject(ApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('listReviews sends pagination as query params', () => {
    api.listReviews(2, 10).subscribe();
    const req = httpMock.expectOne(r => r.url === `${base}/api/reviews`);
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('10');
    req.flush({ page: 2, pageSize: 10, total: 0, items: [] });
  });

  it('listReviews defaults to page 1 with size 20', () => {
    api.listReviews().subscribe();
    const req = httpMock.expectOne(r => r.url === `${base}/api/reviews`);
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('20');
    req.flush({ page: 1, pageSize: 20, total: 0, items: [] });
  });

  it('getReview hits the per-id endpoint', () => {
    api.getReview(7).subscribe(r => expect(r.id).toBe(7));
    httpMock.expectOne(`${base}/api/reviews/7`).flush({ id: 7 });
  });
});
