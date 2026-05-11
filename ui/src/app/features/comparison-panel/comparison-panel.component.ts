import { Component, computed, input } from '@angular/core';
import { DatePipe, SlicePipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { ComparisonResult } from '../../core/models/api.models';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-comparison-panel',
  standalone: true,
  imports: [DatePipe, SlicePipe, MatCardModule, MatDividerModule, MatIconModule, StatusBadgeComponent],
  templateUrl: './comparison-panel.component.html',
  styleUrl: './comparison-panel.component.scss',
})
export class ComparisonPanelComponent {
  readonly result = input.required<ComparisonResult>();

  readonly isIdentical = computed(() => {
    const r = this.result();
    return (
      r.issuesOnlyInA.length === 0 &&
      r.issuesOnlyInB.length === 0 &&
      r.recommendationsOnlyInA.length === 0 &&
      r.recommendationsOnlyInB.length === 0
    );
  });
}
