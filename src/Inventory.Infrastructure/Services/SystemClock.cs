using Inventory.Application.Abstractions;

namespace Inventory.Infrastructure.Services;

public sealed class SystemClock : IClock { public DateTime UtcNow => DateTime.UtcNow; }
