import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ReviewDetailComponent } from './review-detail.component';
import { ApiService } from '../../core/api.service';

class FakeApi {
  getReview = jasmine.createSpy('getReview');
}

const fakeRoute = (id: string) => ({
  snapshot: { paramMap: { get: (_: string) => id } }
}) as unknown as ActivatedRoute;

describe('ReviewDetailComponent', () => {
  let fixture: ComponentFixture<ReviewDetailComponent>;
  let component: ReviewDetailComponent;
  let api: FakeApi;

  async function build(id: string) {
    api = new FakeApi();
    await TestBed.configureTestingModule({
      imports: [ReviewDetailComponent],
      providers: [
        provideRouter([]),
        { provide: ApiService, useValue: api },
        { provide: ActivatedRoute, useValue: fakeRoute(id) }
      ]
    }).compileComponents();
    fixture = TestBed.createComponent(ReviewDetailComponent);
    component = fixture.componentInstance;
  }

  it('parses inline comments out of rawJson', async () => {
    await build('5');
    api.getReview.and.returnValue(of({
      id: 5, repoOwner: 'o', repoName: 'r', prNumber: 5, prTitle: 't', prUrl: 'u',
      score: 7, commentCount: 1, createdAt: '2026-01-01', headSha: 'sha', summary: 's',
      rawJson: JSON.stringify({ summary: 's', score: 7, comments: [{ path: 'a.ts', line: 3, body: 'fix' }] })
    }));

    fixture.detectChanges();

    expect(component.review()?.id).toBe(5);
    expect(component.comments().length).toBe(1);
    expect(component.comments()[0].body).toBe('fix');
  });

  it('falls back to empty comments when rawJson is malformed', async () => {
    await build('1');
    api.getReview.and.returnValue(of({
      id: 1, repoOwner: 'o', repoName: 'r', prNumber: 1, prTitle: 't', prUrl: 'u',
      score: 5, commentCount: 0, createdAt: '2026-01-01', headSha: 'sha', summary: 's',
      rawJson: 'not-json'
    }));

    fixture.detectChanges();

    expect(component.comments()).toEqual([]);
  });

  it('exposes an error when API fails', async () => {
    await build('99');
    api.getReview.and.returnValue(throwError(() => new Error('nope')));

    fixture.detectChanges();

    expect(component.error()).toContain('Could not load');
    expect(component.review()).toBeNull();
  });
});
