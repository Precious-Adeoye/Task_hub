using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Task_hub.Application.Abstraction;
using Task_hub.Application.Migration;
using TaskHub.Core.Entities;
using TaskHub.Core.Entities.File_storage;
using TaskHub.Core.Enum;

namespace TaskHub.Infrastructure.Storage
{
    public class FileStorage : IStorage
    {
        private readonly string _storagePath;
        private readonly ILogger<FileStorage> _logger;
        private readonly IMigrationService _migrationService;
        private readonly SemaphoreSlim _fileLock = new(1, 1);
        private FileStorageSchema? _cachedSchema;
        private DateTime _lastReadTime = DateTime.MinValue;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(5);

        public FileStorage(IConfiguration configuration, ILogger<FileStorage> logger, IMigrationService migrationService)
        {
            var basePath = configuration.GetValue<string>("FileStorage:Path") ?? "storage";
            _storagePath = Path.Combine(Directory.GetCurrentDirectory(), basePath, "taskhub-data.json");
            _logger = logger;
            _migrationService = migrationService;

            // Ensure directory exists
            var directory = Path.GetDirectoryName(_storagePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Initialize storage if it doesn't exist
            if (!System.IO.File.Exists(_storagePath))
            {
                InitializeStorage();
            }
        }

        private void InitializeStorage()
        {
            var schema = new FileStorageSchema
            {
                SchemaVersion = 2, // Current version
                LastModified = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(schema, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(_storagePath, json);
            _logger.LogInformation("Initialized new file storage at {Path}", _storagePath);
        }

        private async Task<FileStorageSchema> ReadSchemaAsync()
        {
            // Check cache
            if (_cachedSchema != null && DateTime.UtcNow - _lastReadTime < _cacheDuration)
            {
                return _cachedSchema;
            }

            await _fileLock.WaitAsync();
            try
            {
                var json = await System.IO.File.ReadAllTextAsync(_storagePath);
                var schema = JsonSerializer.Deserialize<FileStorageSchema>(json);

                if (schema == null)
                {
                    throw new InvalidOperationException("Failed to deserialize storage file");
                }

                // Run migrations if needed
                schema = await _migrationService.MigrateIfNeededAsync(schema);

                // Update cache
                _cachedSchema = schema;
                _lastReadTime = DateTime.UtcNow;

                return schema;
            }
            finally
            {
                _fileLock.Release();
            }
        }

        private async Task WriteSchemaAsync(FileStorageSchema schema)
        {
            schema.LastModified = DateTime.UtcNow;

            await _fileLock.WaitAsync();
            try
            {
                // Write to temporary file first (atomic write)
                var tempPath = _storagePath + ".tmp";
                var json = JsonSerializer.Serialize(schema, new JsonSerializerOptions { WriteIndented = true });
                await System.IO.File.WriteAllTextAsync(tempPath, json);

                // Rename (atomic operation on most file systems)
                System.IO.File.Delete(_storagePath);
                System.IO.File.Move(tempPath, _storagePath);

                // Update cache
                _cachedSchema = schema;
                _lastReadTime = DateTime.UtcNow;
            }
            finally
            {
                _fileLock.Release();
            }
        }

        // Users
        public async Task<User?> GetUserByIdAsync(Guid id)
        {
            var schema = await ReadSchemaAsync();
            if (schema.Users.TryGetValue(id, out var userData))
            {
                return MapToUser(userData);
            }
            return null;
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            var schema = await ReadSchemaAsync();
            var userData = schema.Users.Values.FirstOrDefault(u => u.Username == username);
            return userData != null ? MapToUser(userData) : null;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            var schema = await ReadSchemaAsync();
            var userData = schema.Users.Values.FirstOrDefault(u => u.Email == email);
            return userData != null ? MapToUser(userData) : null;
        }

        public async Task AddUserAsync(User user)
        {
            var schema = await ReadSchemaAsync();
            schema.Users[user.Id] = MapToUserData(user);
            await WriteSchemaAsync(schema);
        }

        public async Task UpdateUserAsync(User user)
        {
            var schema = await ReadSchemaAsync();
            if (schema.Users.ContainsKey(user.Id))
            {
                schema.Users[user.Id] = MapToUserData(user);
                await WriteSchemaAsync(schema);
            }
        }

        // Organisations
        public async Task<Organisation?> GetOrganisationByIdAsync(Guid id)
        {
            var schema = await ReadSchemaAsync();
            if (schema.Organisations.TryGetValue(id, out var orgData))
            {
                return MapToOrganisation(orgData);
            }
            return null;
        }

        public async Task<IEnumerable<Organisation>> GetUserOrganisationsAsync(Guid userId)
        {
            var schema = await ReadSchemaAsync();
            var orgIds = schema.Memberships.Values
                .Where(m => m.UserId == userId)
                .Select(m => m.OrganisationId)
                .ToHashSet();

            return schema.Organisations.Values
                .Where(o => orgIds.Contains(o.Id))
                .Select(MapToOrganisation);
        }

        public async Task AddOrganisationAsync(Organisation organisation)
        {
            var schema = await ReadSchemaAsync();
            schema.Organisations[organisation.Id] = MapToOrganisationData(organisation);
            await WriteSchemaAsync(schema);
        }

        public async Task UpdateOrganisationAsync(Organisation organisation)
        {
            var schema = await ReadSchemaAsync();
            if (schema.Organisations.ContainsKey(organisation.Id))
            {
                schema.Organisations[organisation.Id] = MapToOrganisationData(organisation);
                await WriteSchemaAsync(schema);
            }
        }

        // Memberships
        public async Task<Membership?> GetMembershipAsync(Guid userId, Guid organisationId)
        {
            var schema = await ReadSchemaAsync();
            var key = $"{userId}:{organisationId}";
            if (schema.Memberships.TryGetValue(key, out var membershipData))
            {
                return MapToMembership(membershipData);
            }
            return null;
        }

        public async Task<IEnumerable<Membership>> GetOrganisationMembershipsAsync(Guid organisationId)
        {
            var schema = await ReadSchemaAsync();
            return schema.Memberships.Values
                .Where(m => m.OrganisationId == organisationId)
                .Select(MapToMembership);
        }

        public async Task AddMembershipAsync(Membership membership)
        {
            var schema = await ReadSchemaAsync();
            var key = $"{membership.UserId}:{membership.OrganisationId}";
            schema.Memberships[key] = MapToMembershipData(membership);
            await WriteSchemaAsync(schema);
        }

        public async Task UpdateMembershipAsync(Membership membership)
        {
            var schema = await ReadSchemaAsync();
            var key = $"{membership.UserId}:{membership.OrganisationId}";
            if (schema.Memberships.ContainsKey(key))
            {
                schema.Memberships[key] = MapToMembershipData(membership);
                await WriteSchemaAsync(schema);
            }
        }

        public async Task RemoveMembershipAsync(Guid userId, Guid organisationId)
        {
            var schema = await ReadSchemaAsync();
            var key = $"{userId}:{organisationId}";
            schema.Memberships.Remove(key);
            await WriteSchemaAsync(schema);
        }

        // Todos
        public async Task<Todo?> GetTodoByIdAsync(Guid id, Guid organisationId)
        {
            var schema = await ReadSchemaAsync();
            if (schema.Todos.TryGetValue(id, out var todoData) && todoData.OrganisationId == organisationId)
            {
                return MapToTodo(todoData);
            }
            return null;
        }

        public async Task<IEnumerable<Todo>> GetTodosAsync(Guid organisationId, TodoFilter? filter = null)
        {
            var schema = await ReadSchemaAsync();
            var query = schema.Todos.Values.Where(t => t.OrganisationId == organisationId);

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

            return query.Select(MapToTodo);
        }

        public async Task AddTodoAsync(Todo todo)
        {
            var schema = await ReadSchemaAsync();
            todo.Version = Guid.NewGuid().ToString();
            schema.Todos[todo.Id] = MapToTodoData(todo);
            await WriteSchemaAsync(schema);
        }

        public async Task UpdateTodoAsync(Todo todo)
        {
            var schema = await ReadSchemaAsync();
            if (schema.Todos.ContainsKey(todo.Id))
            {
                todo.Version = Guid.NewGuid().ToString();
                todo.UpdatedAt = DateTime.UtcNow;
                schema.Todos[todo.Id] = MapToTodoData(todo);
                await WriteSchemaAsync(schema);
            }
        }

        public async Task DeleteTodoAsync(Guid id, Guid organisationId)
        {
            var schema = await ReadSchemaAsync();
            if (schema.Todos.TryGetValue(id, out var todo) && todo.OrganisationId == organisationId)
            {
                schema.Todos.Remove(id);
                await WriteSchemaAsync(schema);
            }
        }

        // Audit Logs
        public async Task AddAuditLogAsync(AuditLog log)
        {
            var schema = await ReadSchemaAsync();
            schema.AuditLogs[log.Id] = MapToAuditLogData(log);
            await WriteSchemaAsync(schema);
        }

        public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(Guid organisationId, DateTime? from = null, DateTime? to = null)
        {
            var schema = await ReadSchemaAsync();
            var query = schema.AuditLogs.Values.Where(l => l.OrganisationId == organisationId);

            if (from.HasValue)
                query = query.Where(l => l.Timestamp >= from.Value);

            if (to.HasValue)
                query = query.Where(l => l.Timestamp <= to.Value);

            return query.OrderByDescending(l => l.Timestamp).Select(MapToAuditLog);
        }

        // Mapping methods
        private User MapToUser(UserData data)
        {
            return new User
            {
                Id = data.Id,
                Username = data.Username,
                Email = data.Email,
                PasswordHash = data.PasswordHash,
                CreatedAt = data.CreatedAt,
                LastLoginAt = data.LastLoginAt,
                FailedLoginAttempts = data.FailedLoginAttempts,
                LockoutEnd = data.LockoutEnd
            };
        }

        private UserData MapToUserData(User user)
        {
            return new UserData
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                PasswordHash = user.PasswordHash,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                FailedLoginAttempts = user.FailedLoginAttempts,
                LockoutEnd = user.LockoutEnd
            };
        }

        private Organisation MapToOrganisation(OrganisationData data)
        {
            return new Organisation
            {
                Id = data.Id,
                Name = data.Name,
                CreatedAt = data.CreatedAt,
                CreatedBy = data.CreatedBy
            };
        }

        private OrganisationData MapToOrganisationData(Organisation org)
        {
            return new OrganisationData
            {
                Id = org.Id,
                Name = org.Name,
                CreatedAt = org.CreatedAt,
                CreatedBy = org.CreatedBy
            };
        }

        private Membership MapToMembership(MembershipData data)
        {
            return new Membership
            {
                Id = data.Id,
                UserId = data.UserId,
                OrganisationId = data.OrganisationId,
                Role = data.Role,
                JoinedAt = data.JoinedAt
            };
        }

        private MembershipData MapToMembershipData(Membership membership)
        {
            return new MembershipData
            {
                Id = membership.Id,
                UserId = membership.UserId,
                OrganisationId = membership.OrganisationId,
                Role = membership.Role,
                JoinedAt = membership.JoinedAt
            };
        }

        private Todo MapToTodo(TodoData data)
        {
            return new Todo
            {
                Id = data.Id,
                OrganisationId = data.OrganisationId,
                CreatedBy = data.CreatedBy,
                Title = data.Title,
                Description = data.Description,
                Status = data.Status,
                Priority = data.Priority,
                Tags = data.Tags,
                DueDate = data.DueDate,
                CreatedAt = data.CreatedAt,
                UpdatedAt = data.UpdatedAt,
                DeletedAt = data.DeletedAt,
                Version = data.Version
            };
        }

        private TodoData MapToTodoData(Todo todo)
        {
            return new TodoData
            {
                Id = todo.Id,
                OrganisationId = todo.OrganisationId,
                CreatedBy = todo.CreatedBy,
                Title = todo.Title,
                Description = todo.Description,
                Status = todo.Status,
                Priority = todo.Priority,
                Tags = todo.Tags,
                DueDate = todo.DueDate,
                CreatedAt = todo.CreatedAt,
                UpdatedAt = todo.UpdatedAt,
                DeletedAt = todo.DeletedAt,
                Version = todo.Version
            };
        }

        private AuditLog MapToAuditLog(AuditLogData data)
        {
            return new AuditLog
            {
                Id = data.Id,
                Timestamp = data.Timestamp,
                ActorUserId = data.ActorUserId,
                OrganisationId = data.OrganisationId,
                ActionType = data.ActionType,
                EntityType = data.EntityType,
                EntityId = data.EntityId,
                Details = data.Details,
                CorrelationId = data.CorrelationId
            };
        }

        private AuditLogData MapToAuditLogData(AuditLog log)
        {
            return new AuditLogData
            {
                Id = log.Id,
                Timestamp = log.Timestamp,
                ActorUserId = log.ActorUserId,
                OrganisationId = log.OrganisationId,
                ActionType = log.ActionType,
                EntityType = log.EntityType,
                EntityId = log.EntityId,
                Details = log.Details,
                CorrelationId = log.CorrelationId
            };
        }
}
}
