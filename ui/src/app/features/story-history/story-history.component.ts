import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { DatePipe, DecimalPipe, SlicePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ComparisonResult, ExecutionSummary } from '../../core/models/api.models';
import { ReviewApiService } from '../../core/services/review-api.service';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';
import { ComparisonPanelComponent } from '../comparison-panel/comparison-panel.component';

export interface StoryGroup {
  storyId: string;
  title: string;
  executions: ExecutionSummary[];
}

@Component({
  selector: 'app-story-history',
  standalone: true,
  imports: [
    DatePipe, DecimalPipe, SlicePipe,
    MatCardModule, MatIconModule, MatButtonModule, MatTooltipModule,
    MatExpansionModule, MatCheckboxModule, MatProgressSpinnerModule,
    StatusBadgeComponent, ComparisonPanelComponent,
  ],
  templateUrl: './story-history.component.html',
  styleUrl: './story-history.component.scss',
})
export class StoryHistoryComponent implements OnInit {
  private readonly api = inject(ReviewApiService);

  protected loading   = signal(true);
  protected error     = signal<string | null>(null);
  protected storyGroups = signal<StoryGroup[]>([]);

  protected selected        = signal<Set<string>>(new Set());
  protected comparing       = signal(false);
  protected comparisonResult = signal<ComparisonResult | null>(null);
  protected comparisonError  = signal<string | null>(null);

  protected readonly canCompare   = computed(() => this.selected().size === 2);
  protected readonly selectionCount = computed(() => this.selected().size);

  ngOnInit(): void {
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

  protected toggle(executionId: string): void {
    const s = new Set(this.selected());
    if (s.has(executionId)) {
      s.delete(executionId);
    } else {
      s.add(executionId);
    }
    this.selected.set(s);
    this.comparisonResult.set(null);
    this.comparisonError.set(null);
  }

  protected isSelected(id: string): boolean {
    return this.selected().has(id);
  }

  protected isDisabled(id: string): boolean {
    return !this.isSelected(id) && this.selected().size >= 2;
  }

  protected compare(): void {
    const [a, b] = [...this.selected()];
    this.comparing.set(true);
    this.comparisonResult.set(null);
    this.comparisonError.set(null);
    this.api.compareExecutions(a, b).subscribe({
      next: (result) => {
        this.comparisonResult.set(result);
        this.comparing.set(false);
      },
      error: (err) => {
        this.comparisonError.set(
          err?.error?.message ?? 'Comparison failed — executions may belong to different stories.'
        );
        this.comparing.set(false);
      },
    });
  }

  protected clearSelection(): void {
    this.selected.set(new Set());
    this.comparisonResult.set(null);
    this.comparisonError.set(null);
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
