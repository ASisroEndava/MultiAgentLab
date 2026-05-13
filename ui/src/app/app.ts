import { Component, effect, inject, signal, viewChild } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ExecutionStateService } from './core/services/execution-state.service';
import { DecisionPanelComponent } from './features/decision-panel/decision-panel.component';
import { EventTimelineComponent } from './features/event-timeline/event-timeline.component';
import { FinalResultComponent } from './features/final-result/final-result.component';
import { HistoryInputComponent, SubmitEvent } from './features/history-input/history-input.component';
import { StoryHistoryComponent } from './features/story-history/story-history.component';
import { ExecutionHistoryComponent } from './features/execution-history/execution-history.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    MatToolbarModule, MatIconModule, MatTooltipModule, MatTabsModule,
    HistoryInputComponent, DecisionPanelComponent,
    EventTimelineComponent, FinalResultComponent, StoryHistoryComponent,
    ExecutionHistoryComponent,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  protected readonly state = inject(ExecutionStateService);
  protected readonly activeTab = signal(0);
  private readonly executionHistory = viewChild(ExecutionHistoryComponent);
  private readonly storyHistory = viewChild(StoryHistoryComponent);

  constructor() {
    effect(() => {
      if (this.state.overallState() === 'complete') {
        this.activeTab.set(1);
      }
    });
  }

  protected onExecutionStarted(event: SubmitEvent): void {
    this.activeTab.set(0);
    this.state.reset();
    if (event.type === 'async') {
      this.state.begin(event.executionId);
    } else {
      this.state.setDirectResult(event.result);
    }
  }

  protected onTabChanged(index: number): void {
    this.activeTab.set(index);
    if (index === 2) {
      this.executionHistory()?.refresh();
    }
    if (index === 3) {
      this.storyHistory()?.refresh();
    }
  }
}
