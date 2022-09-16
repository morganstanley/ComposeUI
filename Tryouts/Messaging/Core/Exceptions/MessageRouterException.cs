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

using System.Runtime.Serialization;

namespace MorganStanley.ComposeUI.Tryouts.Messaging.Core.Exceptions;

public class MessageRouterException : Exception
{
    public MessageRouterException()
    {
    }

    protected MessageRouterException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public MessageRouterException(string? message) : base(message)
    {
    }

    public MessageRouterException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}