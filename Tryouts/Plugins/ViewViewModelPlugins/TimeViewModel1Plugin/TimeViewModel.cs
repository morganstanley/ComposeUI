using NP.Utilities;
using NP.Utilities.Attributes;
using NP.Utilities.PluginUtils;
using System;
using System.Reactive.Linq;

namespace TimeViewModel1Plugin
{
    [Implements(typeof(IPlugin), partKey:"MyNewTimeViewModel", isSingleton:true)]
    public class TimeViewModel : VMBase, IPlugin
    {
        IDisposable _subscription;

        public TimeViewModel()
        {
            DateTime now = DateTime.Now;
            int delayInSeconds = 5 - now.Second % 5;

            IObservable<DateTime> observable =
                Observable
                    .Interval(TimeSpan.FromSeconds(5))
                    .Delay(TimeSpan.FromSeconds(delayInSeconds))
                    .Select(l => DateTime.Now);

            _subscription = observable.Subscribe(OnTimeChanged);
        }

        private void OnTimeChanged(DateTime time)
        {
            TheTime = time;
        }

        #region TheTime Property
        private DateTime _time;
        public DateTime TheTime
        {
            get
            {
                return this._time;
            }
            set
            {
                if (this._time == value)
                {
                    return;
                }

                this._time = value;
                this.OnPropertyChanged(nameof(TheTime));
            }
        }
        #endregion TheTime Property

    }
}
