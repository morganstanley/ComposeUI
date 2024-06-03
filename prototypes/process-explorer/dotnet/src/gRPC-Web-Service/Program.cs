using gRPC_Web_Service.DependencyInjection;
using gRPC_Web_Service.Server.GrpcServer;
using gRPC_Web_Service.Server.Abstractions;
using gRPC_Web_Service.Server.Infrastructure.Grpc;
using gRPC_Web_Service.Services;
using gRPC_Web_Service.Sever.Abstractions;
using MorganStanley.ComposeUI.ProcessExplorer.Core.DependencyInjection;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Infrastructure;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddCors(o => o.AddPolicy("AllowAll", builder =>
{
    builder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding");
}));
builder.Services.AddProcessExplorerAggregator();
builder.Services.AddProcessMonitorWindows();
builder.Services.AddSubsystemController();
builder.Services.AddSingleton<IUiHandler, GrpcUiHandler>();
builder.Services.Configure<ProcessExplorerServerOptions>(op =>
{
    op.Port = 5060;
    op.MainProcessId = Process.GetCurrentProcess().Id;
    op.EnableProcessExplorer = true;
});
builder.Services.AddSingleton<GrpcListenerService>();
builder.Services.AddSingleton<IHostedService>(provider => provider.GetRequiredService<GrpcListenerService>());
builder.Services.AddSingleton<ProcessExplorerServer>(provider => provider.GetRequiredService<GrpcListenerService>());

var app = builder.Build();
app.UseGrpcWeb();
app.UseCors();
// Configure the HTTP request pipeline.
//app.MapGrpcService<GreeterService>().EnableGrpcWeb().RequireCors("AllowAll");
app.MapGrpcService<ProcessExplorerMessageHandlerService>().EnableGrpcWeb().RequireCors("AllowAll");

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
