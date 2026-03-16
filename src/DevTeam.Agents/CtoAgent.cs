using DevTeam.Core;
using Microsoft.Extensions.AI;

namespace DevTeam.Agents;

public sealed class CtoAgent(IChatClient chatClient) : DevAgentBase(chatClient)
{
    public override string Name => "CtoAgent";
    public override AgentRole Role => AgentRole.Cto;

    public override async Task<AgentResult> ExecuteAsync(AgentExecutionContext context)
    {
        var userPrompt = $$"""
你要根据当前信息生成开发计划。

用户原始请求:
{{context.UserRequest}}

任务类型:
{{context.Memory.TaskCategory}}

需求目标:
{{context.Memory.RequirementGoal}}

需求摘要:
{{context.Memory.RequirementSummary}}

约束:
{{context.Memory.Constraints}}

验收标准:
{{context.Memory.AcceptanceCriteria}}

风险:
{{context.Memory.Risks}}

架构方案:
{{context.Memory.ArchitecturePlan}}

接口契约:
{{context.Memory.InterfaceContracts}}

请输出 JSON：
{
  "epic": "...",
  "taskType": "feature|bugfix|refactor|docs|tests",
  "decision": "...",
  "routingDecision": "...",
  "rollbackStrategy": "...",
  "workItems": [
    {
      "id": "W1",
      "title": "...",
      "goal": "...",
      "assignedRole": "Architect|Coder|Reviewer",
      "priority": 1,
      "dependsOn": [],
      "fileHints": [],
      "constraints": [],
      "testsToRun": [],
      "rollbackStrategy": "..."
    }
  ]
}
""";

        const string systemPrompt = """
你是研发团队 CTO。
你负责做任务拆分、优先级安排和跨角色协调。
输出必须利于程序执行，不要写成散文。
优先输出任务 DAG、优先级、角色分派、回滚策略。
""";

        var output = await AskAsync(systemPrompt, userPrompt, context.CancellationToken);
        return new AgentResult { AgentName = Name, Output = output };
    }

    public async Task<AgentResult> ExecuteFinalAsync(AgentExecutionContext context)
    {
        var userPrompt = $$"""
请基于当前研发过程给出最终 CTO 决策。

需求摘要:
{{context.Memory.RequirementSummary}}

架构方案:
{{context.Memory.ArchitecturePlan}}

任务执行情况:
{{FormatWorkItems(context.Memory.WorkItems)}}

代码变更记录:
{{FormatList(context.Memory.CodeChanges)}}

审查结论:
{{FormatList(context.Memory.ReviewFindings)}}

测试执行记录:
{{FormatList(context.Memory.TestExecutionLog)}}

请输出 Markdown，包含：
- 最终决策（合并 / 打回 / 重规划）
- 变更摘要
- 剩余风险
- 需要人工确认项
""";

        const string systemPrompt = """
你是研发团队 CTO。
你的职责是在任务执行后收口，基于事实给出合并、打回或重规划决策。
输出简洁、明确、可执行。
""";

        var output = await AskAsync(systemPrompt, userPrompt, context.CancellationToken);
        return new AgentResult { AgentName = Name, Output = output };
    }

    private static string FormatList(IEnumerable<string> items)
    {
        var values = items.Where(static x => !string.IsNullOrWhiteSpace(x)).ToArray();
        return values.Length == 0 ? "- 无" : string.Join('\n', values.Select(static x => $"- {x}"));
    }

    private static string FormatWorkItems(IEnumerable<WorkItem> workItems)
    {
        var values = workItems
            .Select(static item => $"- {item.Id} | {item.AssignedRole} | P{item.Priority} | {item.Status} | {item.Title}")
            .ToArray();

        return values.Length == 0 ? "- 无" : string.Join('\n', values);
    }
}
