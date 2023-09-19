using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MorganStanley.Fdc3.AppDirectory;

namespace MorganStanley.ComposeUI.Shell.ToRemove
{
    internal class AppDirectorySample : IAppDirectory
    {
        private readonly IEnumerable<Fdc3App> _apps = new List<Fdc3App>()
        {
            new Fdc3App("Morgan Stanley", "Morgan Stanley", AppType.Web, new WebAppDetails("http://www.morganstanley.com")),
            new Fdc3App("Microsoft", "Microsoft", AppType.Web, new WebAppDetails("http://www.microsoft.com")),
            new Fdc3App("Google", "Google", AppType.Web, new WebAppDetails("http://www.google.com"))
            {
                Interop = new()
                {
                    Intents = new()
                    {
                        ListensFor = new()
                        {
                            { "ViewGoogle", new IntentMetadata("ViewGoogle", "Google", new[] { "testContext" }) }
                        }
                    }
                }
            },
            new Fdc3App("FINOS FDC3 Workbenchid", "FINOS FDC3 Workbench", AppType.Web, new WebAppDetails("https://fdc3.finos.org/toolbox/fdc3-workbench/")),
        };
        public Task<Fdc3App?> GetApp(string appId)
        {
            return Task.FromResult<Fdc3App?>(_apps.FirstOrDefault(app => app.AppId == appId));
        }

        public Task<IEnumerable<Fdc3App>> GetApps()
        {
            return Task.FromResult(_apps);
        }
    }
}
