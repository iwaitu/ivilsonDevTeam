namespace DevTeam.Core;

public sealed class AgentExecutionContext
{
    public required string UserRequest { get; init; }
    public required ProjectMemory Memory { get; init; }
    public required CancellationToken CancellationToken { get; init; }
}

public sealed class AgentResult
{
    public required string AgentName { get; init; }
    public required string Output { get; init; }
    public bool Success { get; init; } = true;
}

public interface IDevAgent
{
    string Name { get; }
    AgentRole Role { get; }

    Task<AgentResult> ExecuteAsync(AgentExecutionContext context);
}
