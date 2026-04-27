export interface ReviewSummary {
  id: number;
  repoOwner: string;
  repoName: string;
  prNumber: number;
  prTitle: string;
  prUrl: string;
  score: number;
  commentCount: number;
  createdAt: string;
}

export interface ReviewDetail extends ReviewSummary {
  headSha: string;
  summary: string;
  rawJson: string;
}

export interface ReviewPage {
  page: number;
  pageSize: number;
  total: number;
  items: ReviewSummary[];
}

export interface ReviewComment {
  path: string;
  line: number;
  body: string;
}
