import { DecimalPipe, SlicePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ReviewResult } from '../../core/models/api.models';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';
import { ExecutionStateService } from '../../core/services/execution-state.service';

interface AgentItemGroup {
  agent: string;
  items: string[];
}

@Component({
  selector: 'app-final-result',
  standalone: true,
  imports: [
    DecimalPipe, SlicePipe,
    MatCardModule, MatIconModule, MatProgressSpinnerModule,
    MatExpansionModule, MatDividerModule, MatButtonModule,
    StatusBadgeComponent,
  ],
  templateUrl: './final-result.component.html',
  styleUrl: './final-result.component.scss',
})
export class FinalResultComponent {
  protected readonly state = inject(ExecutionStateService);

  protected issuesByAgent(result: ReviewResult): AgentItemGroup[] {
    return this.groupByAgent(
      result.issues ?? [],
      item => (result.agentResults ?? [])
        .filter(ar => (ar.issues ?? []).includes(item))
        .map(ar => ar.agent)
    );
  }

  protected recommendationsByAgent(result: ReviewResult): AgentItemGroup[] {
    return this.groupByAgent(
      result.recommendations ?? [],
      item => (result.agentResults ?? [])
        .filter(ar => (ar.recommendations ?? []).includes(item))
        .map(ar => ar.agent)
    );
  }

  private groupByAgent(items: string[], getAgents: (item: string) => string[]): AgentItemGroup[] {
    const groups = new Map<string, string[]>();

    for (const item of items) {
      const agents = getAgents(item);
      if (agents.length === 0) {
        const unattributed = groups.get('unattributed') ?? [];
        unattributed.push(item);
        groups.set('unattributed', unattributed);
        continue;
      }

      for (const agent of agents) {
        const byAgent = groups.get(agent) ?? [];
        byAgent.push(item);
        groups.set(agent, byAgent);
      }
    }

    return [...groups.entries()].map(([agent, groupedItems]) => ({
      agent,
      items: groupedItems
    }));
  }
}
