using System.Collections.Concurrent;
using Task_hub.Application.Abstraction;



namespace TaskHub.Storage.InMemory;

public class InMemoryStorage : IStorage
{
    private readonly ConcurrentDictionary<Guid, User> _users = new();
    private readonly ConcurrentDictionary<Guid, Organisation> _organisations = new();
    private readonly ConcurrentDictionary<string, Membership> _memberships = new(); // Key: $"{userId}:{orgId}"
    private readonly ConcurrentDictionary<Guid, Todo> _todos = new();
    private readonly ConcurrentDictionary<Guid, AuditLog> _auditLogs = new();

    // Users
    public Task<User?> GetUserByIdAsync(Guid id) =>
        Task.FromResult(_users.GetValueOrDefault(id));

    public Task<User?> GetUserByUsernameAsync(string username) =>
        Task.FromResult(_users.Values.FirstOrDefault(u => u.Username == username));

    public Task<User?> GetUserByEmailAsync(string email) =>
        Task.FromResult(_users.Values.FirstOrDefault(u => u.Email == email));

    public Task AddUserAsync(User user)
    {
        _users[user.Id] = user;
        return Task.CompletedTask;
    }

    public Task UpdateUserAsync(User user)
    {
        _users[user.Id] = user;
        return Task.CompletedTask;
    }

    // Organisations
    public Task<Organisation?> GetOrganisationByIdAsync(Guid id) =>
        Task.FromResult(_organisations.GetValueOrDefault(id));

    public Task<IEnumerable<Organisation>> GetUserOrganisationsAsync(Guid userId)
    {
        var orgIds = _memberships.Values
            .Where(m => m.UserId == userId)
            .Select(m => m.OrganisationId)
            .ToHashSet();

        var orgs = _organisations.Values.Where(o => orgIds.Contains(o.Id));
        return Task.FromResult(orgs);
    }

    public Task AddOrganisationAsync(Organisation organisation)
    {
        _organisations[organisation.Id] = organisation;
        return Task.CompletedTask;
    }

    public Task UpdateOrganisationAsync(Organisation organisation)
    {
        _organisations[organisation.Id] = organisation;
        return Task.CompletedTask;
    }

    // Memberships
    public Task<Membership?> GetMembershipAsync(Guid userId, Guid organisationId)
    {
        var key = $"{userId}:{organisationId}";
        return Task.FromResult(_memberships.GetValueOrDefault(key));
    }

    public Task<IEnumerable<Membership>> GetOrganisationMembershipsAsync(Guid organisationId)
    {
        var memberships = _memberships.Values.Where(m => m.OrganisationId == organisationId);
        return Task.FromResult(memberships);
    }

    public Task AddMembershipAsync(Membership membership)
    {
        var key = $"{membership.UserId}:{membership.OrganisationId}";
        _memberships[key] = membership;
        return Task.CompletedTask;
    }

    public Task UpdateMembershipAsync(Membership membership)
    {
        var key = $"{membership.UserId}:{membership.OrganisationId}";
        _memberships[key] = membership;
        return Task.CompletedTask;
    }

    public Task RemoveMembershipAsync(Guid userId, Guid organisationId)
    {
        var key = $"{userId}:{organisationId}";
        _memberships.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    // Todos
    public Task<Todo?> GetTodoByIdAsync(Guid id, Guid organisationId)
    {
        var todo = _todos.GetValueOrDefault(id);
        if (todo?.OrganisationId != organisationId)
            return Task.FromResult<Todo?>(null);

        return Task.FromResult(todo);
    }

    public Task<IEnumerable<Todo>> GetTodosAsync(Guid organisationId, TodoFilter? filter = null)
    {
        var query = _todos.Values.Where(t => t.OrganisationId == organisationId);

        if (filter != null)
        {
            if (!filter.IncludeDeleted)
                query = query.Where(t => t.DeletedAt == null);

            if (filter.Status.HasValue)
                query = query.Where(t => t.Status == filter.Status.Value);

            if (filter.Overdue.HasValue && filter.Overdue.Value)
                query = query.Where(t => t.DueDate < DateTime.UtcNow && t.Status != TodoStatus.Done);

            if (!string.IsNullOrWhiteSpace(filter.Tag))
                query = query.Where(t => t.Tags.Contains(filter.Tag));

            // Sorting
            query = filter.SortBy.ToLower() switch
            {
                "createdat" => filter.SortDescending
                    ? query.OrderByDescending(t => t.CreatedAt)
                    : query.OrderBy(t => t.CreatedAt),
                "duedate" => filter.SortDescending
                    ? query.OrderByDescending(t => t.DueDate)
                    : query.OrderBy(t => t.DueDate),
                "priority" => filter.SortDescending
                    ? query.OrderByDescending(t => t.Priority)
                    : query.OrderBy(t => t.Priority),
                _ => filter.SortDescending
                    ? query.OrderByDescending(t => t.CreatedAt)
                    : query.OrderBy(t => t.CreatedAt)
            };

            // Pagination
            query = query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize);
        }

        return Task.FromResult(query);
    }

    public Task AddTodoAsync(Todo todo)
    {
        todo.Version = Guid.NewGuid().ToString(); // New version
        _todos[todo.Id] = todo;
        return Task.CompletedTask;
    }

    public Task UpdateTodoAsync(Todo todo)
    {
        todo.Version = Guid.NewGuid().ToString(); // Update version
        todo.UpdatedAt = DateTime.UtcNow;
        _todos[todo.Id] = todo;
        return Task.CompletedTask;
    }

    public Task DeleteTodoAsync(Guid id, Guid organisationId)
    {
        _todos.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    // Audit Logs
    public Task AddAuditLogAsync(AuditLog log)
    {
        _auditLogs[log.Id] = log;
        return Task.CompletedTask;
    }

    public Task<IEnumerable<AuditLog>> GetAuditLogsAsync(Guid organisationId, DateTime? from = null, DateTime? to = null)
    {
        var query = _auditLogs.Values.Where(l => l.OrganisationId == organisationId);

        if (from.HasValue)
            query = query.Where(l => l.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(l => l.Timestamp <= to.Value);

        return Task.FromResult(query.OrderByDescending(l => l.Timestamp));
    }
}