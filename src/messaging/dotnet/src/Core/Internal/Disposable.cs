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

namespace MorganStanley.ComposeUI.Messaging.Internal;

internal static class Disposable
{
    public static IDisposable FromAsyncDisposable(IAsyncDisposable asyncDisposable) =>
        new AsyncDisposableWrapper(asyncDisposable);

    public static ValueTask<IDisposable> FromAsyncDisposable(ValueTask<IAsyncDisposable> awaitable)
    {
        return awaitable.IsCompletedSuccessfully 
            ? new ValueTask<IDisposable>(FromAsyncDisposable(awaitable.Result)) 
            : FromAsyncDisposableImpl(awaitable);

        static async ValueTask<IDisposable> FromAsyncDisposableImpl(ValueTask<IAsyncDisposable> awaitable)
        {
            return FromAsyncDisposable(await awaitable);
        }
    }

    internal class AsyncDisposableWrapper : IDisposable
    {
        public AsyncDisposableWrapper(IAsyncDisposable asyncDisposable)
        {
            _asyncDisposable = asyncDisposable;
        }

        public void Dispose()
        {
            var task = _asyncDisposable.DisposeAsync();

            if (task.IsCompletedSuccessfully)
                return;

            task.GetAwaiter().GetResult();
        }

        private readonly IAsyncDisposable _asyncDisposable;
    }
}