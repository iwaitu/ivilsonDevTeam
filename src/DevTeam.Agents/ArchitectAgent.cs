using DevTeam.Core;
using Microsoft.Extensions.AI;

namespace DevTeam.Agents;

public sealed class ArchitectAgent(IChatClient chatClient) : DevAgentBase(chatClient)
{
    public override string Name => "ArchitectAgent";
    public override AgentRole Role => AgentRole.Architect;

    public override async Task<AgentResult> ExecuteAsync(AgentExecutionContext context)
    {
        var nextTask = context.Memory.WorkItems
            .Where(x => x.AssignedRole == AgentRole.Architect && x.Status == WorkItemStatus.Pending)
            .OrderBy(x => x.Priority)
            .FirstOrDefault();

        var userPrompt = nextTask is null
            ? $"""
基于以下需求分析结果，输出一份架构设计说明。

任务类型:
{context.Memory.TaskCategory}

需求目标:
{context.Memory.RequirementGoal}

需求摘要:
{context.Memory.RequirementSummary}

约束:
{context.Memory.Constraints}

验收标准:
{context.Memory.AcceptanceCriteria}

风险:
{context.Memory.Risks}

请严格输出 Markdown，包含以下一级标题：
- # 模块拆分
- # 接口契约
- # 任务 DAG
- # 设计说明
"""
            : $"""
请完成以下架构任务，并只输出一份 Markdown 结果。

任务标题:
{nextTask.Title}

任务目标:
{nextTask.Goal}

优先级:
{nextTask.Priority}

请严格输出 Markdown，包含以下一级标题：
- # 任务设计
- # 接口契约
- # 风险与回滚
""";

        const string systemPrompt = """
你是资深系统架构师。
你只负责设计，不写实现代码。
输出要可执行、可拆任务，不要空泛套话。
每次只产出一份结构化 Markdown 设计结果。
""";
        try
        {
            var output = await AskAsync(systemPrompt, userPrompt, context.CancellationToken);

            if (nextTask is not null)
            {
                nextTask.Status = WorkItemStatus.Completed;
            }

            return new AgentResult { AgentName = Name, Output = output };
        }
        catch (Exception ex)
        {
            return new AgentResult
            {
                AgentName = Name,
                Output = $"发生错误: {ex.Message}。\n\n请检查上述内容，确保信息完整且格式正确。"
            };
        }
    }
}
