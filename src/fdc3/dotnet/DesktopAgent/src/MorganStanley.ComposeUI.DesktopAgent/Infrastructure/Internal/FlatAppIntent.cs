using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Finos.Fdc3.AppDirectory;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal
{
    internal class FlatAppIntent
    {
        public Guid? InstanceId { get; init; }
        public IntentMetadata Intent { get; init; }
        public Fdc3App App { get; init; }
    }
}
