using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace MorganStanley.ComposeUI.Services.CommunicationsServices.GrpcCommunicationsService
{
    public class ObservableWithRefCount<IMessage> : IObservable<IMessage>
    {
        ObservableCollection<IObserver<IMessage>> _observers = 
            new ObservableCollection<IObserver<IMessage>>();

        private IObservable<IMessage> _parentObservable;

        private IDisposable _serverConnection;

        public ObservableWithRefCount(IObservable<IMessage> parentObservable, IDisposable serverConnection)
        {
            _parentObservable = parentObservable;

            _serverConnection = serverConnection;

            _observers.CollectionChanged += _observers_CollectionChanged;
        }

        private void _observers_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_observers.Count == 0)
            {
                _serverConnection?.Dispose();
            }
        }

        public IDisposable Subscribe(IObserver<IMessage> observer)
        {
            _observers.Add(observer);

            IDisposable subscription = _parentObservable.Subscribe(observer);

            SubscriptionDisposable subscriptionDisposable = 
                new SubscriptionDisposable(this, observer, subscription);

            return subscriptionDisposable;
        }

        private class SubscriptionDisposable : IDisposable
        {
            ObservableWithRefCount<IMessage> _observableWithRefCount;
            IObserver<IMessage> _observer;
            IDisposable _subscription;

            public SubscriptionDisposable
            (
                ObservableWithRefCount<IMessage> observableWithRefCount, 
                IObserver<IMessage> observer,
                IDisposable subscription)
            {
                _observableWithRefCount = observableWithRefCount;
                _observer = observer;
                _subscription = subscription;
            }

            public void Dispose()
            {
                _observableWithRefCount._observers.Remove(_observer);

                _subscription?.Dispose();
            }
        }
    }
}
