import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ReviewListComponent } from './review-list.component';
import { ApiService } from '../../core/api.service';

class FakeApi {
  listReviews = jasmine.createSpy('listReviews');
}

describe('ReviewListComponent', () => {
  let fixture: ComponentFixture<ReviewListComponent>;
  let component: ReviewListComponent;
  let api: FakeApi;

  beforeEach(async () => {
    api = new FakeApi();
    await TestBed.configureTestingModule({
      imports: [ReviewListComponent],
      providers: [provideRouter([]), { provide: ApiService, useValue: api }]
    }).compileComponents();
    fixture = TestBed.createComponent(ReviewListComponent);
    component = fixture.componentInstance;
  });

  it('populates reviews on successful load', () => {
    api.listReviews.and.returnValue(of({
      page: 1, pageSize: 20, total: 1,
      items: [{ id: 1, repoOwner: 'o', repoName: 'r', prNumber: 1, prTitle: 't', prUrl: 'u', score: 9, commentCount: 0, createdAt: '2026-01-01' }]
    }));

    fixture.detectChanges();

    expect(component.loading()).toBeFalse();
    expect(component.error()).toBeNull();
    expect(component.reviews().length).toBe(1);
    expect(component.total()).toBe(1);
  });

  it('exposes an error message when the API fails', () => {
    api.listReviews.and.returnValue(throwError(() => new Error('boom')));

    fixture.detectChanges();

    expect(component.loading()).toBeFalse();
    expect(component.error()).toContain('Could not load');
    expect(component.reviews().length).toBe(0);
  });

  it('scoreClass returns expected bucket', () => {
    expect(component.scoreClass(9)).toBe('score-high');
    expect(component.scoreClass(6)).toBe('score-mid');
    expect(component.scoreClass(2)).toBe('score-low');
  });
});
