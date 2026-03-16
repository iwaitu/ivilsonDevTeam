using DevTeam.Agents;
using DevTeam.Core;
using DevTeam.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevTeam.Tests;

public class UnitTest1
{
    [Fact]
    public async Task InMemoryProjectMemoryStore_ReturnsSameMemoryForSameProject()
    {
        var store = new InMemoryProjectMemoryStore();

        var memory = await store.GetAsync("demo-project");
        memory.RequirementSummary = "summary";

        await store.SaveAsync("demo-project", memory);
        var loaded = await store.GetAsync("demo-project");

        Assert.Same(memory, loaded);
        Assert.Equal("summary", loaded.RequirementSummary);
    }

    [Fact]
    public void AddDevTeamAgents_RegistersAllRoleAgents()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Models:Kimi:BaseUrl"] = "https://kimi.example/v1",
            ["Models:Kimi:ApiKey"] = "test-key",
            ["Models:Kimi:Model"] = "kimi-k2.5",
            ["Models:Qwen:BaseUrl"] = "https://qwen.example/v1/{0}/{1}",
            ["Models:Qwen:ApiKey"] = "test-key",
            ["Models:Qwen:Model"] = "qwen3.5-397b-a17b",
            ["Models:Glm:BaseUrl"] = "https://glm.example/v1",
            ["Models:Glm:ApiKey"] = "test-key",
            ["Models:Glm:Model"] = "glm-4.7",
            ["Models:MiniMax:BaseUrl"] = "https://minimax.example/v1",
            ["Models:MiniMax:ApiKey"] = "test-key",
            ["Models:MiniMax:Model"] = "MiniMax-M2.5",
            ["Models:Reviewer:BaseUrl"] = "https://reviewer.example/v1/{0}/{1}",
            ["Models:Reviewer:ApiKey"] = "test-key",
            ["Models:Reviewer:Model"] = "qwen3.5-397b-a17b"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();

        services.AddDevTeamAgents(configuration);

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<AnalystAgent>());
        Assert.NotNull(provider.GetService<ArchitectAgent>());
        Assert.NotNull(provider.GetService<CoderAgent>());
        Assert.NotNull(provider.GetService<ReviewerAgent>());
        Assert.NotNull(provider.GetService<CtoAgent>());
    }

    [Fact]
    public void AddDevTeamAgents_UsesQwenFallbackWhenReviewerModelIsNotConfigured()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Models:Kimi:BaseUrl"] = "https://kimi.example/v1",
            ["Models:Kimi:ApiKey"] = "test-key",
            ["Models:Kimi:Model"] = "kimi-k2.5",
            ["Models:Qwen:BaseUrl"] = "https://qwen.example/v1/{0}/{1}",
            ["Models:Qwen:ApiKey"] = "test-key",
            ["Models:Qwen:Model"] = "qwen3.5-397b-a17b",
            ["Models:Glm:BaseUrl"] = "https://glm.example/v1",
            ["Models:Glm:ApiKey"] = "test-key",
            ["Models:Glm:Model"] = "glm-4.7",
            ["Models:MiniMax:BaseUrl"] = "https://minimax.example/v1",
            ["Models:MiniMax:ApiKey"] = "test-key",
            ["Models:MiniMax:Model"] = "MiniMax-M2.5"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();

        services.AddDevTeamAgents(configuration);

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<ReviewerAgent>());
    }

    [Fact]
    public void WorkItem_DefaultWorkflowMetadata_IsInitialized()
    {
        var workItem = new WorkItem
        {
            Id = "W1",
            Title = "Implement feature",
            Goal = "Ship the initial feature",
            AssignedRole = AgentRole.Coder
        };

        Assert.Equal(WorkItemStatus.Pending, workItem.Status);
        Assert.Equal(0, workItem.Priority);
        Assert.Empty(workItem.DependsOn);
        Assert.Empty(workItem.FileHints);
        Assert.Empty(workItem.Constraints);
        Assert.Empty(workItem.TestsToRun);
        Assert.Equal(string.Empty, workItem.RollbackStrategy);
        Assert.Empty(workItem.Metadata);
    }

    [Fact]
    public void ProjectMemory_DefaultWorkflowSlots_AreInitialized()
    {
        var memory = new ProjectMemory();

        Assert.Equal(string.Empty, memory.TaskCategory);
        Assert.Equal(string.Empty, memory.RequirementGoal);
        Assert.Equal(string.Empty, memory.RoutingDecision);
        Assert.Equal(string.Empty, memory.InterfaceContracts);
        Assert.Equal(string.Empty, memory.TaskDag);
        Assert.Equal(string.Empty, memory.RollbackStrategy);
        Assert.Equal(string.Empty, memory.FinalDecision);
        Assert.Equal(string.Empty, memory.FinalSummary);
        Assert.Empty(memory.WorkItems);
        Assert.Empty(memory.RepoMap);
        Assert.Empty(memory.PatchHistory);
        Assert.Empty(memory.ReviewFindings);
        Assert.Empty(memory.TestExecutionLog);
        Assert.Empty(memory.CodeChanges);
    }
}
