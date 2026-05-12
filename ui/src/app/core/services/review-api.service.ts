import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ComparisonResult,
  ExecutionLogEvent,
  ExecutionSummary,
  MockCaseSummary,
  ProvidersStatus,
  ReviewRequest,
  ReviewResult,
  SemanticCompareRequest,
  SemanticComparisonResult,
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

  startReviewStory(request: ReviewRequest): Observable<StartCaseResponse> {
    return this.http.post<StartCaseResponse>(`${this.base}/review-story/start`, request);
  }

  getExecutionLog(executionId: string): Observable<ExecutionLogEvent[]> {
    return this.http.get<ExecutionLogEvent[]>(
      `${this.base}/executions/${executionId}/log`
    );
  }

  getExecutions(): Observable<ExecutionSummary[]> {
    return this.http.get<ExecutionSummary[]>(`${this.base}/executions`);
  }

  getOllamaModels(endpoint: string): Observable<string[]> {
    return this.http.get<string[]>(
      `${this.base}/providers/ollama/models`,
      { params: { endpoint } }
    );
  }

  getProvidersStatus(): Observable<ProvidersStatus> {
    return this.http.get<ProvidersStatus>(`${this.base}/providers/status`);
  }

  compareExecutions(a: string, b: string): Observable<ComparisonResult> {
    return this.http.get<ComparisonResult>(`${this.base}/executions/compare`, {
      params: { a, b },
    });
  }

  getExecutionResult(executionId: string): Observable<ReviewResult> {
    return this.http.get<ReviewResult>(`${this.base}/executions/${executionId}/result`);
  }

  semanticCompareExecutions(req: SemanticCompareRequest): Observable<SemanticComparisonResult> {
    return this.http.post<SemanticComparisonResult>(`${this.base}/executions/compare/semantic`, req);
  }
}
