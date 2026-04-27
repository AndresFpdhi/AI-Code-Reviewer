import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { ReviewSummary } from '../../core/models/review.model';

@Component({
  selector: 'app-review-list',
  standalone: true,
  imports: [CommonModule, RouterLink, DatePipe],
  templateUrl: './review-list.component.html',
  styleUrl: './review-list.component.css'
})
export class ReviewListComponent implements OnInit {
  private api = inject(ApiService);

  reviews = signal<ReviewSummary[]>([]);
  total = signal(0);
  loading = signal(true);
  error = signal<string | null>(null);

  ngOnInit() {
    this.api.listReviews().subscribe({
      next: page => {
        this.reviews.set(page.items);
        this.total.set(page.total);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load reviews. Is the API running?');
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
