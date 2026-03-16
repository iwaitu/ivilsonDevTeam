using DevTeam.App;
using DevTeam.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IProjectMemoryStore, InMemoryProjectMemoryStore>();
builder.Services.AddDevTeamAgents(builder.Configuration);
builder.Services.AddSingleton<DevTeamOrchestrator>();

using var host = builder.Build();

var orchestrator = host.Services.GetRequiredService<DevTeamOrchestrator>();

const string request = """
请为一个 .NET 10 Web API 设计“项目归档系统”：
- 支持项目创建、查询、归档
- 使用 PostgreSQL
- 保留现有 JWT 鉴权
- 要求单元测试可运行
""";

var result = await orchestrator.RunAsync("demo-project", request);

Console.WriteLine("=== Task Category ===");
Console.WriteLine(result.TaskCategory);
Console.WriteLine();

Console.WriteLine("=== Requirement Summary ===");
Console.WriteLine(result.RequirementSummary);
Console.WriteLine();

Console.WriteLine("=== Architecture Plan ===");
Console.WriteLine(result.ArchitecturePlan);
Console.WriteLine();

Console.WriteLine("=== Routing Decision ===");
Console.WriteLine(result.RoutingDecision);
Console.WriteLine();

Console.WriteLine("=== Work Items ===");
foreach (var item in result.WorkItems)
{
    Console.WriteLine($"- [{item.AssignedRole}] {item.Title} ({item.Status})");
}

Console.WriteLine();
Console.WriteLine("=== Code Changes ===");
foreach (var change in result.CodeChanges)
{
    Console.WriteLine(change);
    Console.WriteLine("------------------------------------------------");
}

Console.WriteLine();
Console.WriteLine("=== Review Findings ===");
foreach (var finding in result.ReviewFindings)
{
    Console.WriteLine(finding);
    Console.WriteLine("------------------------------------------------");
}

Console.WriteLine();
Console.WriteLine("=== Final Decision ===");
Console.WriteLine(result.FinalDecision);
Console.WriteLine();

Console.WriteLine("=== Final Summary ===");
Console.WriteLine(result.FinalSummary);
