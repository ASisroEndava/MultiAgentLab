import { DatePipe } from '@angular/common';
import { Component, computed, ElementRef, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { ExecutionLogEvent } from '../../core/models/api.models';
import { ExecutionStateService } from '../../core/services/execution-state.service';

interface EventDisplay {
  event: ExecutionLogEvent;
  icon: string;
  cssClass: string;
  label: string;
  summary: string;
  collapsible: boolean;
  expanded: boolean;
  detail?: string;
}

const ALL_FILTER = 'all';
const VERBOSE_TYPES = ['agent_prompt_sent', 'agent_response_received'];

@Component({
  selector: 'app-event-timeline',
  standalone: true,
  imports: [DatePipe, FormsModule, MatCardModule, MatFormFieldModule, MatSelectModule, MatIconModule],
  templateUrl: './event-timeline.component.html',
  styleUrl: './event-timeline.component.scss',
})
export class EventTimelineComponent {
  protected readonly state = inject(ExecutionStateService);
  protected readonly listEl = viewChild<ElementRef>('eventList');

  protected filterType = signal(ALL_FILTER);
  protected showVerbose = signal(false);

  protected readonly filterOptions = [
    { value: ALL_FILTER, label: 'All events' },
    { value: 'selected_agents', label: 'Agent selection' },
    { value: 'agent_started', label: 'Agent started' },
    { value: 'agent_completed', label: 'Agent completed' },
    { value: 'agent_failed', label: 'Agent failed' },
    { value: 'final_result_generated', label: 'Final result' },
    { value: 'request_completed', label: 'Completed' },
  ];

  protected readonly displayEvents = computed<EventDisplay[]>(() => {
    const filter = this.filterType();
    const verbose = this.showVerbose();
    return this.state.events()
      .filter(e => {
        if (!verbose && VERBOSE_TYPES.includes(e.eventType)) return false;
        if (filter !== ALL_FILTER && e.eventType !== filter) return false;
        return true;
      })
      .map(e => this.toDisplay(e));
  });

  protected toggle(item: EventDisplay): void {
    if (item.collapsible) item.expanded = !item.expanded;
  }

  private toDisplay(e: ExecutionLogEvent): EventDisplay {
    const d = e.data as Record<string, unknown>;
    const map: Record<string, Pick<EventDisplay, 'icon' | 'cssClass' | 'label'>> = {
      request_received:        { icon: 'play_arrow',      cssClass: 'ev-request',    label: 'Request received' },
      supervisor_started:      { icon: 'settings',        cssClass: 'ev-supervisor', label: 'Supervisor started' },
      selected_agents:         { icon: 'checklist',       cssClass: 'ev-selection',  label: 'Agents selected' },
      agent_started:           { icon: 'smart_toy',       cssClass: 'ev-agent-start',label: `[${d['agent']}] started` },
      agent_prompt_sent:       { icon: 'send',            cssClass: 'ev-prompt',     label: `[${d['agent']}] prompt sent` },
      agent_response_received: { icon: 'reply',           cssClass: 'ev-response',   label: `[${d['agent']}] response` },
      agent_completed:         { icon: 'check_circle',    cssClass: 'ev-agent-ok',   label: `[${d['agent']}] completed · score ${(d['score'] as number)?.toFixed(2)}` },
      agent_failed:            { icon: 'error',           cssClass: 'ev-agent-fail', label: `[${d['agent']}] FAILED` },
      conflict_detected:       { icon: 'bolt',            cssClass: 'ev-conflict',   label: 'Conflicts detected' },
      supervisor_resolution:   { icon: 'task_alt',        cssClass: 'ev-resolution', label: 'Supervisor resolution' },
      final_result_generated:  { icon: 'star',            cssClass: 'ev-result',     label: 'Final result generated' },
      request_completed:       { icon: 'stop_circle',     cssClass: 'ev-done',       label: 'Execution completed' },
    };

    const meta = map[e.eventType] ?? { icon: 'info', cssClass: 'ev-default', label: e.eventType };
    const collapsible = VERBOSE_TYPES.includes(e.eventType);
    const detail = collapsible ? String(d['prompt'] ?? d['response'] ?? '') : undefined;

    let summary = '';
    if (e.eventType === 'selected_agents') {
      const invoked = (d['invoked'] as string[]) ?? [];
      summary = `Invoked: ${invoked.join(', ')}`;
    } else if (e.eventType === 'agent_failed') {
      summary = String(d['error'] ?? '');
    }

    return { event: e, ...meta, collapsible, expanded: false, detail, summary };
  }
}
