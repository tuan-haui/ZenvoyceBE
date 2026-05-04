using Zenvoyce.Domain.Entities;

namespace Zenvoyce.Application.Abstractions.Persistence;

public interface ITemplateRepository
{
    Task<bool> BaseTemplateCodeExistsAsync(string kyhieu, Guid? excludingId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Mauhoadongoc>> GetBaseTemplatesAsync(CancellationToken cancellationToken);
    Task<Mauhoadongoc?> GetBaseTemplateByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> IsBaseTemplateInUseAsync(Guid baseTemplateId, CancellationToken cancellationToken);
    Task AddBaseTemplateAsync(Mauhoadongoc template, CancellationToken cancellationToken);
    Task UpdateBaseTemplateAsync(Mauhoadongoc template, CancellationToken cancellationToken);

    Task<bool> BaseTemplateExistsAsync(Guid baseTemplateId, CancellationToken cancellationToken);
    Task<bool> CompanyExistsAsync(Guid companyId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<MauchoctyListItem>> GetCompanyTemplatesAsync(
        Guid donviId,
        string? kyhieuMau,
        string? loaiHoadon,
        short? trangthaiPhatHanh,
        CancellationToken cancellationToken);
    Task<Mauchocty?> GetCompanyTemplateByIdAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateCompanyTemplateStatusAsync(
        Guid id,
        short trangthaiPhatHanh,
        DateTime updatedAt,
        Guid? updatedBy,
        CancellationToken cancellationToken);
    Task ApplyTemplateAsync(
        Mauchocty companyTemplate,
        IReadOnlyCollection<Thongtinhdmau> metadata,
        bool setDefaultTemplate,
        CancellationToken cancellationToken);
}
