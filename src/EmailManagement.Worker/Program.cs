using EmailManagement.Infrastructure;
using EmailManagement.Infrastructure.Configuration;
using EmailManagement.Worker;

var builder = Host.CreateApplicationBuilder(args);

EnvFileLoader.LoadIfPresent();

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddEmailManagementInfrastructure(builder.Configuration);
builder.Services.AddHostedService<EmailProcessingWorker>();

var host = builder.Build();
await host.RunAsync();