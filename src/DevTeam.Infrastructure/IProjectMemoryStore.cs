using DevTeam.Core;

namespace DevTeam.Infrastructure;

public interface IProjectMemoryStore
{
    Task<ProjectMemory> GetAsync(string projectId, CancellationToken cancellationToken = default);
    Task SaveAsync(string projectId, ProjectMemory memory, CancellationToken cancellationToken = default);
}
