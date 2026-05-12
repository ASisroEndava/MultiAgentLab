import { Component, computed, input } from '@angular/core';
import { DatePipe, SlicePipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { SemanticComparisonResult } from '../../core/models/api.models';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-comparison-panel',
  standalone: true,
  imports: [DatePipe, SlicePipe, MatCardModule, MatDividerModule, MatIconModule, StatusBadgeComponent],
  templateUrl: './comparison-panel.component.html',
  styleUrl: './comparison-panel.component.scss',
})
export class ComparisonPanelComponent {
  readonly result = input.required<SemanticComparisonResult>();

  readonly isIdentical = computed(() => {
    const r = this.result();
    return (
      r.issues.onlyInA.length === 0 &&
      r.issues.onlyInB.length === 0 &&
      r.issues.similar.length > 0 &&
      r.recommendations.onlyInA.length === 0 &&
      r.recommendations.onlyInB.length === 0
    );
  });
}
