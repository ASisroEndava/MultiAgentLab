import { Component, computed, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  imports: [MatIconModule],
  template: `
    <span class="status-badge" [class]="'badge-' + status()">
      <mat-icon>{{ icon() }}</mat-icon>
      {{ label() }}
    </span>
  `,
  styles: [`
    .status-badge {
      display: inline-flex; align-items: center; gap: 0.375rem;
      padding: 0.25rem 0.75rem; border-radius: 999px;
      font-size: 0.8rem; font-weight: 700; letter-spacing: 0.04em;
    }
    .status-badge mat-icon { font-size: 16px; width: 16px; height: 16px; }
    .badge-verde    { background: rgba(74,222,128,0.15); color: #4ade80; }
    .badge-amarillo { background: rgba(250,204,21,0.15); color: #facc15; }
    .badge-rojo     { background: rgba(248,113,113,0.15); color: #f87171; }
    .badge-default  { background: rgba(148,163,184,0.15); color: #94a3b8; }
  `],
})
export class StatusBadgeComponent {
  readonly status = input<string>('default');

  readonly label = computed(() => {
    const s = this.status();
    if (s === 'verde') return 'Verde';
    if (s === 'amarillo') return 'Amarillo';
    if (s === 'rojo') return 'Rojo';
    return 'Unknown';
  });

  readonly icon = computed(() => {
    const s = this.status();
    if (s === 'verde') return 'check_circle';
    if (s === 'amarillo') return 'warning';
    if (s === 'rojo') return 'cancel';
    return 'help_outline';
  });
}
