namespace DevTeam.Core;

public sealed class ProjectMemory
{
    public string TaskCategory { get; set; } = string.Empty;
    public string RequirementGoal { get; set; } = string.Empty;
    public string RequirementSummary { get; set; } = string.Empty;
    public string Constraints { get; set; } = string.Empty;
    public string AcceptanceCriteria { get; set; } = string.Empty;
    public string Risks { get; set; } = string.Empty;
    public string RoutingDecision { get; set; } = string.Empty;
    public string ArchitecturePlan { get; set; } = string.Empty;
    public string InterfaceContracts { get; set; } = string.Empty;
    public string TaskDag { get; set; } = string.Empty;
    public string CtoDecisionLog { get; set; } = string.Empty;
    public string RollbackStrategy { get; set; } = string.Empty;
    public string FinalDecision { get; set; } = string.Empty;
    public string FinalSummary { get; set; } = string.Empty;
    public List<WorkItem> WorkItems { get; set; } = [];
    public List<string> RepoMap { get; set; } = [];
    public List<string> PatchHistory { get; set; } = [];
    public List<string> ReviewFindings { get; set; } = [];
    public List<string> TestExecutionLog { get; set; } = [];
    public List<string> CodeChanges { get; set; } = [];
}
