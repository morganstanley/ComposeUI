using MorganStanley.ComposeUI.ProcessExplorer.GrpcWebServer.DependencyInjection;
using MorganStanley.ComposeUI.ProcessExplorer.GrpcWebServer.Server.Abstractions;
using MorganStanley.ComposeUI.ProcessExplorer.GrpcWebServer.Server.Infrastructure.Grpc;

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddProcessExplorerWindowsServerWithGrpc(pe => pe.UseGrpc());

var app = builder.Build();
app.UseGrpcWeb();
app.UseCors();
// Configure the HTTP request pipeline.
//app.MapGrpcService<GreeterService>().EnableGrpcWeb().RequireCors("AllowAll");
app.MapGrpcService<ProcessExplorerMessageHandlerService>().EnableGrpcWeb().RequireCors("AllowAll");

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
