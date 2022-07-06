namespace ComposeUI.Messaging.Prototypes;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSingleton<MessageRouterServer>();

        var app = builder.Build();

        app.UseWebSockets();

        app.Use(
            async (context, next) =>
            {
                if (context.Request.Path == "/ws")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        var messageRouter = context.RequestServices.GetRequiredService<MessageRouterServer>();
                        await messageRouter.HandleWebSocketRequest(webSocket, CancellationToken.None);
                    }
                    else
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    }
                }
                else
                {
                    await next(context);
                }
            });

        app.Run();
    }
}