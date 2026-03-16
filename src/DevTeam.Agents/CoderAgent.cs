using DevTeam.Core;
using Microsoft.Extensions.AI;

namespace DevTeam.Agents;

public sealed class CoderAgent(IChatClient chatClient) : DevAgentBase(chatClient)
{
    public override string Name => "CoderAgent";
    public override AgentRole Role => AgentRole.Coder;

    public override async Task<AgentResult> ExecuteAsync(AgentExecutionContext context)
    {
        var nextTask = context.Memory.WorkItems
            .Where(x => x.AssignedRole == AgentRole.Coder && x.Status == WorkItemStatus.Pending)
            .OrderBy(x => x.Priority)
            .FirstOrDefault();

        if (nextTask is null)
        {
            return new AgentResult
            {
                AgentName = Name,
                Output = "没有待执行的编码任务。"
            };
        }

        nextTask.Status = WorkItemStatus.InProgress;

        var userPrompt = $"""
请完成以下开发任务，并只输出一份 Markdown 结果：

任务标题: {nextTask.Title}
任务目标: {nextTask.Goal}
任务优先级: {nextTask.Priority}

文件提示:
{FormatList(nextTask.FileHints)}

任务约束:
{FormatList(nextTask.Constraints)}

建议执行测试:
{FormatList(nextTask.TestsToRun)}

回滚策略:
{nextTask.RollbackStrategy}

架构方案:
{context.Memory.ArchitecturePlan}

请严格输出 Markdown，包含以下一级标题：
- # 实现思路
- # 关键代码
- # 修改文件
- # 测试点
""";

        const string systemPrompt = """
你是高级开发工程师。
你的目标是完成单个任务，不要重写整个系统。
优先给出最小可行修改方案。
每次只产出一份结构化 Markdown 开发结果。
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
