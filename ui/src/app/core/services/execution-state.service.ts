import { computed, inject, Injectable, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import {
  AgentCard,
  ExecutionLogEvent,
  OverallState,
  ReviewResult,
} from '../models/api.models';
import { ExecutionStreamService } from './execution-stream.service';

@Injectable({ providedIn: 'root' })
export class ExecutionStateService {
  private readonly streamSvc = inject(ExecutionStreamService);

  readonly executionId = signal<string | null>(null);
  readonly overallState = signal<OverallState>('idle');
  readonly agentCards = signal<AgentCard[]>([]);
  readonly events = signal<ExecutionLogEvent[]>([]);
  readonly finalResult = signal<ReviewResult | null>(null);
  readonly totalMs = signal<number | null>(null);
  readonly streamStatus = this.streamSvc.status;

  readonly isExecuting = computed(() => this.overallState() === 'executing');
  readonly isComplete = computed(() => this.overallState() === 'complete');
  readonly hasResult = computed(() => this.finalResult() !== null);
  readonly statusColor = computed(() => {
    const s = this.finalResult()?.status;
    if (s === 'verde') return 'success';
    if (s === 'amarillo') return 'warning';
    if (s === 'rojo') return 'error';
    return 'default';
  });

  private subscription?: Subscription;

  begin(executionId: string): void {
    this.executionId.set(executionId);
    this.overallState.set('executing');

    this.subscription = this.streamSvc.stream(executionId).subscribe({
      next: (event) => this.applyEvent(event),
      error: () => this.overallState.set('error'),
      complete: () => {
        if (this.overallState() !== 'complete') {
          this.overallState.set('complete');
        }
      },
    });
  }

  setDirectResult(result: ReviewResult): void {
    this.executionId.set(result.executionId);
    this.finalResult.set(result);
    const allAgents: AgentCard[] = [
      ...result.invokedAgents.map(name => ({
        name,
        state: 'completed' as const,
        score: result.agentResults.find(a => a.agent === name)?.score,
        issueCount: result.agentResults.find(a => a.agent === name)?.issues.length ?? 0,
      })),
      ...result.skippedAgents.map(s => ({
        name: s.agent,
        state: 'skipped' as const,
        skipReason: s.reason,
      })),
    ];
    this.agentCards.set(allAgents);
    this.overallState.set('complete');
  }

  reset(): void {
    this.subscription?.unsubscribe();
    this.subscription = undefined;
    this.streamSvc.reset();
    this.executionId.set(null);
    this.overallState.set('idle');
    this.agentCards.set([]);
    this.events.set([]);
    this.finalResult.set(null);
    this.totalMs.set(null);
  }

  private applyEvent(event: ExecutionLogEvent): void {
    this.events.update(prev => [...prev, event]);
    const data = event.data as Record<string, unknown>;

    switch (event.eventType) {
      case 'selected_agents': {
        const invoked = (data['invoked'] as string[]) ?? [];
        const skipped = (data['skipped'] as { agent: string; reason: string }[]) ?? [];
        this.agentCards.set([
          ...invoked.map(name => ({ name, state: 'selected' as const })),
          ...skipped.map(s => ({ name: s.agent, state: 'skipped' as const, skipReason: s.reason })),
        ]);
        break;
      }
      case 'agent_started':
        this.agentCards.update(cards =>
          cards.map(c => c.name === data['agent'] ? { ...c, state: 'executing' as const } : c)
        );
        break;
      case 'agent_completed':
        this.agentCards.update(cards =>
          cards.map(c => c.name === data['agent']
            ? { ...c, state: 'completed' as const, score: data['score'] as number, issueCount: (data['issues'] as unknown[])?.length ?? 0 }
            : c)
        );
        break;
      case 'agent_failed':
        this.agentCards.update(cards =>
          cards.map(c => c.name === data['agent']
            ? { ...c, state: 'failed' as const, error: data['error'] as string }
            : c)
        );
        break;
      case 'final_result_generated':
        this.finalResult.set(event.data as unknown as ReviewResult);
        break;
      case 'execution_failed':
        this.overallState.set('error');
        break;
      case 'request_completed':
        this.totalMs.set(data['totalMs'] as number ?? null);
        this.overallState.set('complete');
        break;
    }
  }
}
