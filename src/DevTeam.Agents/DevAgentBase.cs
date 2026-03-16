using DevTeam.Core;
using Microsoft.Extensions.AI;

namespace DevTeam.Agents;

public abstract class DevAgentBase : IDevAgent
{
    protected readonly IChatClient ChatClient;

    protected DevAgentBase(IChatClient chatClient)
    {
        ChatClient = chatClient;
    }

    public abstract string Name { get; }
    public abstract AgentRole Role { get; }

    protected async Task<string> AskAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        List<ChatMessage> messages =
        [
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, userPrompt)
        ];

        var response = await ChatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
        return response.Text ?? string.Empty;
    }

    public abstract Task<AgentResult> ExecuteAsync(AgentExecutionContext context);
}
