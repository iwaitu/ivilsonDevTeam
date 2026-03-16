using DevTeam.Agents;
using DevTeam.Contracts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.VllmChatClient.Glm4;
using Microsoft.Extensions.AI.VllmChatClient.Kimi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevTeam.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDevTeamAgents(this IServiceCollection services, IConfiguration configuration)
    {
        var models = configuration.GetSection(DevTeamModelsOptions.SectionName).Get<DevTeamModelsOptions>()
            ?? new DevTeamModelsOptions();

        services.AddSingleton<AnalystAgent>(_ => new AnalystAgent(CreateKimiClient(models.Kimi)));
        services.AddSingleton<ArchitectAgent>(_ => new ArchitectAgent(CreateQwenClient(models.Qwen)));
        services.AddSingleton<CoderAgent>(_ => new CoderAgent(CreateGlmClient(models.Glm)));
        services.AddSingleton<ReviewerAgent>(_ => new ReviewerAgent(CreateReviewerClient(models)));
        services.AddSingleton<CtoAgent>(_ => new CtoAgent(CreateMiniMaxClient(models.MiniMax)));

        return services;
    }

    private static IChatClient CreateKimiClient(ModelEndpointOptions options)
    {
        Validate(options, nameof(DevTeamModelsOptions.Kimi));
        return new VllmKimiK2ChatClient(options.BaseUrl, options.ApiKey, options.Model);
    }

    private static IChatClient CreateQwenClient(ModelEndpointOptions options)
    {
        Validate(options, nameof(DevTeamModelsOptions.Qwen));
        return new VllmQwen3NextChatClient(options.BaseUrl, options.ApiKey, options.Model);
    }

    private static IChatClient CreateGlmClient(ModelEndpointOptions options)
    {
        Validate(options, nameof(DevTeamModelsOptions.Glm));
        return new VllmGlmChatClient(options.BaseUrl, options.ApiKey, options.Model);
    }

    private static IChatClient CreateMiniMaxClient(ModelEndpointOptions options)
    {
        Validate(options, nameof(DevTeamModelsOptions.MiniMax));
        return new VllmMiniMaxChatClient(options.BaseUrl, options.ApiKey, options.Model);
    }

    private static IChatClient CreateReviewerClient(DevTeamModelsOptions models)
    {
        if (!string.IsNullOrWhiteSpace(models.Reviewer.BaseUrl) && !string.IsNullOrWhiteSpace(models.Reviewer.Model))
        {
            return CreateQwenClient(models.Reviewer);
        }

        return CreateQwenClient(models.Qwen);
    }

    private static void Validate(ModelEndpointOptions options, string name)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            throw new InvalidOperationException($"Models:{name}:BaseUrl is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Model))
        {
            throw new InvalidOperationException($"Models:{name}:Model is required.");
        }
    }
}
