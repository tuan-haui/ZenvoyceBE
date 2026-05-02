using Zenvoyce.Application.Features.System.DTOs;

namespace Zenvoyce.Application.Abstractions;

public interface IApplicationInitializationService
{
    Task<InitializeSystemResponseDto> TryInitializeAsync(CancellationToken cancellationToken);
}
