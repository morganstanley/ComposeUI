/*
* Morgan Stanley makes this available to you under the Apache License,
* Version 2.0 (the "License"). You may obtain a copy of the License at
*
*      http://www.apache.org/licenses/LICENSE-2.0.
*
* See the NOTICE file distributed with this work for additional information
* regarding copyright ownership. Unless required by applicable law or agreed
* to in writing, software distributed under the License is distributed on an
* "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
* or implied. See the License for the specific language governing permissions
* and limitations under the License.
*/

using MorganStanley.ComposeUI.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MorganStanley.ComposeUI.Host.Mock;

internal class CommunicationModuleMock : ICommunicationModule
{
    List<Action<string>>? _subscriptions;

    public ICommunicationClient GetClient()
    {
        return new CommunicationClientMock(this);
    }

    public Task Initialize(ICommunicationClient? ignore)
    {
        _subscriptions = new List<Action<string>>();
        return Task.CompletedTask;
    }

    public Task Teardown()
    {
        _subscriptions = null;
        return Task.CompletedTask;
    }

    internal Task Broadcast(string message)
    {
        if (_subscriptions == null)
        {
            throw new InvalidOperationException("Not initialized or already tore down");
        }

        // Fire and forget broadcast the message
        foreach (var action in _subscriptions)
        {
            Task.Run(() => action(message));
        }

        // Send back a completed task that the comms module successfully received and processed the message.
        // No guarantees on the receivers though.
        return Task.CompletedTask;
    }

    internal Task SubscribeAction(Action<string> client)
    {
        if (_subscriptions == null)
        {
            throw new InvalidOperationException("Not initialized or already tore down");
        }
        _subscriptions.Add(client);
        return Task.CompletedTask;
    }
}
