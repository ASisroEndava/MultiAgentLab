import { Component, input } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AgentCard } from '../../../core/models/api.models';

@Component({
  selector: 'app-agent-card',
  standalone: true,
  imports: [DecimalPipe, MatCardModule, MatIconModule, MatProgressSpinnerModule, MatTooltipModule],
  template: `
    <div class="agent-card" [class]="'state-' + card().state">
      <div class="agent-icon">
        @switch (card().state) {
          @case ('executing') {
            <mat-spinner diameter="20"></mat-spinner>
          }
          @case ('completed') {
            <mat-icon class="icon-success">check_circle</mat-icon>
          }
          @case ('failed') {
            <mat-icon class="icon-error" [matTooltip]="card().error ?? ''">error</mat-icon>
          }
          @case ('skipped') {
            <mat-icon class="icon-muted" [matTooltip]="card().skipReason ?? ''">remove_circle_outline</mat-icon>
          }
          @default {
            <mat-icon class="icon-muted">radio_button_unchecked</mat-icon>
          }
        }
      </div>
      <div class="agent-info">
        <span class="agent-name">{{ card().name }}</span>
        @if (card().state === 'completed' && card().score !== undefined) {
          <span class="agent-score">{{ card().score | number:'1.2-2' }}</span>
        }
        @if (card().state === 'skipped' && card().skipReason) {
          <span class="agent-reason" [matTooltip]="card().skipReason!">skipped</span>
        }
        @if (card().issueCount !== undefined && card().issueCount! > 0) {
          <span class="agent-issues">{{ card().issueCount }} issue{{ card().issueCount! > 1 ? 's' : '' }}</span>
        }
      </div>
    </div>
  `,
  styles: [`
    .agent-card {
      display: flex;
      align-items: center;
      gap: 0.625rem;
      padding: 0.625rem 0.75rem;
      border-radius: 8px;
      border: 1px solid var(--mat-sys-outline-variant);
      background: var(--mat-sys-surface-container);
      transition: all 0.2s ease;
    }
    .state-executing { border-color: var(--mat-sys-primary); }
    .state-completed { border-color: color-mix(in srgb, #4ade80 30%, transparent); }
    .state-failed    { border-color: color-mix(in srgb, #f87171 30%, transparent); }
    .state-skipped   { opacity: 0.5; }

    .agent-icon { display: flex; align-items: center; min-width: 24px; }
    .icon-success { color: #4ade80; font-size: 20px; }
    .icon-error   { color: #f87171; font-size: 20px; }
    .icon-muted   { color: var(--mat-sys-outline); font-size: 20px; }

    .agent-info {
      display: flex; flex: 1; align-items: center;
      gap: 0.5rem; flex-wrap: wrap; min-width: 0;
    }
    .agent-name {
      font-size: 0.8rem; font-weight: 500;
      color: var(--mat-sys-on-surface);
      white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
    }
    .agent-score {
      font-size: 0.75rem; font-weight: 600;
      color: #4ade80; margin-left: auto;
    }
    .agent-issues { font-size: 0.7rem; color: #facc15; }
    .agent-reason {
      font-size: 0.7rem; color: var(--mat-sys-outline);
      cursor: help; text-decoration: underline dotted;
    }
  `],
})
export class AgentCardComponent {
  readonly card = input.required<AgentCard>();
}
