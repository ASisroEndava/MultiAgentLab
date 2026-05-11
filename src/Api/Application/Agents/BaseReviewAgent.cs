using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MultiAgentLab.Api.Domain;
using MultiAgentLab.Api.Infrastructure.LLM;
using MultiAgentLab.Api.Infrastructure.Logging;

namespace MultiAgentLab.Api.Application.Agents;

public abstract class BaseReviewAgent : IReviewAgent
{
    private readonly IModelRouter _modelRouter;
    private readonly IExecutionLogger _logger;

    protected BaseReviewAgent(IModelRouter modelRouter, IExecutionLogger logger)
    {
        _modelRouter = modelRouter;
        _logger = logger;
    }

    public abstract string Name { get; }

    protected abstract string SystemPrompt { get; }

    public async Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken = default)
    {
        var prompt = BuildPrompt(context);

        if (context.Logging.IncludePrompts)
        {
            await _logger.LogAsync(LogEvents.AgentPromptSent(context.ExecutionId, Name, prompt), cancellationToken);
        }

        var client = _modelRouter.Resolve(context.Provider);
        var response = await client.GenerateAsync(new ModelRequest
        {
            Prompt = prompt,
            Provider = context.Provider
        }, cancellationToken);

        if (context.Logging.IncludeResponses)
        {
            await _logger.LogAsync(LogEvents.AgentResponseReceived(context.ExecutionId, Name, response.Text), cancellationToken);
        }

        return ParseResponse(response.Text);
    }

    protected virtual string BuildPrompt(AgentContext context)
    {
        return $$"""
                {{SystemPrompt}}

                --- Story to review ---
                Title: {{context.Title}}
                ID: {{context.StoryId}}
                Text: {{context.StoryText}}

                Respond exclusively in JSON with this format:
                {
                  "issues": [],
                  "recommendations": [],
                  "questions": [],
                  "rawSummary": ""
                }
                """;
    }

    protected AgentResult ParseResponse(string responseText)
    {
        try
        {
            var jsonStart = responseText.IndexOf('{');
            var jsonEnd = responseText.LastIndexOf('}');
// 
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonStr = responseText[jsonStart..(jsonEnd + 1)];

                var doc = TryParseJson(jsonStr) ?? TryParseJson(RepairJson(jsonStr));

                if (doc != null)
                {
                    var root = doc.RootElement;
                    var issues = ExtractStringArray(root, "issues");
                    var recommendations = ExtractStringArray(root, "recommendations");
                    var questions = ExtractStringArray(root, "questions");
                    var rawSummary = root.TryGetProperty("rawSummary", out var rs) ? rs.GetString() : null;

                    return new AgentResult
                    {
                        Agent = Name,
                        Status = "ok",
                        Score = CalculateScore(issues.Count),
                        Issues = issues,
                        Recommendations = recommendations,
                        Questions = questions,
                        RawSummary = rawSummary
                    };
                }
            }
        }
        catch
        {
            // Fall through to fallback
        }

        return new AgentResult
        {
            Agent = Name,
            Status = "parse_error",
            Score = 0,
            Issues = new List<string> { "Could not parse the model response" },
            Recommendations = new(),
            Questions = new(),
            RawSummary = responseText
        };
    }

    private static JsonDocument? TryParseJson(string json)
    {
        try
        {
            return JsonDocument.Parse(json);
        }
        catch
        {
            return null;
        }
    }

    private static string RepairJson(string json)
    {
        var repaired = json;

        // Fix unescaped newlines inside JSON string values
        repaired = FixNewlinesInStrings(repaired);

        // Fix missing commas between elements
        repaired = Regex.Replace(repaired, @"""(\s*\n\s*)""", @""",${1}""");
        repaired = Regex.Replace(repaired, @"\}(\s*\n\s*)\{", @"},${1}{");
        repaired = Regex.Replace(repaired, @"\}(\s*\n\s*)""", @"},${1}""");
        repaired = Regex.Replace(repaired, @"\](\s*\n\s*)""", @"],${1}""");

        // Fix unescaped double quotes inside string values (e.g. a "word" inside a string)
        repaired = FixUnescapedQuotes(repaired);

        return repaired;
    }

    private static string FixNewlinesInStrings(string json)
    {
        var sb = new StringBuilder(json.Length);
        bool inString = false;
        bool escaped = false;

        for (int i = 0; i < json.Length; i++)
        {
            char c = json[i];

            if (escaped) { sb.Append(c); escaped = false; continue; }

            if (c == '\\' && inString) { sb.Append(c); escaped = true; continue; }

            if (c == '"') { inString = !inString; sb.Append(c); continue; }

            if (inString && (c == '\n' || c == '\r'))
            {
                if (c == '\r' && i + 1 < json.Length && json[i + 1] == '\n') i++;
                sb.Append("\\n");
                continue;
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    private static string FixUnescapedQuotes(string json)
    {
        var sb = new StringBuilder(json.Length);
        bool inString = false;
        bool escaped = false;

        for (int i = 0; i < json.Length; i++)
        {
            char c = json[i];

            if (escaped) { sb.Append(c); escaped = false; continue; }

            if (c == '\\' && inString) { sb.Append(c); escaped = true; continue; }

            if (c == '"')
            {
                if (!inString)
                {
                    inString = true;
                    sb.Append(c);
                }
                else
                {
                    // Check if this quote ends the string or is an unescaped interior quote
                    var rest = json.AsSpan(i + 1).TrimStart();
                    if (rest.Length == 0 || rest[0] == ',' || rest[0] == '}' || rest[0] == ']' || rest[0] == ':')
                    {
                        inString = false;
                        sb.Append(c);
                    }
                    else
                    {
                        sb.Append("\\\"");
                    }
                }
                continue;
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    private static List<string> ExtractStringArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var arr) || arr.ValueKind != JsonValueKind.Array)
            return new();

        var result = new List<string>();
        foreach (var item in arr.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                result.Add(item.GetString() ?? "");
            }
            else if (item.ValueKind == JsonValueKind.Object)
            {
                if (item.TryGetProperty("description", out var desc))
                    result.Add(desc.GetString() ?? "");
                else
                    result.Add(item.ToString());
            }
        }
        return result;
    }

    private static double CalculateScore(int issueCount)
    {
        if (issueCount == 0) return 1.0;
        if (issueCount <= 2) return 0.7;
        if (issueCount <= 4) return 0.5;
        return 0.3;
    }
}
