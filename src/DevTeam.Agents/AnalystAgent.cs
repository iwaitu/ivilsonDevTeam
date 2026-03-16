using DevTeam.Core;
using Microsoft.Extensions.AI;

namespace DevTeam.Agents;

public sealed class AnalystAgent(IChatClient chatClient) : DevAgentBase(chatClient)
{
    public override string Name => "AnalystAgent";
    public override AgentRole Role => AgentRole.Analyst;

    public override async Task<AgentResult> ExecuteAsync(AgentExecutionContext context)
    {
        const string systemPrompt = """
你是软件研发团队中的分析师。
你的任务不是写代码，而是把输入整理成单一结构化分析结果。
你的任务只包括：
1. 任务类型
2. 需求目标
3. 需求摘要
4. 业务约束
5. 验收标准
6. 风险点

请严格输出 JSON：
{
  "taskCategory": "feature|bugfix|refactor|docs|tests",
  "goal": "...",
  "requirementSummary": "...",
  "constraints": ["..."],
  "acceptanceCriteria": ["..."],
  "risks": ["..."]
}
""";

        var output = await AskAsync(systemPrompt, context.UserRequest, context.CancellationToken);
        return new AgentResult { AgentName = Name, Output = output };
    }
}
