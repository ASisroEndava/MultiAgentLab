import { Component, inject, OnInit, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MockCaseSummary, ProvidersStatus, ReviewRequest, ReviewResult } from '../../core/models/api.models';
import { ExecutionStateService } from '../../core/services/execution-state.service';
import { ReviewApiService } from '../../core/services/review-api.service';

export type SubmitEvent =
  | { type: 'async'; executionId: string }
  | { type: 'sync'; result: ReviewResult };

@Component({
  selector: 'app-history-input',
  standalone: true,
  imports: [
    FormsModule,
    MatCardModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, MatTooltipModule,
  ],
  templateUrl: './history-input.component.html',
  styleUrl: './history-input.component.scss',
})
export class HistoryInputComponent implements OnInit {
  private readonly api = inject(ReviewApiService);
  protected readonly state = inject(ExecutionStateService);

  readonly executionStarted = output<SubmitEvent>();

  protected mockCases = signal<MockCaseSummary[]>([]);
  protected selectedMockCase = signal<MockCaseSummary | null>(null);
  protected storyText = signal('');
  protected storyTitle = signal('');
  protected storyId = signal('');
  protected providerType = signal<'ollama' | 'bedrock'>('ollama');
  protected model = signal('qwen2.5:3b');
  protected endpoint = signal('http://localhost:11434');
  protected loggingLevel = signal<'basic' | 'standard' | 'full'>('standard');
  protected includePrompts = signal(false);
  protected submitting = signal(false);
  protected loadError = signal<string | null>(null);

  protected readonly ollamaModels = signal<string[]>(['qwen2.5:3b']);
  protected ollamaModelsLoading = signal(false);
  protected readonly bedrockModels = [
    'anthropic.claude-3-haiku-20240307-v1:0',
    'anthropic.claude-3-sonnet-20240229-v1:0',
    'anthropic.claude-3-5-sonnet-20241022-v2:0',
    'amazon.titan-text-express-v1',
    'amazon.titan-text-premier-v1:0',
    'meta.llama3-8b-instruct-v1:0',
    'meta.llama3-70b-instruct-v1:0',
    'mistral.mistral-7b-instruct-v0:2',
  ];
  protected readonly providersStatus = signal<ProvidersStatus>({ ollama: true, bedrock: false });

  ngOnInit(): void {
    this.api.getMockCases().subscribe({
      next: (cases) => this.mockCases.set(cases),
      error: () => this.loadError.set('Could not load mock cases — is the backend running?'),
    });
    this.api.getProvidersStatus().subscribe({
      next: (status) => this.providersStatus.set(status),
    });
    this.fetchOllamaModels();
  }

  protected get canSubmit(): boolean {
    const ready = !!this.selectedMockCase() || !!this.storyText();
    return ready && !this.submitting() && !this.state.isExecuting();
  }

  protected get availableModels(): string[] {
    return this.providerType() === 'bedrock' ? this.bedrockModels : this.ollamaModels();
  }

  protected onMockCaseSelect(mc: MockCaseSummary | null): void {
    this.selectedMockCase.set(mc);
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

  protected async submit(): Promise<void> {
    if (!this.canSubmit) return;
    this.submitting.set(true);

    const mc = this.selectedMockCase();

    if (mc) {
      this.api.startMockCase(mc.caseId).subscribe({
        next: (res) => {
          this.submitting.set(false);
          this.executionStarted.emit({ type: 'async', executionId: res.executionId });
        },
        error: (err) => {
          this.submitting.set(false);
          console.error('Failed to start mock case', err);
        },
      });
    } else {
      const req: ReviewRequest = {
        storyId: this.storyId() || `story-${Date.now()}`,
        title: this.storyTitle() || 'Manual Review',
        storyText: this.storyText(),
        provider: {
          type: this.providerType(),
          model: this.model(),
          endpoint: this.providerType() === 'ollama' ? this.endpoint() : undefined,
          temperature: 0.2,
        },
        logging: {
          level: this.loggingLevel(),
          includePrompts: this.includePrompts(),
        },
      };

      this.api.startReviewStory(req).subscribe({
        next: (res) => {
          this.submitting.set(false);
          this.executionStarted.emit({ type: 'async', executionId: res.executionId });
        },
        error: (err) => {
          this.submitting.set(false);
          console.error('Review failed', err);
        },
      });
    }
  }

  protected reset(): void {
    this.selectedMockCase.set(null);
    this.storyText.set('');
    this.storyTitle.set('');
    this.storyId.set('');
    this.submitting.set(false);
  }
}
