using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskHub.Core.Entities;

namespace Task_hub.Application.Abstraction
{
    public interface IStorage
    {
        // Users
        Task<User?> GetUserByIdAsync(Guid id);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User?> GetUserByEmailAsync(string email);
        Task AddUserAsync(User user);
        Task UpdateUserAsync(User user);


        // Organisations
        Task<Organisation?> GetOrganisationByIdAsync(Guid id);
        Task<IEnumerable<Organisation>> GetUserOrganisationsAsync(Guid userId);
        Task AddOrganisationAsync(Organisation organisation);
        Task UpdateOrganisationAsync(Organisation organisation);


        // Memberships
        Task<Membership?> GetMembershipAsync(Guid userId, Guid organisationId);
        Task<IEnumerable<Membership>> GetOrganisationMembershipsAsync(Guid organisationId);
        Task AddMembershipAsync(Membership membership);
        Task UpdateMembershipAsync(Membership membership);
        Task RemoveMembershipAsync(Guid userId, Guid organisationId);

        // Todos
        Task<Todo?> GetTodoByIdAsync(Guid id, Guid organisationId);
        Task<IEnumerable<Todo>> GetTodosAsync(Guid organisationId, TodoFilter? filter = null);
        Task AddTodoAsync(Todo todo);
        Task UpdateTodoAsync(Todo todo);
        Task DeleteTodoAsync(Guid id, Guid organisationId); // Hard delete

        // Audit Logs
        Task AddAuditLogAsync(AuditLog log);
        Task<IEnumerable<AuditLog>> GetAuditLogsAsync(Guid organisationId, DateTime? from = null, DateTime? to = null);
    }
}
