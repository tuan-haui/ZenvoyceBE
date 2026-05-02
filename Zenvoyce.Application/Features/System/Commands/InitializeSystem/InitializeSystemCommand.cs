using MediatR;
using Zenvoyce.Application.Abstractions;
using Zenvoyce.Application.Features.System.DTOs;

namespace Zenvoyce.Application.Features.System.Commands.InitializeSystem;

public record InitializeSystemCommand : IRequest<InitializeSystemResponseDto>;

public class InitializeSystemCommandHandler(IApplicationInitializationService initializationService)
    : IRequestHandler<InitializeSystemCommand, InitializeSystemResponseDto>
{
    public Task<InitializeSystemResponseDto> Handle(InitializeSystemCommand request, CancellationToken cancellationToken)
        => initializationService.TryInitializeAsync(cancellationToken);
}
