import { DecimalPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AgentCardComponent } from '../../shared/components/agent-card/agent-card.component';
import { ExecutionStateService } from '../../core/services/execution-state.service';

@Component({
  selector: 'app-decision-panel',
  standalone: true,
  imports: [DecimalPipe, MatCardModule, MatIconModule, MatProgressSpinnerModule, AgentCardComponent],
  templateUrl: './decision-panel.component.html',
  styleUrl: './decision-panel.component.scss',
})
export class DecisionPanelComponent {
  protected readonly state = inject(ExecutionStateService);
}
