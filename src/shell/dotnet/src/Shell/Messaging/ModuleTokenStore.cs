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

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace MorganStanley.ComposeUI.Shell.Messaging;

internal sealed class ModuleTokenStore : IModuleTokenStore
{
    private const int TokenSizeInBytes = 32;
    private const int TokenLength = TokenSizeInBytes * 2; // hex encoded

    private readonly ConcurrentDictionary<Guid, Entry> _entries = new();
    private readonly byte[] _hostTokenBytes;

    public ModuleTokenStore()
    {
        HostToken = GenerateToken();
        _hostTokenBytes = Encoding.UTF8.GetBytes(HostToken);
    }

    public string HostToken { get; }

    public bool IsHostToken(string? token)
    {
        if (token == null || token.Length != TokenLength)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(token), _hostTokenBytes);
    }

    public string Issue(Guid moduleInstanceId, string moduleId)
    {
        ArgumentNullException.ThrowIfNull(moduleId);

        return _entries.GetOrAdd(
                moduleInstanceId,
                static (instanceId, id) => new Entry(GenerateToken(), new ModuleIdentity(instanceId, id)),
                moduleId)
            .Token;
    }

    public bool TryValidate(string? token, [NotNullWhen(true)] out ModuleIdentity? identity)
    {
        identity = null;

        // The length of the token is not a secret, so it is fine to bail out early.
        if (token == null || token.Length != TokenLength)
        {
            return false;
        }

        var tokenBytes = Encoding.UTF8.GetBytes(token);

        // Every entry is compared (no early exit on a match) so the time taken does not reveal which token matched.
        foreach (var entry in _entries.Values)
        {
            if (CryptographicOperations.FixedTimeEquals(tokenBytes, entry.TokenBytes))
            {
                identity ??= entry.Identity;
            }
        }

        return identity != null;
    }

    public bool Revoke(Guid moduleInstanceId)
    {
        return _entries.TryRemove(moduleInstanceId, out _);
    }

    private static string GenerateToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(TokenSizeInBytes));

    private sealed class Entry
    {
        public Entry(string token, ModuleIdentity identity)
        {
            Token = token;
            TokenBytes = Encoding.UTF8.GetBytes(token);
            Identity = identity;
        }

        public string Token { get; }
        public byte[] TokenBytes { get; }
        public ModuleIdentity Identity { get; }
    }
}
