import { Component, inject, OnInit, signal } from '@angular/core';
import { DatePipe, DecimalPipe, SlicePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ExecutionSummary, ReviewResult } from '../../core/models/api.models';
import { ReviewApiService } from '../../core/services/review-api.service';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';

interface StoryGroup {
  storyId: string;
  title: string;
  executions: ExecutionSummary[];
}

@Component({
  selector: 'app-execution-history',
  standalone: true,
  imports: [
    DatePipe, DecimalPipe, SlicePipe,
    MatCardModule, MatExpansionModule, MatIconModule, MatButtonModule,
    MatProgressSpinnerModule, MatTooltipModule, MatDividerModule, MatChipsModule,
    StatusBadgeComponent,
  ],
  templateUrl: './execution-history.component.html',
  styleUrl: './execution-history.component.scss',
})
export class ExecutionHistoryComponent implements OnInit {
  private readonly api = inject(ReviewApiService);

  protected loading = signal(true);
  protected error = signal<string | null>(null);
  protected storyGroups = signal<StoryGroup[]>([]);

  protected selectedId = signal<string | null>(null);
  protected loadingResult = signal(false);
  protected selectedResult = signal<ReviewResult | null>(null);
  protected resultError = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  refresh(): void {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.getExecutions().subscribe({
      next: (summaries) => {
        this.storyGroups.set(this.groupByStory(summaries));
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load executions — is the backend running?');
        this.loading.set(false);
      },
    });
  }

  protected selectExecution(id: string): void {
    if (this.selectedId() === id) {
      this.selectedId.set(null);
      this.selectedResult.set(null);
      return;
    }
    this.selectedId.set(id);
    this.selectedResult.set(null);
    this.resultError.set(null);
    this.loadingResult.set(true);
    this.api.getExecutionResult(id).subscribe({
      next: (r) => { this.selectedResult.set(r); this.loadingResult.set(false); },
      error: () => { this.resultError.set('Could not load result.'); this.loadingResult.set(false); },
    });
  }

  private groupByStory(summaries: ExecutionSummary[]): StoryGroup[] {
    const map = new Map<string, ExecutionSummary[]>();
    for (const s of summaries) {
      const key = s.storyId ?? '(unknown)';
      if (!map.has(key)) map.set(key, []);
      map.get(key)!.push(s);
    }
    return [...map.entries()]
      .map(([storyId, executions]) => ({
        storyId,
        title: executions.find(e => e.title)?.title ?? storyId,
        executions: [...executions].sort((a, b) => b.timestamp.localeCompare(a.timestamp)),
      }))
      .sort((a, b) => {
        const latestA = a.executions[0]?.timestamp ?? '';
        const latestB = b.executions[0]?.timestamp ?? '';
        return latestB.localeCompare(latestA);
      });
  }
}
