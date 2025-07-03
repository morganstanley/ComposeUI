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

namespace MorganStanley.ComposeUI.Messaging.Protocol.Json;

internal static class JsonConstants
{
    public const byte Backslash = (byte)'\\';
    public const byte Quote = (byte)'"';
    public const byte Slash = (byte)'/';

    public const byte Tab = (byte)'\t';
    public const byte LineFeed = (byte)'\n';
    public const byte CarriageReturn = (byte)'\r';
    public const byte Backspace = (byte)'\b';
    public const byte FormFeed = (byte)'\f';

    public const int HighSurrogateStart = 0xD800;
    public const int HighSurrogateEnd = 0xDBFF;
    public const int LowSurrogateStart = 0xDC00;
    public const int LowSurrogateEnd = 0xDFFF;

    public static readonly byte[] UnicodeEscape = "\\u"u8.ToArray();

}
