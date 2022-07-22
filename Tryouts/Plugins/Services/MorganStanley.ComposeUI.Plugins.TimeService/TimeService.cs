using MorganStanley.ComposeUI.Playground.Interfaces;
using NP.Utilities.Attributes;
using System.Reactive.Linq;

namespace MorganStanley.ComposeUI.Plugins.TimeService
{
    [Implements(typeof(ITimeService), IsSingleton = true)]
    public class TimeService : ITimeService
    {
        public IObservable<DateTime> TimeObservable
        {
            get
            {
                int delaySeconds = 5 - DateTime.Now.Second % 5;

                return 
                    Observable
                        .Interval(TimeSpan.FromSeconds(5))
                        .Delay(TimeSpan.FromSeconds(delaySeconds))
                        .Select(l => DateTime.Now);
            }
        }
    }
}
