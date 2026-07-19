using Inventory.IisCollector;
using Inventory.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options => options.ServiceName = "PSMCollector IIS Collector");
builder.Services.Configure<IisCollectorOptions>(builder.Configuration.GetSection("Collector"));
builder.Services.AddInventoryInfrastructure(builder.Configuration.GetConnectionString("InventoryDatabase") ?? throw new InvalidOperationException("InventoryDatabase connection string is required."));
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
