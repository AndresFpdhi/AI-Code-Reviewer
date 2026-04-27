import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { ReviewComment, ReviewDetail } from '../../core/models/review.model';

@Component({
  selector: 'app-review-detail',
  imports: [CommonModule, RouterLink, DatePipe],
  templateUrl: './review-detail.component.html',
  styleUrl: './review-detail.component.css'
})
export class ReviewDetailComponent implements OnInit {
  private api = inject(ApiService);
  private route = inject(ActivatedRoute);

  review = signal<ReviewDetail | null>(null);
  comments = signal<ReviewComment[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  ngOnInit() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.api.getReview(id).subscribe({
      next: r => {
        this.review.set(r);
        try {
          const parsed = JSON.parse(r.rawJson);
          this.comments.set(parsed.comments ?? []);
        } catch {
          this.comments.set([]);
        }
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load review.');
        this.loading.set(false);
      }
    });
  }

  scoreClass(score: number): string {
    if (score >= 8) return 'score-high';
    if (score >= 5) return 'score-mid';
    return 'score-low';
  }
}
