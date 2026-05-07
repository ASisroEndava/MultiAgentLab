export interface ProviderSelection {
  type: 'ollama' | 'bedrock';
  model: string;
  endpoint?: string;
  region?: string;
  temperature?: number;
  maxTokens?: number;
}

export interface LoggingOptions {
  level: 'basic' | 'standard' | 'full';
  includePrompts?: boolean;
  includeResponses?: boolean;
}

export interface ReviewRequest {
  storyId: string;
  title: string;
  storyText: string;
  provider: ProviderSelection;
  logging?: LoggingOptions;
}

export interface AgentResult {
  agent: string;
  status: string;
  score: number;
  issues: string[];
  recommendations: string[];
  questions: string[];
  rawSummary?: string;
}

export interface SkippedAgent {
  agent: string;
  reason: string;
}

export interface ReviewResult {
  executionId: string;
  status: 'green' | 'yellow' | 'red';
  summary?: string;
  provider: string;
  model: string;
  invokedAgents?: string[];
  skippedAgents?: SkippedAgent[];
  issues?: string[];
  recommendations?: string[];
  conflicts?: string[];
  resolution?: string[];
  agentResults?: AgentResult[];
}

export interface ExecutionLogEvent {
  executionId: string;
  timestamp: string;
  eventType: string;
  data: unknown;
}

export interface MockCaseSummary {
  caseId: string;
  title: string;
  description: string;
  expectedAgents: string[];
  expectedStatus: string;
}

export interface StartCaseResponse {
  executionId: string;
  caseId: string;
  status: string;
}

export interface ExecutionSummary {
  executionId: string;
  timestamp: string;
  title?: string;
  storyId?: string;
  status: string;
  totalMs: number;
  eventCount: number;
}

export interface AgentCard {
  name: string;
  state: 'selected' | 'executing' | 'completed' | 'failed' | 'skipped';
  score?: number;
  issueCount?: number;
  error?: string;
  skipReason?: string;
}

export type OverallState = 'idle' | 'executing' | 'complete' | 'error';
export type StreamStatus = 'idle' | 'polling' | 'complete' | 'error';
