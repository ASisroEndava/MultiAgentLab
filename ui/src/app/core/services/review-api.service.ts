import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ExecutionLogEvent,
  ExecutionSummary,
  MockCaseSummary,
  ReviewRequest,
  ReviewResult,
  StartCaseResponse,
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class ReviewApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  getMockCases(): Observable<MockCaseSummary[]> {
    return this.http.get<MockCaseSummary[]>(`${this.base}/mock-cases`);
  }

  startMockCase(caseId: string): Observable<StartCaseResponse> {
    return this.http.post<StartCaseResponse>(
      `${this.base}/mock-cases/${caseId}/start`,
      {}
    );
  }

  reviewStory(request: ReviewRequest): Observable<ReviewResult> {
    return this.http.post<ReviewResult>(`${this.base}/review-story`, request);
  }

  getExecutionLog(executionId: string): Observable<ExecutionLogEvent[]> {
    return this.http.get<ExecutionLogEvent[]>(
      `${this.base}/executions/${executionId}/log`
    );
  }

  getExecutions(): Observable<ExecutionSummary[]> {
    return this.http.get<ExecutionSummary[]>(`${this.base}/executions`);
  }
}
