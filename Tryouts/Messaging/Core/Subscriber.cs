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

namespace MorganStanley.ComposeUI.Messaging;

/// <summary>
/// Contains utilities for creating <see cref="ISubscriber{T}"/> instances.
/// </summary>
public static class Subscriber
{
    /// <summary>
    /// Creates a subscriber from an <see cref="Action{T}"/>
    /// </summary>
    /// <param name="onNext"></param>
    /// <param name="onError"></param>
    /// <param name="onCompleted"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static ISubscriber<T> Create<T>(Action<T> onNext, Action<Exception>? onError = null, Action? onCompleted = null)
    {
        return new SubscriberImpl<T>(
            onNext: value =>
            {
                onNext(value);

                return default;
            },
            onError: exception =>
            {
                onError?.Invoke(exception);

                return default;
            },
            onCompleted: () =>
            {
                onCompleted?.Invoke();

                return default;
            });
    }

    /// <summary>
    /// Creates a subscriber from the provided delegates.
    /// </summary>
    /// <param name="onNext"></param>
    /// <param name="onError"></param>
    /// <param name="onCompleted"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static ISubscriber<T> Create<T>(Func<T, ValueTask> onNext, Func<Exception, ValueTask>? onError = null, Func<ValueTask>? onCompleted = null)
    {
        return new SubscriberImpl<T>(onNext, onError, onCompleted);
    }

    private sealed class SubscriberImpl<T> : ISubscriber<T>
    {
        private readonly Func<T, ValueTask> _onNext;
        private readonly Func<Exception, ValueTask>? _onError;
        private readonly Func<ValueTask>? _onCompleted;

        public SubscriberImpl(
            Func<T, ValueTask> onNext,
            Func<Exception, ValueTask>? onError = null,
            Func<ValueTask>? onCompleted = null)
        {
            _onNext = onNext;
            _onError = onError;
            _onCompleted = onCompleted;
        }

        public ValueTask OnNextAsync(T value)
        {
            return _onNext(value);
        }

        public ValueTask OnErrorAsync(Exception error)
        {
            return _onError?.Invoke(error) ?? default;
        }

        public ValueTask OnCompletedAsync()
        {
            return _onCompleted?.Invoke() ?? default;
        }
    }
}
