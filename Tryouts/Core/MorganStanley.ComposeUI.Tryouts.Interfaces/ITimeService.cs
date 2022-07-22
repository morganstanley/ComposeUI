namespace MorganStanley.ComposeUI.Playground.Interfaces
{
    public interface ITimeService
    {
        IObservable<DateTime> TimeObservable { get; }
    }
}
