using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
