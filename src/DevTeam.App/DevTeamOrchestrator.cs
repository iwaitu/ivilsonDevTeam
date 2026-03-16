using System.Text.Json;
using DevTeam.Agents;
using DevTeam.Core;
using DevTeam.Infrastructure;

namespace DevTeam.App;

public sealed class DevTeamOrchestrator
{
    private readonly AnalystAgent _analyst;
    private readonly ArchitectAgent _architect;
    private readonly CtoAgent _cto;
    private readonly CoderAgent _coder;
    private readonly ReviewerAgent _reviewer;
    private readonly IProjectMemoryStore _memoryStore;

    public DevTeamOrchestrator(
        AnalystAgent analyst,
        ArchitectAgent architect,
        CtoAgent cto,
        CoderAgent coder,
        ReviewerAgent reviewer,
        IProjectMemoryStore memoryStore)
    {
        _analyst = analyst;
        _architect = architect;
        _cto = cto;
        _coder = coder;
        _reviewer = reviewer;
        _memoryStore = memoryStore;
    }

    public async Task<ProjectMemory> RunAsync(string projectId, string request, CancellationToken cancellationToken = default)
    {
        var memory = await _memoryStore.GetAsync(projectId, cancellationToken);

        var ctx = new AgentExecutionContext
        {
            UserRequest = request,
            Memory = memory,
            CancellationToken = cancellationToken
        };

        var analystResult = await _analyst.ExecuteAsync(ctx);
        ApplyAnalystResult(memory, analystResult.Output);

        var architectResult = await _architect.ExecuteAsync(ctx);
        ApplyArchitectResult(memory, architectResult.Output);

        var ctoResult = await _cto.ExecuteAsync(ctx);
        ApplyCtoResult(memory, ctoResult.Output);

        while (TryGetNextReadyWorkItem(memory, out var workItem))
        {
            switch (workItem!.AssignedRole)
            {
                case AgentRole.Architect:
                {
                    var result = await _architect.ExecuteAsync(ctx);
                    memory.ArchitecturePlan = AppendSection(memory.ArchitecturePlan, result.Output);
                    memory.PatchHistory.Add($"Architect:{workItem.Id}");
                    break;
                }
                case AgentRole.Coder:
                {
                    var result = await _coder.ExecuteAsync(ctx);
                    memory.CodeChanges.Add(result.Output);
                    memory.PatchHistory.Add($"Coder:{workItem.Id}");
                    break;
                }
                case AgentRole.Reviewer:
                {
                    var result = await _reviewer.ExecuteAsync(ctx);
                    memory.ReviewFindings.Add(result.Output);
                    memory.PatchHistory.Add($"Reviewer:{workItem.Id}");
                    break;
                }
                default:
                    workItem.Status = WorkItemStatus.Blocked;
                    break;
            }
        }

        if (memory.TestExecutionLog.Count == 0)
        {
            memory.TestExecutionLog.Add("原型阶段未接入独立测试执行器，使用外部 build/test 流程验证。");
        }

        var finalResult = await _cto.ExecuteFinalAsync(ctx);
        memory.FinalSummary = finalResult.Output;
        memory.FinalDecision = ExtractFinalDecision(finalResult.Output);

        await _memoryStore.SaveAsync(projectId, memory, cancellationToken);
        return memory;
    }

    private static bool TryGetNextReadyWorkItem(ProjectMemory memory, out WorkItem? workItem)
    {
        workItem = memory.WorkItems
            .Where(x => x.Status == WorkItemStatus.Pending)
            .Where(x => x.DependsOn.All(dep => memory.WorkItems.Any(done => done.Id == dep && done.Status == WorkItemStatus.Completed)))
            .OrderBy(x => x.Priority)
            .FirstOrDefault();

        return workItem is not null;
    }

    private static void ApplyAnalystResult(ProjectMemory memory, string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        memory.TaskCategory = GetString(root, "taskCategory");
        memory.RequirementGoal = GetString(root, "goal");
        memory.RequirementSummary = GetString(root, "requirementSummary");
        memory.Constraints = FormatArray(root, "constraints");
        memory.AcceptanceCriteria = FormatArray(root, "acceptanceCriteria");
        memory.Risks = FormatArray(root, "risks");
    }

    private static void ApplyArchitectResult(ProjectMemory memory, string markdown)
    {
        memory.ArchitecturePlan = markdown;
        memory.InterfaceContracts = ExtractMarkdownSection(markdown, "接口契约");
        memory.TaskDag = ExtractMarkdownSection(markdown, "任务 DAG");
    }

    private static void ApplyCtoResult(ProjectMemory memory, string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        memory.RequirementGoal = string.IsNullOrWhiteSpace(memory.RequirementGoal)
            ? GetString(root, "epic")
            : memory.RequirementGoal;
        memory.TaskCategory = string.IsNullOrWhiteSpace(memory.TaskCategory)
            ? GetString(root, "taskType")
            : memory.TaskCategory;
        memory.CtoDecisionLog = GetString(root, "decision");
        memory.RoutingDecision = GetString(root, "routingDecision");
        memory.RollbackStrategy = GetString(root, "rollbackStrategy");

        List<WorkItem> workItems = [];
        foreach (var item in root.GetProperty("workItems").EnumerateArray())
        {
            workItems.Add(new WorkItem
            {
                Id = item.GetProperty("id").GetString() ?? Guid.NewGuid().ToString("N"),
                Title = item.GetProperty("title").GetString() ?? string.Empty,
                Goal = item.GetProperty("goal").GetString() ?? string.Empty,
                AssignedRole = Enum.Parse<AgentRole>(item.GetProperty("assignedRole").GetString() ?? nameof(AgentRole.Coder), true),
                Priority = item.TryGetProperty("priority", out var priority) && priority.TryGetInt32(out var priorityValue)
                    ? priorityValue
                    : int.MaxValue,
                DependsOn = item.TryGetProperty("dependsOn", out var deps)
                    ? deps.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(x => x.Length > 0).ToList()
                    : [],
                FileHints = item.TryGetProperty("fileHints", out var fileHints)
                    ? fileHints.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(x => x.Length > 0).ToList()
                    : [],
                Constraints = item.TryGetProperty("constraints", out var constraints)
                    ? constraints.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(x => x.Length > 0).ToList()
                    : [],
                TestsToRun = item.TryGetProperty("testsToRun", out var testsToRun)
                    ? testsToRun.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(x => x.Length > 0).ToList()
                    : [],
                RollbackStrategy = item.TryGetProperty("rollbackStrategy", out var rollback)
                    ? rollback.GetString() ?? string.Empty
                    : string.Empty
            });
        }

        memory.WorkItems = workItems;
        memory.TaskDag = string.Join('\n', workItems.Select(static item => $"- {item.Id} -> [{item.AssignedRole}] {item.Title}"));
    }

    private static string GetString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var element)
            ? element.GetString() ?? string.Empty
            : string.Empty;
    }

    private static string FormatArray(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var element)
            ? string.Join('\n', element.EnumerateArray().Select(static x => $"- {x.GetString() ?? string.Empty}"))
            : string.Empty;
    }

    private static string ExtractMarkdownSection(string markdown, string heading)
    {
        var marker = $"# {heading}";
        var start = markdown.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
        {
            return string.Empty;
        }

        start += marker.Length;
        var next = markdown.IndexOf("# ", start, StringComparison.Ordinal);
        var length = next < 0 ? markdown.Length - start : next - start;
        return markdown.Substring(start, length).Trim();
    }

    private static string AppendSection(string existing, string addition)
    {
        return string.IsNullOrWhiteSpace(existing)
            ? addition
            : existing + Environment.NewLine + Environment.NewLine + addition;
    }

    private static string ExtractFinalDecision(string markdown)
    {
        if (markdown.Contains("合并", StringComparison.Ordinal))
        {
            return "合并";
        }

        if (markdown.Contains("打回", StringComparison.Ordinal))
        {
            return "打回";
        }

        if (markdown.Contains("重规划", StringComparison.Ordinal))
        {
            return "重规划";
        }

        return string.Empty;
    }
}
