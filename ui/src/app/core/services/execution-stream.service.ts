import { inject, Injectable, signal } from '@angular/core';
import { Observable, interval } from 'rxjs';
import { startWith, switchMap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ExecutionLogEvent, StreamStatus } from '../models/api.models';
import { ReviewApiService } from './review-api.service';

@Injectable({ providedIn: 'root' })
export class ExecutionStreamService {
  private readonly api = inject(ReviewApiService);

  readonly status = signal<StreamStatus>('idle');

  stream(executionId: string): Observable<ExecutionLogEvent> {
    this.status.set('polling');

    return new Observable<ExecutionLogEvent>(observer => {
      let seenCount = 0;
      let done = false;

      const sub = interval(environment.pollIntervalMs).pipe(
        startWith(0),
        switchMap(() => this.api.getExecutionLog(executionId))
      ).subscribe({
        next: (events) => {
          if (done) return;
          const newEvents = events.slice(seenCount);
          seenCount = events.length;

          for (const event of newEvents) {
            observer.next(event);
            if (event.eventType === 'request_completed') {
              done = true;
              this.status.set('complete');
              observer.complete();
              return;
            }
          }
        },
        error: (err) => {
          this.status.set('error');
          observer.error(err);
        },
      });

      return () => {
        sub.unsubscribe();
        if (!done) this.status.set('idle');
      };
    });
  }

  reset(): void {
    this.status.set('idle');
  }
}
