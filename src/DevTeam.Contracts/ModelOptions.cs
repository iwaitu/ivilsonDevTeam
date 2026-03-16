namespace DevTeam.Contracts;

public sealed class ModelEndpointOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
}

public sealed class DevTeamModelsOptions
{
    public const string SectionName = "Models";

    public ModelEndpointOptions Kimi { get; set; } = new();
    public ModelEndpointOptions Qwen { get; set; } = new();
    public ModelEndpointOptions Glm { get; set; } = new();
    public ModelEndpointOptions MiniMax { get; set; } = new();
    public ModelEndpointOptions Reviewer { get; set; } = new();
}
