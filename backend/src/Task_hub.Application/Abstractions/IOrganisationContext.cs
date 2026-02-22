namespace Task_hub.Application.Abstractions
{
    public interface IOrganisationContext
    {
        Guid? CurrentOrganisationId { get; }
        Task<bool> UserIsInOrganisationAsync(Guid userId, Guid organisationId);
        Task<bool> UserIsOrgAdminAsync(Guid userId, Guid organisationId);
    }
}
