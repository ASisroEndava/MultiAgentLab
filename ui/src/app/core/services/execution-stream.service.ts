import { inject, Injectable, signal } from '@angular/core';
import { Observable, Subject, interval, of, throwError, timer } from 'rxjs';
import { catchError, startWith, switchMap, takeUntil } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ExecutionLogEvent, StreamStatus } from '../models/api.models';
import { ReviewApiService } from './review-api.service';

const TERMINAL_EVENTS = new Set(['request_completed', 'execution_failed']);

@Injectable({ providedIn: 'root' })
export class ExecutionStreamService {
  private readonly api = inject(ReviewApiService);

  readonly status = signal<StreamStatus>('idle');

  stream(executionId: string): Observable<ExecutionLogEvent> {
    this.status.set('polling');

    return new Observable<ExecutionLogEvent>(observer => {
      let seenCount = 0;
      const stop$ = new Subject<void>();
      const timeout$ = timer(environment.maxPollDurationMs);

      const sub = interval(environment.pollIntervalMs).pipe(
        startWith(0),
        takeUntil(stop$),
        takeUntil(timeout$),
        switchMap(() =>
          this.api.getExecutionLog(executionId).pipe(
            catchError(err => {
              if (err?.status === 404) return of([] as ExecutionLogEvent[]);
              return throwError(() => err);
            })
          )
        )
      ).subscribe({
        next: (events) => {
          const newEvents = events.slice(seenCount);
          seenCount = events.length;

          for (const event of newEvents) {
            observer.next(event);
            if (TERMINAL_EVENTS.has(event.eventType)) {
              this.status.set(event.eventType === 'execution_failed' ? 'error' : 'complete');
              stop$.next();
              stop$.complete();
              observer.complete();
              return;
            }
          }
        },
        error: (err) => {
          this.status.set('error');
          observer.error(err);
        },
        complete: () => {
          if (this.status() === 'polling') {
            this.status.set('error');
            observer.error(new Error('Polling timeout: execution did not complete within the allowed duration.'));
          } else {
            observer.complete();
          }
        },
      });

      return () => {
        sub.unsubscribe();
        if (!stop$.closed) {
          stop$.next();
          stop$.complete();
        }
        if (this.status() === 'polling') this.status.set('idle');
      };
    });
  }

  reset(): void {
    this.status.set('idle');
  }
}
