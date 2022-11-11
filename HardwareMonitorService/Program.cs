using HardwareMonitorService;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "HardwareMonitorService";
    })
    .ConfigureServices(services =>
    {
        services.AddHostedService<HardwareService>();
    })
    .Build();

await host.RunAsync();
