namespace DevTeam.Core;

public sealed class WorkItem
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Goal { get; init; }
    public AgentRole AssignedRole { get; set; }
    public int Priority { get; set; }
    public WorkItemStatus Status { get; set; } = WorkItemStatus.Pending;
    public List<string> DependsOn { get; init; } = [];
    public List<string> FileHints { get; init; } = [];
    public List<string> Constraints { get; init; } = [];
    public List<string> TestsToRun { get; init; } = [];
    public string RollbackStrategy { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; init; } = new();
}
