using System.Text;
using System.Text.Json;
using MultiAgentLab.Api.Application.Supervisor;
using MultiAgentLab.Api.Domain;
using MultiAgentLab.Api.Infrastructure.Logging;
using MultiAgentLab.Api.Infrastructure.Mocks;

namespace MultiAgentLab.Api;

public static class ReviewEndpoints
{
    public static void MapReviewEndpoints(this WebApplication app)
    {
        app.MapPost("/review-story", ReviewStoryAsync)
           .WithName("ReviewStory")
           .WithOpenApi();

        app.MapGet("/executions", ListExecutionsAsync)
           .WithName("ListExecutions")
           .WithOpenApi();

        app.MapGet("/executions/{executionId}", GetExecutionAsync)
           .WithName("GetExecution")
           .WithOpenApi();

        app.MapGet("/executions/{executionId}/log", GetExecutionLogAsync)
           .WithName("GetExecutionLog")
           .WithOpenApi();

        app.MapGet("/executions/{executionId}/log/text", GetExecutionLogTextAsync)
           .WithName("GetExecutionLogText")
           .WithOpenApi()
           .Produces<string>(200, "text/plain");

        app.MapGet("/mock-cases", ListMockCasesAsync)
           .WithName("ListMockCases")
           .WithOpenApi();

        app.MapPost("/mock-cases/{caseId}/run", RunMockCaseAsync)
           .WithName("RunMockCase")
           .WithOpenApi();

        app.MapPost("/mock-cases/{caseId}/start", StartMockCaseAsync)
           .WithName("StartMockCase")
           .WithOpenApi();

        app.MapGet("/dashboard", GetDashboard)
           .WithName("Dashboard")
           .ExcludeFromDescription();
    }

    private static async Task<IResult> ReviewStoryAsync(
        ReviewRequest request,
        ReviewSupervisor supervisor,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await supervisor.ReviewAsync(request, cancellationToken);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: 500,
                title: "Error during review");
        }
    }

    private static async Task<IResult> ListExecutionsAsync(
        IExecutionLogger logger,
        CancellationToken cancellationToken)
    {
        var ids = await logger.GetAllExecutionIdsAsync(cancellationToken);
        var summaries = new List<object>();

        foreach (var id in ids)
        {
            var logs = await logger.GetLogsAsync(id, cancellationToken);
            var request = logs.FirstOrDefault(l => l.EventType == "request_received");
            var final_ = logs.FirstOrDefault(l => l.EventType == "final_result_generated");
            var completed = logs.FirstOrDefault(l => l.EventType == "request_completed");

            var data = request?.Data as JsonElement? ?? default;
            var finalData = final_?.Data as JsonElement? ?? default;
            var completedData = completed?.Data as JsonElement? ?? default;

            summaries.Add(new
            {
                executionId = id,
                timestamp = request?.Timestamp,
                title = data.ValueKind == JsonValueKind.Object && data.TryGetProperty("title", out var t) ? t.GetString() : null,
                storyId = data.ValueKind == JsonValueKind.Object && data.TryGetProperty("storyId", out var s) ? s.GetString() : null,
                status = finalData.ValueKind == JsonValueKind.Object && finalData.TryGetProperty("status", out var st) ? st.GetString() : "unknown",
                totalMs = completedData.ValueKind == JsonValueKind.Object && completedData.TryGetProperty("totalMs", out var ms) ? ms.GetDouble() : 0,
                eventCount = logs.Count
            });
        }

        return Results.Ok(summaries);
    }

    private static async Task<IResult> GetExecutionAsync(
        string executionId,
        IExecutionLogger logger,
        CancellationToken cancellationToken)
    {
        var logs = await logger.GetLogsAsync(executionId, cancellationToken);
        var finalResult = logs.LastOrDefault(l => l.EventType == "final_result_generated");

        if (finalResult == null)
            return Results.NotFound(new { message = $"Execution {executionId} not found" });

        return Results.Ok(finalResult);
    }

    private static async Task<IResult> GetExecutionLogAsync(
        string executionId,
        IExecutionLogger logger,
        CancellationToken cancellationToken)
    {
        var logs = await logger.GetLogsAsync(executionId, cancellationToken);

        if (logs.Count == 0)
            return Results.NotFound(new { message = $"No logs found for execution {executionId}" });

        return Results.Ok(logs);
    }

    private static async Task<IResult> ListMockCasesAsync(
        MockCaseLoader mockLoader,
        CancellationToken cancellationToken)
    {
        var cases = await mockLoader.ListCasesAsync(cancellationToken);
        var summary = cases.Select(c => new
        {
            c.CaseId,
            c.Title,
            c.Description,
            c.ExpectedAgents,
            c.ExpectedStatus
        });
        return Results.Ok(summary);
    }

    private static async Task<IResult> RunMockCaseAsync(
        string caseId,
        MockCaseLoader mockLoader,
        ReviewSupervisor supervisor,
        CancellationToken cancellationToken)
    {
        var mockCase = await mockLoader.GetCaseAsync(caseId, cancellationToken);

        if (mockCase == null)
            return Results.NotFound(new { message = $"Mock case {caseId} not found" });

        try
        {
            var result = await supervisor.ReviewAsync(mockCase.Request, cancellationToken);
            return Results.Ok(new
            {
                mockCase = new
                {
                    mockCase.CaseId,
                    mockCase.Title,
                    mockCase.Description,
                    mockCase.ExpectedAgents,
                    mockCase.ExpectedStatus
                },
                result
            });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: 500,
                title: $"Error running mock case {caseId}");
        }
    }

    private static async Task<IResult> StartMockCaseAsync(
        string caseId,
        MockCaseLoader mockLoader,
        ReviewSupervisor supervisor,
        CancellationToken cancellationToken)
    {
        var mockCase = await mockLoader.GetCaseAsync(caseId, cancellationToken);

        if (mockCase == null)
            return Results.NotFound(new { message = $"Mock case {caseId} not found" });

        var executionId = $"exec-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString()[..8]}";

        _ = Task.Run(async () =>
        {
            try
            {
                await supervisor.ReviewAsync(mockCase.Request, CancellationToken.None, executionId);
            }
            catch (Exception)
            {
            }
        });

        return Results.Ok(new { executionId, caseId, status = "started" });
    }

    private static async Task<IResult> GetExecutionLogTextAsync(
        string executionId,
        IExecutionLogger logger,
        CancellationToken cancellationToken)
    {
        var logs = await logger.GetLogsAsync(executionId, cancellationToken);

        if (logs.Count == 0)
            return Results.NotFound($"No logs found for execution {executionId}");

        var sb = new StringBuilder();
        sb.AppendLine($"═══════════════════════════════════════════════════════════════");
        sb.AppendLine($"  EXECUTION LOG: {executionId}");
        sb.AppendLine($"═══════════════════════════════════════════════════════════════");
        sb.AppendLine();

        foreach (var log in logs)
        {
            var time = DateTime.Parse(log.Timestamp.ToString()).ToString("HH:mm:ss.fff");
            var data = log.Data as JsonElement? ?? default;

            switch (log.EventType)
            {
                case "request_received":
                    sb.AppendLine($"[{time}] ▶ REQUEST RECEIVED");
                    if (data.ValueKind == JsonValueKind.Object)
                    {
                        TryAppend(sb, data, "storyId", "       Story ID");
                        TryAppend(sb, data, "title", "       Title");
                    }
                    sb.AppendLine();
                    break;

                case "supervisor_started":
                    sb.AppendLine($"[{time}] ⚙ SUPERVISOR STARTED");
                    sb.AppendLine();
                    break;

                case "selected_agents":
                    sb.AppendLine($"[{time}] 🎯 AGENT SELECTION");
                    if (data.ValueKind == JsonValueKind.Object)
                    {
                        if (data.TryGetProperty("invoked", out var invoked))
                        {
                            var agents = invoked.EnumerateArray().Select(a => a.GetString()).ToList();
                            sb.AppendLine($"       Invoked:  {string.Join(", ", agents)}");
                        }
                        if (data.TryGetProperty("skipped", out var skipped))
                        {
                            foreach (var s in skipped.EnumerateArray())
                            {
                                var agent = s.GetProperty("agent").GetString();
                                var reason = s.GetProperty("reason").GetString();
                                sb.AppendLine($"       Skipped:  {agent} → {reason}");
                            }
                        }
                    }
                    sb.AppendLine();
                    break;

                case "agent_started":
                    var agentName = data.ValueKind == JsonValueKind.Object && data.TryGetProperty("agent", out var an) ? an.GetString() : "?";
                    sb.AppendLine($"[{time}] ┌─ AGENT [{agentName?.ToUpper()}] STARTED");
                    break;

                case "agent_prompt_sent":
                    var pa = data.ValueKind == JsonValueKind.Object && data.TryGetProperty("agent", out var pa2) ? pa2.GetString() : "?";
                    var prompt = data.ValueKind == JsonValueKind.Object && data.TryGetProperty("prompt", out var pr) ? pr.GetString() : "";
                    sb.AppendLine($"[{time}] │  PROMPT SENT ({prompt?.Length ?? 0} chars):");
                    sb.AppendLine($"       ┊  ┌────────────────────────────────────────────");
                    foreach (var line in (prompt ?? "").Split('\n'))
                        sb.AppendLine($"       ┊  │ {line.TrimEnd('\r')}");
                    sb.AppendLine($"       ┊  └────────────────────────────────────────────");
                    break;

                case "agent_response_received":
                    var ra = data.ValueKind == JsonValueKind.Object && data.TryGetProperty("agent", out var ra2) ? ra2.GetString() : "?";
                    var resp = data.ValueKind == JsonValueKind.Object && data.TryGetProperty("response", out var rr) ? rr.GetString() : "";
                    sb.AppendLine($"[{time}] │  LLM RESPONSE ({resp?.Length ?? 0} chars):");
                    sb.AppendLine($"       ┊  ┌────────────────────────────────────────────");
                    foreach (var line in (resp ?? "").Split('\n'))
                        sb.AppendLine($"       ┊  │ {line.TrimEnd('\r')}");
                    sb.AppendLine($"       ┊  └────────────────────────────────────────────");
                    break;

                case "agent_completed":
                    if (data.ValueKind == JsonValueKind.Object)
                    {
                        var ca = data.TryGetProperty("agent", out var ca2) ? ca2.GetString() : "?";
                        var status = data.TryGetProperty("status", out var st) ? st.GetString() : "?";
                        var score = data.TryGetProperty("score", out var sc) ? sc.GetDouble().ToString("F1") : "?";
                        var issues = data.TryGetProperty("issues", out var iss) ? iss.GetArrayLength() : 0;
                        sb.AppendLine($"[{time}] └─ AGENT [{ca?.ToUpper()}] COMPLETED → status={status}, score={score}, issues={issues}");
                    }
                    sb.AppendLine();
                    break;

                case "agent_failed":
                    if (data.ValueKind == JsonValueKind.Object)
                    {
                        var fa = data.TryGetProperty("agent", out var fa2) ? fa2.GetString() : "?";
                        var err = data.TryGetProperty("error", out var er) ? er.GetString() : "?";
                        sb.AppendLine($"[{time}] └─ AGENT [{fa?.ToUpper()}] FAILED ✗ → {err}");
                    }
                    sb.AppendLine();
                    break;

                case "conflict_detected":
                    sb.AppendLine($"[{time}] ⚡ CONFLICTS DETECTED");
                    if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty("conflicts", out var cf))
                    {
                        foreach (var c in cf.EnumerateArray())
                            sb.AppendLine($"       - {c.GetString()}");
                    }
                    sb.AppendLine();
                    break;

                case "supervisor_resolution":
                    sb.AppendLine($"[{time}] ✔ SUPERVISOR RESOLUTION");
                    if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty("resolution", out var res))
                    {
                        foreach (var r in res.EnumerateArray())
                            sb.AppendLine($"       - {r.GetString()}");
                    }
                    sb.AppendLine();
                    break;

                case "final_result_generated":
                    sb.AppendLine($"[{time}] ★ FINAL RESULT");
                    if (data.ValueKind == JsonValueKind.Object)
                    {
                        TryAppend(sb, data, "status", "       Status");
                        if (data.TryGetProperty("invokedAgents", out var ia))
                        {
                            var agents = ia.EnumerateArray().Select(a => a.GetString()).ToList();
                            sb.AppendLine($"       Agents:       {string.Join(", ", agents)}");
                        }
                        TryAppend(sb, data, "issueCount", "       Issues");
                    }
                    sb.AppendLine();
                    break;

                case "request_completed":
                    var ms = data.ValueKind == JsonValueKind.Object && data.TryGetProperty("totalMs", out var tm) ? tm.GetDouble() : 0;
                    sb.AppendLine($"[{time}] ■ COMPLETED in {ms / 1000:F1}s");
                    break;

                default:
                    sb.AppendLine($"[{time}] ? {log.EventType}");
                    break;
            }
        }

        sb.AppendLine();
        sb.AppendLine($"═══════════════════════════════════════════════════════════════");

        return Results.Text(sb.ToString(), "text/plain; charset=utf-8");
    }

    private static void TryAppend(StringBuilder sb, JsonElement data, string prop, string label)
    {
        if (data.TryGetProperty(prop, out var val))
            sb.AppendLine($"{label,-20} {val}");
    }

    private static IResult GetDashboard()
    {
        var html = """
<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>MultiAgentLab - Dashboard</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: 'Segoe UI', system-ui, sans-serif; background: #0f172a; color: #e2e8f0; min-height: 100vh; }
        .header { background: #1e293b; padding: 1.5rem 2rem; border-bottom: 1px solid #334155; display: flex; justify-content: space-between; align-items: center; }
        .header h1 { font-size: 1.5rem; color: #f8fafc; }
        .header p { color: #94a3b8; font-size: 0.875rem; margin-top: 0.25rem; }
        .header-right { display: flex; gap: 0.5rem; align-items: center; }
        .tab-bar { background: #1e293b; border-bottom: 1px solid #334155; padding: 0 2rem; display: flex; gap: 0; }
        .tab { padding: 0.75rem 1.25rem; color: #94a3b8; cursor: pointer; border-bottom: 2px solid transparent; font-size: 0.9rem; transition: all 0.2s; }
        .tab:hover { color: #e2e8f0; }
        .tab.active { color: #60a5fa; border-bottom-color: #60a5fa; }
        .container { max-width: 1200px; margin: 0 auto; padding: 2rem; }
        .section { margin-bottom: 2rem; }
        .section h2 { font-size: 1.1rem; color: #94a3b8; margin-bottom: 1rem; text-transform: uppercase; letter-spacing: 0.05em; }
        .hidden { display: none !important; }
        .mock-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(340px, 1fr)); gap: 1rem; }
        .mock-card { background: #1e293b; border: 1px solid #334155; border-radius: 0.75rem; padding: 1.25rem; transition: all 0.2s; }
        .mock-card:hover { border-color: #60a5fa; transform: translateY(-2px); }
        .mock-card.running { opacity: 0.7; }
        .mock-card h3 { font-size: 1rem; color: #f1f5f9; margin-bottom: 0.5rem; }
        .mock-card p { font-size: 0.8rem; color: #94a3b8; margin-bottom: 0.75rem; }
        .mock-card .agents { display: flex; gap: 0.375rem; flex-wrap: wrap; margin-bottom: 0.5rem; }
        .badge { padding: 0.2rem 0.5rem; border-radius: 9999px; font-size: 0.7rem; font-weight: 600; }
        .badge-agent { background: #1e3a5f; color: #60a5fa; }
        .badge-verde { background: #14532d; color: #4ade80; }
        .badge-amarillo { background: #713f12; color: #facc15; }
        .badge-rojo { background: #7f1d1d; color: #f87171; }
        .badge-unknown { background: #334155; color: #94a3b8; }
        .badge-status { font-size: 0.75rem; padding: 0.25rem 0.625rem; }
        .run-btn { background: #3b82f6; color: white; border: none; padding: 0.5rem 1rem; border-radius: 0.5rem; font-size: 0.8rem; cursor: pointer; width: 100%; margin-top: 0.5rem; }
        .run-btn:hover { background: #2563eb; }
        .run-btn:disabled { background: #475569; cursor: wait; }
        .exec-table { width: 100%; border-collapse: collapse; }
        .exec-table th { text-align: left; padding: 0.75rem 1rem; color: #64748b; font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.05em; border-bottom: 1px solid #334155; }
        .exec-table td { padding: 0.75rem 1rem; border-bottom: 1px solid #1e293b; font-size: 0.85rem; }
        .exec-table tr { cursor: pointer; transition: background 0.15s; }
        .exec-table tr:hover { background: #1e293b; }
        .exec-table tr.selected { background: #1e3a5f; }
        .exec-table .mono { font-family: 'Cascadia Code', 'Fira Code', monospace; font-size: 0.75rem; color: #94a3b8; }
        .lookup-bar { display: flex; gap: 0.5rem; margin-bottom: 1.5rem; }
        .lookup-bar input { flex: 1; background: #1e293b; border: 1px solid #334155; border-radius: 0.5rem; padding: 0.625rem 1rem; color: #e2e8f0; font-size: 0.85rem; font-family: 'Cascadia Code', monospace; }
        .lookup-bar input::placeholder { color: #475569; }
        .lookup-bar input:focus { outline: none; border-color: #60a5fa; }
        .lookup-btn { background: #3b82f6; color: white; border: none; padding: 0.625rem 1.25rem; border-radius: 0.5rem; cursor: pointer; font-size: 0.85rem; white-space: nowrap; }
        .lookup-btn:hover { background: #2563eb; }
        .log-panel { background: #0c0c0c; border: 1px solid #334155; border-radius: 0.75rem; overflow: hidden; }
        .log-header { background: #1e293b; padding: 1rem 1.25rem; display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid #334155; }
        .log-header h3 { font-size: 0.95rem; }
        .log-body { padding: 1rem 1.25rem; font-family: 'Cascadia Code', 'Fira Code', monospace; font-size: 0.78rem; line-height: 1.7; white-space: pre-wrap; max-height: 700px; overflow-y: auto; color: #cbd5e1; }
        .log-body .ev-request { color: #60a5fa; }
        .log-body .ev-supervisor { color: #a78bfa; }
        .log-body .ev-selection { color: #f472b6; }
        .log-body .ev-agent-start { color: #22d3ee; }
        .log-body .ev-agent-ok { color: #4ade80; }
        .log-body .ev-agent-fail { color: #f87171; }
        .log-body .ev-conflict { color: #fb923c; }
        .log-body .ev-result { color: #facc15; font-weight: bold; }
        .log-body .ev-done { color: #94a3b8; }
        .log-body .ev-time { color: #64748b; }
        .log-body .ev-prompt { color: #a78bfa; opacity: 0.85; }
        .log-body .ev-response { color: #34d399; opacity: 0.85; }
        .log-body .ev-box { color: #475569; }
        .result-summary { background: #1e293b; border: 1px solid #334155; border-radius: 0.75rem; padding: 1.25rem; margin-bottom: 1rem; }
        .result-summary h3 { margin-bottom: 0.75rem; }
        .result-row { display: flex; gap: 2rem; flex-wrap: wrap; }
        .result-item { margin-bottom: 0.5rem; }
        .result-item label { font-size: 0.75rem; color: #64748b; display: block; }
        .result-item span { font-size: 0.95rem; }
        .agent-details { margin-top: 1rem; display: grid; grid-template-columns: repeat(auto-fill, minmax(320px, 1fr)); gap: 0.75rem; }
        .agent-card { background: #0f172a; border: 1px solid #334155; border-radius: 0.5rem; padding: 1rem; }
        .agent-card h4 { font-size: 0.85rem; margin-bottom: 0.5rem; display: flex; align-items: center; gap: 0.5rem; }
        .agent-card .detail-list { font-size: 0.8rem; color: #94a3b8; }
        .agent-card .detail-list li { margin-bottom: 0.25rem; list-style: disc; margin-left: 1rem; }
        .spinner { display: inline-block; width: 14px; height: 14px; border: 2px solid #475569; border-top-color: #60a5fa; border-radius: 50%; animation: spin 0.8s linear infinite; margin-right: 0.5rem; vertical-align: middle; }
        @keyframes spin { to { transform: rotate(360deg); } }
        .empty-state { text-align: center; padding: 3rem; color: #475569; font-size: 0.9rem; }
    </style>
</head>
<body>
    <div class="header">
        <div>
            <h1>MultiAgentLab Dashboard</h1>
            <p>POC Multiagente - Revision de Historias de Usuario</p>
        </div>
    </div>
    <div class="tab-bar">
        <div class="tab active" onclick="switchTab('run')">Ejecutar</div>
        <div class="tab" onclick="switchTab('history')">Historial</div>
    </div>
    <div class="container">
        <!-- TAB: Ejecutar -->
        <div id="tab-run">
            <div class="section">
                <h2>Mock Cases</h2>
                <div class="mock-grid" id="mockGrid"></div>
            </div>
            <div class="section" id="runResultSection" style="display:none;">
                <div class="result-summary" id="resultSummary">
                    <h3>Resultado</h3>
                    <div class="result-row" id="resultRow"></div>
                    <div class="agent-details" id="agentDetails"></div>
                </div>
                <div class="log-panel">
                    <div class="log-header">
                        <h3 id="logTitle">Execution Log</h3>
                    </div>
                    <div class="log-body" id="logBody"></div>
                </div>
            </div>
        </div>

        <!-- TAB: Historial -->
        <div id="tab-history" class="hidden">
            <div class="section">
                <h2>Buscar Ejecucion</h2>
                <div class="lookup-bar">
                    <input type="text" id="execIdInput" placeholder="Pegar execution ID, ej: exec-20260421-152532-4e23f21f" />
                    <button class="lookup-btn" onclick="lookupExecution()">Ver Log</button>
                </div>
            </div>
            <div class="section">
                <h2>Ejecuciones Anteriores</h2>
                <div id="execList"></div>
            </div>
            <div class="section" id="historyResultSection" style="display:none;">
                <div class="result-summary" id="histResultSummary">
                    <h3>Resultado</h3>
                    <div class="result-row" id="histResultRow"></div>
                    <div class="agent-details" id="histAgentDetails"></div>
                </div>
                <div class="log-panel">
                    <div class="log-header">
                        <h3 id="histLogTitle">Execution Log</h3>
                    </div>
                    <div class="log-body" id="histLogBody"></div>
                </div>
            </div>
        </div>
    </div>
<script>
const BASE = window.location.origin;

function switchTab(tab) {
    document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
    document.getElementById('tab-run').classList.toggle('hidden', tab !== 'run');
    document.getElementById('tab-history').classList.toggle('hidden', tab !== 'history');
    document.querySelector(`.tab:nth-child(${tab === 'run' ? 1 : 2})`).classList.add('active');
    if (tab === 'history') loadExecutions();
}

async function loadMocks() {
    const res = await fetch(`${BASE}/mock-cases`);
    const cases = await res.json();
    const grid = document.getElementById('mockGrid');
    grid.innerHTML = '';
    cases.forEach(c => {
        const card = document.createElement('div');
        card.className = 'mock-card';
        card.id = `card-${c.caseId}`;
        card.innerHTML = `
            <h3>${c.title}</h3>
            <p>${c.description}</p>
            <div class="agents">${c.expectedAgents.map(a => `<span class="badge badge-agent">${a}</span>`).join('')}</div>
            <div>Esperado: <span class="badge badge-status badge-${c.expectedStatus}">${c.expectedStatus}</span></div>
            <button class="run-btn" onclick="runCase('${c.caseId}')">Ejecutar</button>
        `;
        grid.appendChild(card);
    });
}

async function runCase(caseId) {
    const card = document.getElementById(`card-${caseId}`);
    const btn = card.querySelector('.run-btn');
    btn.disabled = true;
    btn.innerHTML = '<span class="spinner"></span>Iniciando...';
    card.classList.add('running');
    document.getElementById('runResultSection').style.display = 'block';
    document.getElementById('logBody').innerHTML = '<span class="ev-supervisor">Iniciando ejecucion...</span>';
    document.getElementById('logTitle').textContent = `Ejecutando ${caseId}...`;
    document.getElementById('agentDetails').innerHTML = '';
    document.getElementById('resultRow').innerHTML = '';

    try {
        const startRes = await fetch(`${BASE}/mock-cases/${caseId}/start`, { method: 'POST' });
        const { executionId } = await startRes.json();
        document.getElementById('logTitle').textContent = `Live: ${executionId}`;

        let done = false;
        while (!done) {
            await new Promise(r => setTimeout(r, 1000));
            const logRes = await fetch(`${BASE}/executions/${executionId}/log`);
            const logs = await logRes.json();
            renderLiveProgress(logs);
            done = logs.some(l => l.eventType === 'request_completed');
        }

        const logRes = await fetch(`${BASE}/executions/${executionId}/log`);
        const logs = await logRes.json();
        buildResultFromLogs(executionId, logs, 'resultRow', 'agentDetails');
        await loadLog(executionId, 'logTitle', 'logBody');
    } catch (e) {
        document.getElementById('logBody').innerHTML = `<span class="ev-agent-fail">Error: ${e.message}</span>`;
    } finally {
        btn.disabled = false;
        btn.textContent = 'Ejecutar';
        card.classList.remove('running');
    }
}

function renderLiveProgress(logs) {
    const body = document.getElementById('logBody');
    let html = '';
    const selectedEvt = logs.find(l => l.eventType === 'selected_agents');
    const invoked = selectedEvt?.data?.invoked || [];
    const skipped = selectedEvt?.data?.skipped || [];

    if (selectedEvt) {
        html += `<div style="margin-bottom:1rem;">`;
        html += `<span class="ev-selection" style="font-weight:bold;">Agentes seleccionados:</span> ${invoked.map(a => `<span class="badge badge-agent" style="margin-left:4px;">${a}</span>`).join(' ')}`;
        if (skipped.length > 0) {
            html += `<br/><span style="color:#64748b;font-size:0.8rem;">Omitidos: ${skipped.map(s => s.agent).join(', ')}</span>`;
        }
        html += `</div>`;
    }

    const agentStates = {};
    logs.forEach(l => {
        const name = l.data?.agent;
        if (!name) return;
        if (l.eventType === 'agent_started') agentStates[name] = { status: 'running', start: l.timestamp };
        if (l.eventType === 'agent_prompt_sent') { if (agentStates[name]) agentStates[name].status = 'waiting_llm'; }
        if (l.eventType === 'agent_response_received') { if (agentStates[name]) agentStates[name].status = 'processing'; }
        if (l.eventType === 'agent_completed') { if (agentStates[name]) { agentStates[name].status = 'done'; agentStates[name].score = l.data.score; agentStates[name].issues = l.data.issues?.length || 0; } }
        if (l.eventType === 'agent_failed') { if (agentStates[name]) { agentStates[name].status = 'failed'; agentStates[name].error = l.data.error; } }
    });

    const statusLabels = {
        'running': ['Iniciando...', '#60a5fa', true],
        'waiting_llm': ['Esperando LLM...', '#a78bfa', true],
        'processing': ['Procesando respuesta...', '#22d3ee', true],
        'done': ['Completado', '#4ade80', false],
        'failed': ['Error', '#f87171', false]
    };

    invoked.forEach(name => {
        const state = agentStates[name];
        if (!state) {
            html += `<div style="padding:0.5rem 0;border-bottom:1px solid #1e293b;color:#475569;">
                <span style="font-weight:bold;">${name.toUpperCase()}</span> — Pendiente</div>`;
            return;
        }
        const [label, color, spinning] = statusLabels[state.status] || ['?', '#94a3b8', false];
        html += `<div style="padding:0.5rem 0;border-bottom:1px solid #1e293b;">
            <span style="font-weight:bold;color:${color};">`;
        if (spinning) html += `<span class="spinner"></span>`;
        else html += state.status === 'done' ? '&#10003; ' : '&#10007; ';
        html += `${name.toUpperCase()}</span> <span style="color:${color};font-size:0.85rem;">${label}</span>`;
        if (state.status === 'done') html += ` <span class="badge badge-agent">score: ${state.score}</span> <span style="color:#94a3b8;font-size:0.8rem;">${state.issues} issues</span>`;
        if (state.status === 'failed') html += ` <span style="color:#f87171;font-size:0.8rem;">${state.error || ''}</span>`;
        html += `</div>`;
    });

    const finalEvt = logs.find(l => l.eventType === 'final_result_generated');
    if (finalEvt) {
        html += `<div style="margin-top:1rem;padding-top:0.75rem;border-top:2px solid #334155;">
            <span class="ev-result">★ RESULTADO FINAL: </span>
            <span class="badge badge-status badge-${finalEvt.data.status}" style="font-size:0.85rem;">${finalEvt.data.status?.toUpperCase()}</span>
        </div>`;
    }

    body.innerHTML = html;
}

function buildResultFromLogs(executionId, logs, rowId, detailsId) {
    const selectedEvt = logs.find(l => l.eventType === 'selected_agents');
    const finalEvt = logs.find(l => l.eventType === 'final_result_generated');
    const agentCompleted = logs.filter(l => l.eventType === 'agent_completed' || l.eventType === 'agent_failed');
    const agentResponses = logs.filter(l => l.eventType === 'agent_response_received');
    const reqEvt = logs.find(l => l.eventType === 'request_received');

    const promptEvts = logs.filter(l => l.eventType === 'agent_prompt_sent');
    const storyText = reqEvt?.data?.storyText || (promptEvts.length > 0 ? '(ver prompt en log)' : '');
    const conflictEvts = logs.filter(l => l.eventType === 'conflict_detected');
    const resolutionEvts = logs.filter(l => l.eventType === 'supervisor_resolution');
    const conflicts = conflictEvts.flatMap(e => e.data?.conflicts || []);
    const resolutions = resolutionEvts.flatMap(e => e.data?.resolution || []);

    const r = {
        executionId,
        status: finalEvt?.data?.status || 'unknown',
        invokedAgents: selectedEvt?.data?.invoked || [],
        skippedAgents: selectedEvt?.data?.skipped || [],
        issues: [], conflicts: conflicts, resolutions: resolutions,
        provider: reqEvt?.data?.provider || '?',
        model: reqEvt?.data?.model || '?',
        title: reqEvt?.data?.title || '?',
        storyText: storyText,
        agentResults: agentCompleted.map(ac => {
            const agentName = ac.data?.agent || '?';
            const resp = agentResponses.find(r => r.data?.agent === agentName);
            let parsed = {};
            if (resp?.data?.response) {
                try { const t = resp.data.response; const s = t.indexOf('{'); const e = t.lastIndexOf('}'); if (s>=0 && e>s) parsed = JSON.parse(t.substring(s, e+1)); } catch(_){}
            }
            return {
                agent: agentName,
                status: ac.data?.status || (ac.eventType === 'agent_failed' ? 'error' : 'ok'),
                score: ac.data?.score || 0,
                issues: parsed.issues?.map(i => typeof i === 'string' ? i : (i.description || JSON.stringify(i))) || [],
                recommendations: parsed.recommendations?.map(r => typeof r === 'string' ? r : (r.description || JSON.stringify(r))) || [],
                questions: parsed.questions?.map(q => typeof q === 'string' ? q : (q.description || JSON.stringify(q))) || [],
                rawSummary: parsed.rawSummary || null
            };
        })
    };
    showResult(r, rowId, detailsId);
}

function showResult(r, rowId, detailsId) {
    let storyHtml = '';
    if (r.storyText) {
        storyHtml = `<div style="background:#0f172a;border:1px solid #334155;border-radius:0.5rem;padding:1rem;margin-bottom:1rem;">
            <div style="font-size:0.75rem;color:#64748b;text-transform:uppercase;margin-bottom:0.5rem;">Requerimiento enviado</div>
            ${r.title ? `<div style="font-weight:bold;color:#f1f5f9;margin-bottom:0.5rem;">${escHtml(r.title)}</div>` : ''}
            <div style="font-size:0.85rem;color:#cbd5e1;line-height:1.6;white-space:pre-wrap;">${escHtml(r.storyText)}</div>
        </div>`;
    }
    document.getElementById(rowId).innerHTML = storyHtml + `
        <div class="result-item"><label>Status</label><span class="badge badge-status badge-${r.status}">${r.status.toUpperCase()}</span></div>
        <div class="result-item"><label>Agentes invocados</label><span>${r.invokedAgents.join(', ')}</span></div>
        <div class="result-item"><label>Omitidos</label><span>${r.skippedAgents.map(s => s.agent).join(', ') || 'ninguno'}</span></div>
        <div class="result-item"><label>Issues</label><span>${r.issues.length}</span></div>
        <div class="result-item"><label>Conflictos</label><span>${r.conflicts.length}</span></div>
        <div class="result-item"><label>Modelo</label><span>${r.provider} / ${r.model}</span></div>
        <div class="result-item"><label>Execution ID</label><span class="mono">${r.executionId}</span></div>
    `;
    const details = document.getElementById(detailsId);
    details.innerHTML = '';
    if (r.agentResults && r.agentResults.length > 0) {
        r.agentResults.forEach(a => {
            const statusIcon = a.status === 'ok' ? '&#10003;' : (a.status === 'parse_error' ? '⚠' : '&#10007;');
            const statusColor = a.status === 'ok' ? '#4ade80' : (a.status === 'parse_error' ? '#fb923c' : '#f87171');
            const statusLabel = a.status === 'parse_error' ? ' <span style="color:#fb923c;font-size:0.7rem;">(parse error)</span>' : '';
            details.innerHTML += `
                <div class="agent-card"${a.status === 'parse_error' ? ' style="border-color:#fb923c40;"' : ''}>
                    <h4><span style="color:${statusColor}">${statusIcon}</span> ${a.agent.toUpperCase()} <span class="badge badge-agent">score: ${a.score}</span>${statusLabel}</h4>
                    ${a.status === 'parse_error' ? `<div style="margin-bottom:0.5rem;padding:0.5rem;background:#7f1d1d20;border-radius:0.25rem;"><strong style="color:#fb923c;font-size:0.8rem;">El modelo no devolvio JSON valido.</strong><div style="font-size:0.75rem;color:#94a3b8;margin-top:0.25rem;">Ver el log completo para inspeccionar la respuesta raw del LLM.</div></div>` : ''}
                    ${a.issues.length > 0 ? `<div style="margin-bottom:0.5rem;"><strong style="color:#f87171;font-size:0.8rem;">Issues:</strong><ul class="detail-list">${a.issues.map(i => `<li>${escHtml(i)}</li>`).join('')}</ul></div>` : ''}
                    ${a.recommendations.length > 0 ? `<div style="margin-bottom:0.5rem;"><strong style="color:#60a5fa;font-size:0.8rem;">Recomendaciones:</strong><ul class="detail-list">${a.recommendations.map(r => `<li>${escHtml(r)}</li>`).join('')}</ul></div>` : ''}
                    ${a.questions && a.questions.length > 0 ? `<div><strong style="color:#facc15;font-size:0.8rem;">Preguntas:</strong><ul class="detail-list">${a.questions.map(q => `<li>${escHtml(q)}</li>`).join('')}</ul></div>` : ''}
                    ${a.rawSummary ? `<div style="margin-top:0.5rem;font-size:0.75rem;color:#64748b;">Summary: ${escHtml(a.rawSummary).substring(0, 300)}${a.rawSummary.length > 300 ? '...' : ''}</div>` : ''}
                </div>
            `;
        });
    }
    if (r.conflicts && r.conflicts.length > 0) {
        let supHtml = `<div style="margin-top:1.5rem;background:#1a1a2e;border:1px solid #fb923c40;border-radius:0.75rem;padding:1.25rem;">`;
        supHtml += `<h3 style="margin:0 0 1rem 0;font-size:1.1rem;color:#fb923c;">\u26a1 Intervencion del Supervisor</h3>`;
        supHtml += `<div style="margin-bottom:1rem;"><div style="font-size:0.75rem;color:#94a3b8;text-transform:uppercase;margin-bottom:0.5rem;">Conflictos detectados</div>`;
        r.conflicts.forEach(c => {
            supHtml += `<div style="padding:0.5rem 0.75rem;margin-bottom:0.5rem;background:#7f1d1d20;border-left:3px solid #f87171;border-radius:0.25rem;color:#fca5a5;font-size:0.85rem;">${escHtml(c)}</div>`;
        });
        supHtml += `</div>`;
        if (r.resolutions && r.resolutions.length > 0) {
            supHtml += `<div><div style="font-size:0.75rem;color:#94a3b8;text-transform:uppercase;margin-bottom:0.5rem;">Resoluciones del supervisor</div>`;
            r.resolutions.forEach(res => {
                supHtml += `<div style="padding:0.5rem 0.75rem;margin-bottom:0.5rem;background:#16532520;border-left:3px solid #4ade80;border-radius:0.25rem;color:#86efac;font-size:0.85rem;">${escHtml(res)}</div>`;
            });
            supHtml += `</div>`;
        }
        supHtml += `</div>`;
        details.innerHTML += supHtml;
    }
}

function escHtml(s) { const d = document.createElement('div'); d.textContent = s; return d.innerHTML; }

async function loadLog(executionId, titleId, bodyId) {
    document.getElementById(titleId).textContent = `Log: ${executionId}`;
    const res = await fetch(`${BASE}/executions/${executionId}/log/text`);
    const text = await res.text();
    const highlighted = text
        .replace(/(▶ REQUEST RECEIVED)/g, '<span class="ev-request">$1</span>')
        .replace(/(⚙ SUPERVISOR STARTED)/g, '<span class="ev-supervisor">$1</span>')
        .replace(/(🎯 AGENT SELECTION)/g, '<span class="ev-selection">$1</span>')
        .replace(/(┌─ AGENT \[.*?\] STARTED)/g, '<span class="ev-agent-start">$1</span>')
        .replace(/(PROMPT SENT[^:]*:)/g, '<span class="ev-supervisor">$1</span>')
        .replace(/(LLM RESPONSE[^:]*:)/g, '<span class="ev-agent-ok">$1</span>')
        .replace(/(┊\s*[┌└]─+)/g, '<span class="ev-box">$1</span>')
        .replace(/(┊\s*│.*)/g, '<span class="ev-prompt">$1</span>')
        .replace(/(└─ AGENT \[.*?\] COMPLETED.*)/g, '<span class="ev-agent-ok">$1</span>')
        .replace(/(└─ AGENT \[.*?\] FAILED.*)/g, '<span class="ev-agent-fail">$1</span>')
        .replace(/(⚡ CONFLICTS DETECTED)/g, '<span class="ev-conflict">$1</span>')
        .replace(/(✔ SUPERVISOR RESOLUTION)/g, '<span class="ev-conflict">$1</span>')
        .replace(/(★ FINAL RESULT)/g, '<span class="ev-result">$1</span>')
        .replace(/(■ COMPLETED.*)/g, '<span class="ev-done">$1</span>')
        .replace(/(\[\d{2}:\d{2}:\d{2}\.\d{3}\])/g, '<span class="ev-time">$1</span>');
    document.getElementById(bodyId).innerHTML = highlighted;
}

async function loadExecutions() {
    const res = await fetch(`${BASE}/executions`);
    const execs = await res.json();
    const container = document.getElementById('execList');
    if (execs.length === 0) {
        container.innerHTML = '<div class="empty-state">No hay ejecuciones anteriores</div>';
        return;
    }
    let html = '<table class="exec-table"><thead><tr><th>Execution ID</th><th>Historia</th><th>Status</th><th>Tiempo</th><th>Eventos</th></tr></thead><tbody>';
    execs.forEach(e => {
        const secs = (e.totalMs / 1000).toFixed(1);
        html += `<tr onclick="viewExecution('${e.executionId}', this)">
            <td class="mono">${e.executionId}</td>
            <td>${e.title || e.storyId || '-'}</td>
            <td><span class="badge badge-status badge-${e.status}">${e.status}</span></td>
            <td>${secs}s</td>
            <td>${e.eventCount}</td>
        </tr>`;
    });
    html += '</tbody></table>';
    container.innerHTML = html;
}

async function viewExecution(executionId, row) {
    if (row) {
        document.querySelectorAll('.exec-table tr').forEach(r => r.classList.remove('selected'));
        row.classList.add('selected');
    }
    document.getElementById('execIdInput').value = executionId;
    document.getElementById('historyResultSection').style.display = 'block';
    document.getElementById('histLogBody').innerHTML = '<span class="ev-supervisor">Cargando...</span>';

    const logsRes = await fetch(`${BASE}/executions/${executionId}/log`);
    const logs = await logsRes.json();

    buildResultFromLogs(executionId, logs, 'histResultRow', 'histAgentDetails');
    await loadLog(executionId, 'histLogTitle', 'histLogBody');
}

async function lookupExecution() {
    const id = document.getElementById('execIdInput').value.trim();
    if (!id) return;
    await viewExecution(id, null);
}

document.getElementById('execIdInput').addEventListener('keydown', e => { if (e.key === 'Enter') lookupExecution(); });

loadMocks();
</script>
</body>
</html>
""";
        return Results.Content(html, "text/html");
    }
}
