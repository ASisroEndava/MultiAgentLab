import { DecimalPipe, SlicePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';
import { ExecutionStateService } from '../../core/services/execution-state.service';

@Component({
  selector: 'app-final-result',
  standalone: true,
  imports: [
    DecimalPipe, SlicePipe,
    MatCardModule, MatIconModule, MatProgressSpinnerModule,
    MatExpansionModule, MatDividerModule, MatButtonModule,
    StatusBadgeComponent,
  ],
  templateUrl: './final-result.component.html',
  styleUrl: './final-result.component.scss',
})
export class FinalResultComponent {
  protected readonly state = inject(ExecutionStateService);
}
