using DevTeam.Core;
using Microsoft.Extensions.AI;

namespace DevTeam.Agents;

public sealed class ReviewerAgent(IChatClient chatClient) : DevAgentBase(chatClient)
{
    public override string Name => "ReviewerAgent";
    public override AgentRole Role => AgentRole.Reviewer;

    public override async Task<AgentResult> ExecuteAsync(AgentExecutionContext context)
    {
        var nextTask = context.Memory.WorkItems
            .Where(x => x.AssignedRole == AgentRole.Reviewer && x.Status == WorkItemStatus.Pending)
            .OrderBy(x => x.Priority)
            .FirstOrDefault();

        if (nextTask is null)
        {
            return new AgentResult
            {
                AgentName = Name,
                Output = "没有待执行的审查任务。"
            };
        }

        nextTask.Status = WorkItemStatus.InProgress;

        var userPrompt = $$"""
请执行以下代码审查任务，并只输出一份 Markdown 结果。

任务标题: {{nextTask.Title}}
任务目标: {{nextTask.Goal}}
任务优先级: {{nextTask.Priority}}

关注文件:
{{FormatList(nextTask.FileHints)}}

关注约束:
{{FormatList(nextTask.Constraints)}}

关联测试:
{{FormatList(nextTask.TestsToRun)}}

最近代码变更:
{{FormatList(context.Memory.CodeChanges)}}

请重点检查：
- 接口兼容性
- 越权访问
- 并发与空值处理
- 副作用与复杂度债务

请严格输出 Markdown，包含以下一级标题：
- # 审查结论
- # 发现的问题
- # 风险等级
- # 建议动作
""";

        const string systemPrompt = """
你是资深代码审查员。
你的职责是找副作用、兼容性问题和隐藏风险，而不是重写代码。
输出要具体、可执行。
每次只产出一份结构化 Markdown 审查结果。
""";

        try
        {
            var output = await AskAsync(systemPrompt, userPrompt, context.CancellationToken);
            nextTask.Status = WorkItemStatus.Completed;

            return new AgentResult { AgentName = Name, Output = output };
        }
        catch
        {
            nextTask.Status = WorkItemStatus.Failed;
            throw;
        }
    }

    private static string FormatList(IEnumerable<string> items)
    {
        var values = items.Where(static x => !string.IsNullOrWhiteSpace(x)).ToArray();
        return values.Length == 0 ? "- 无" : string.Join('\n', values.Select(static x => $"- {x}"));
    }
}
