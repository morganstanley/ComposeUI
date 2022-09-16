// Morgan Stanley makes this available to you under the Apache License,
// Version 2.0 (the "License"). You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0.
// 
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership. Unless required by applicable law or agreed
// to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions
// and limitations under the License.

namespace MorganStanley.ComposeUI.Tryouts.Messaging.Server.Transport.WebSocket;

/// <summary>
/// Extension methods for handling WebSocket connections to the Message Router server.
/// </summary>
public static class WebApplicationMessageRouterExtensions
{
    /// <summary>
    /// Maps a path as the WebSocket endpoint for the Message Router server.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    public static WebApplication MapMessageRouterWebSocketEndpoint(this WebApplication app, string path)
    {
        app.Use(
            async (context, next) =>
            {
                if (context.Request.Path == path)
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        await using var handler = ActivatorUtilities.CreateInstance<WebSocketConnection>(context.RequestServices);
                        await handler.HandleWebSocketRequest(webSocket, CancellationToken.None);
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

        return app;
    }
}
