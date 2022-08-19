<!-- Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. -->

ThemeChangerPrototype
=====================

This is a prototype showing how to assemble 3 simple WPF applications within Avalonia shell. The WPF applications communicate with
each other vie a GRPC server running within Avalonia shell.
The GRPC server is loaded dynamically as a plugin and the main application
does not know its exact type. The server implements ICommunicationService defined within Tryouts.Abstractions used by the shell to
control the server's lifetime. 

SubscriptionClientImpl is also loaded dynamically as a plugin to communicate with the server. It implements ISubscriptionClient interface
defined within Tryouts.Abstractsions:
public interface ISubscriptionClient
{
    Task Connect(string host, int port);

    Task Publish<TMessage>(Topic topic, TMessage msg)
            where TMessage : IMessage;

    IAsyncEnumerable<TMessage> Subscribe<TMessage>(Topic topic)
        where TMessage : IMessage, new();
}

There is another singileton plugin that's being loaded - ThemingServices. It implements interface IThemingService. It uses the
ISubscriptioClient to send ThemeMessage for updating the theme within all the applicaitions that load it as a plugin.

Important Note: The main application should not depend on the plugins (which are loaded dynamically) so you have to build the plugins
separately from the main applicaiton by right clicking on Plugins folder within Solution Explorer and choosing Rebuild option. 