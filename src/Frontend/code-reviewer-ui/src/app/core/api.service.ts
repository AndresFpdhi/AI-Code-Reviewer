import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ReviewDetail, ReviewPage } from './models/review.model';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);
  private base = environment.apiBase;

  listReviews(page = 1, pageSize = 20): Observable<ReviewPage> {
    return this.http.get<ReviewPage>(`${this.base}/api/reviews`, {
      params: { page, pageSize }
    });
  }

  getReview(id: number): Observable<ReviewDetail> {
    return this.http.get<ReviewDetail>(`${this.base}/api/reviews/${id}`);
  }
}
