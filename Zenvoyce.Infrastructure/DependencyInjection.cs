using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zenvoyce.Application.Abstractions;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Abstractions.Services;
using Zenvoyce.Domain.Interfaces;
using Zenvoyce.Infrastructure.Entities;
using Zenvoyce.Infrastructure.Persistence.Repositories;
using Zenvoyce.Infrastructure.Options;
using Zenvoyce.Infrastructure.Security;
using Zenvoyce.Infrastructure.Services;
using Zenvoyce.Infrastructure.Services.Ai;

namespace Zenvoyce.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ZenvoyceDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName));
        services.AddOptions<BootstrapOptions>()
            .Bind(configuration.GetSection(BootstrapOptions.SectionName));
        services.AddOptions<DigitalSignatureOptions>()
            .Bind(configuration.GetSection(DigitalSignatureOptions.SectionName))
            .Validate(x => !string.IsNullOrWhiteSpace(x.PfxPath), "DigitalSignature:PfxPath is required.")
            .ValidateOnStart();
        services.AddOptions<SmtpSettings>()
            .Bind(configuration.GetSection(SmtpSettings.SectionName))
            .Validate(x => !string.IsNullOrWhiteSpace(x.Host), "SmtpSettings:Host is required.")
            .Validate(x => x.Port > 0 && x.Port <= 65535, "SmtpSettings:Port must be in range 1-65535.")
            .Validate(x => !string.IsNullOrWhiteSpace(x.From), "SmtpSettings:From is required.")
            .ValidateOnStart();
        services.AddOptions<VertexAiOptions>()
            .Bind(configuration.GetSection(VertexAiOptions.SectionName))
            .Validate(x => !string.IsNullOrWhiteSpace(x.ProjectId), "VertexAi:ProjectId is required.")
            .Validate(x => !string.IsNullOrWhiteSpace(x.Location), "VertexAi:Location is required.")
            .Validate(x => !string.IsNullOrWhiteSpace(x.Model), "VertexAi:Model is required.")
            .ValidateOnStart();
        services.AddScoped<IApplicationInitializationService, ApplicationInitializationService>();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserPermissionRepository, UserPermissionRepository>();
        services.AddScoped<IPermissionRepository, UserPermissionRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IMenuRepository, MenuRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ITemplateRepository, TemplateRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.AddSingleton<ITemplateRenderer, HandlebarsTemplateRenderer>();
        services.AddSingleton<IInvoicePdfRenderer, PuppeteerPdfRenderer>();
        services.AddScoped<IInvoiceEmailService, SmtpInvoiceEmailService>();
        services.AddHttpClient<IVertexAiService, VertexAiService>();
        services.AddSingleton<IXmlInvoiceSigner, XmlInvoiceSigner>();

        // AI Chat Service — Memory + Function Calling
        services.AddHttpClient<IVertexAiChatService, VertexAiChatService>(client =>
        {
            client.Timeout = TimeSpan.FromMinutes(5); // Timeout dài cho stream + agentic loop
        });
        services.AddScoped<ToolExecutor>();
        services.AddSingleton<ChatSessionStore>();

        return services;
    }
}
