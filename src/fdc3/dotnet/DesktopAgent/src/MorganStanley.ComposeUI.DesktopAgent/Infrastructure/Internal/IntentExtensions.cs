using System.Runtime.CompilerServices;
using Finos.Fdc3.AppDirectory;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal
{
    internal static class IntentExtensions
    {
        public static IEnumerable<FlatAppIntent> AsFlatAppIntents(this Fdc3App app, Guid? instanceId = null)
        {
            foreach (var intent in (IEnumerable<Finos.Fdc3.AppDirectory.IntentMetadata>?) app.Interop?.Intents?.ListensFor?.Values ?? [])
            {
                yield return new FlatAppIntent()
                {
                    InstanceId = instanceId,
                    Intent = intent,
                    App = app
                };
            }
        }

        public static IEnumerable<FlatAppIntent> AsFlatAppIntents(this IEnumerable<Fdc3App> apps)
        {
            foreach (var app in apps)
            {
                foreach (var intent in (IEnumerable<Finos.Fdc3.AppDirectory.IntentMetadata>?) app.Interop?.Intents?.ListensFor?.Values ?? [])
                {
                    yield return new FlatAppIntent()
                    {
                        Intent = intent,
                        App = app
                    };
                }
            }
        }
    }
}
