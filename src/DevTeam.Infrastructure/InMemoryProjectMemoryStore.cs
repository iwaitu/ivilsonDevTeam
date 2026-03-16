using DevTeam.Core;

namespace DevTeam.Infrastructure;

public sealed class InMemoryProjectMemoryStore : IProjectMemoryStore
{
    private readonly Dictionary<string, ProjectMemory> _store = [];
    private readonly Lock _lock = new();

    public Task<ProjectMemory> GetAsync(string projectId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            if (!_store.TryGetValue(projectId, out var memory))
            {
                memory = new ProjectMemory();
                _store[projectId] = memory;
            }

            return Task.FromResult(memory);
        }
    }

    public Task SaveAsync(string projectId, ProjectMemory memory, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            _store[projectId] = memory;
        }

        return Task.CompletedTask;
    }
}
