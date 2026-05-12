import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { DatePipe, DecimalPipe, SlicePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { FormsModule } from '@angular/forms';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ExecutionSummary, ProvidersStatus, SemanticComparisonResult, SemanticCompareRequest } from '../../core/models/api.models';
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
    DatePipe, DecimalPipe, SlicePipe, FormsModule,
    MatCardModule, MatIconModule, MatButtonModule, MatTooltipModule,
    MatExpansionModule, MatCheckboxModule, MatProgressSpinnerModule,
    MatSelectModule, MatInputModule, MatFormFieldModule,
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
  protected comparisonResult = signal<SemanticComparisonResult | null>(null);
  protected comparisonError  = signal<string | null>(null);

  protected providerType       = signal<'ollama' | 'bedrock'>('ollama');
  protected model               = signal('qwen2.5:3b');
  protected endpoint            = signal('http://localhost:11434');
  protected ollamaModels        = signal<string[]>(['qwen2.5:3b']);
  protected ollamaModelsLoading = signal(false);
  protected readonly bedrockModels = [
    'us.anthropic.claude-3-5-haiku-20241022-v1:0',
    'us.anthropic.claude-3-5-sonnet-20241022-v2:0',
    'us.anthropic.claude-3-7-sonnet-20250219-v1:0',
    'us.amazon.nova-micro-v1:0',
    'us.amazon.nova-lite-v1:0',
    'us.amazon.nova-pro-v1:0',
    'us.meta.llama3-1-8b-instruct-v1:0',
    'us.meta.llama3-1-70b-instruct-v1:0',
    'us.meta.llama3-3-70b-instruct-v1:0',
    'us.mistral.mistral-large-2402-v1:0',
  ];
  protected readonly providersStatus = signal<ProvidersStatus>({ ollama: true, bedrock: false });

  protected get availableModels(): string[] {
    return this.providerType() === 'bedrock' ? this.bedrockModels : this.ollamaModels();
  }

  protected readonly canCompare    = computed(() => this.selected().size === 2);
  protected readonly selectionCount = computed(() => this.selected().size);

  ngOnInit(): void {
    this.load();
    this.api.getProvidersStatus().subscribe({
      next: (status) => this.providersStatus.set(status),
    });
    this.fetchOllamaModels();
  }

  protected onProviderChange(): void {
    if (this.providerType() === 'ollama') this.fetchOllamaModels();
    const models = this.availableModels;
    if (!models.includes(this.model())) {
      this.model.set(models[0] ?? '');
    }
  }

  protected onEndpointBlur(): void {
    if (this.providerType() === 'ollama') this.fetchOllamaModels();
  }

  private fetchOllamaModels(): void {
    this.ollamaModelsLoading.set(true);
    this.api.getOllamaModels(this.endpoint()).subscribe({
      next: (models) => {
        this.ollamaModels.set(models.length > 0 ? models : ['qwen2.5:3b']);
        if (!this.ollamaModels().includes(this.model())) {
          this.model.set(this.ollamaModels()[0]);
        }
        this.ollamaModelsLoading.set(false);
      },
      error: () => this.ollamaModelsLoading.set(false),
    });
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
    const req: SemanticCompareRequest = {
      a, b,
      provider: {
        type: this.providerType(),
        model: this.model(),
        endpoint: this.providerType() === 'ollama' ? this.endpoint() : undefined,
        temperature: 0.1,
      },
    };
    this.api.semanticCompareExecutions(req).subscribe({
      next: (result) => {
        this.comparisonResult.set(result);
        this.comparing.set(false);
      },
      error: (err) => {
        this.comparisonError.set(
          err?.error?.message ?? 'Comparison failed — check provider settings or ensure executions belong to the same story.'
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
