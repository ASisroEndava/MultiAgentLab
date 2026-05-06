import { Component, inject } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ExecutionStateService } from './core/services/execution-state.service';
import { DecisionPanelComponent } from './features/decision-panel/decision-panel.component';
import { EventTimelineComponent } from './features/event-timeline/event-timeline.component';
import { FinalResultComponent } from './features/final-result/final-result.component';
import { HistoryInputComponent, SubmitEvent } from './features/history-input/history-input.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    MatToolbarModule, MatIconModule, MatTooltipModule,
    HistoryInputComponent, DecisionPanelComponent,
    EventTimelineComponent, FinalResultComponent,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  protected readonly state = inject(ExecutionStateService);

  protected onExecutionStarted(event: SubmitEvent): void {
    this.state.reset();
    if (event.type === 'async') {
      this.state.begin(event.executionId);
    } else {
      this.state.setDirectResult(event.result);
    }
  }
}
