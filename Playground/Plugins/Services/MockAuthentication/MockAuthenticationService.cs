/// ********************************************************************************************************
///
/// Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License").
/// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0.
/// See the NOTICE file distributed with this work for additional information regarding copyright ownership.
/// Unless required by applicable law or agreed to in writing, software distributed under the License
/// is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
/// See the License for the specific language governing permissions and limitations under the License.
/// 
/// ********************************************************************************************************

using NP.Utilities;
using NP.Utilities.Attributes;
using NP.Utilities.BasicServices;

namespace MockAuthentication;

[Implements(typeof(IAuthenticationService), IsSingleton = true)]
public class MockAuthenticationService : VMBase, IAuthenticationService
{
    [Part]
    private ILog? Log { get; set; }

    private string? _currentUserName;
    public string? CurrentUserName
    {
        get => _currentUserName;
        private set
        {
            if (_currentUserName == value)
                return;

            _currentUserName = value;

            OnPropertyChanged(nameof(CurrentUserName));
            OnPropertyChanged(nameof(IsAuthenticated));
        }
    }

    // Is authenticated is true if and only if the CurrentUserName is not zero
    public bool IsAuthenticated => CurrentUserName != null;

    public bool Authenticate(string userName, string password)
    {
        if (IsAuthenticated)
        {
            throw new Exception("Already Authenticated");
        }

        CurrentUserName =
                (userName == "nick" && password == "1234") ? userName : null;

        if (IsAuthenticated)
        {
            Log?.Log
            (
                LogKind.Info,
                nameof(IAuthenticationService),
                $"Authenticated user '{userName}'");
        }

        return IsAuthenticated;
    }

    public void Logout()
    {
        if (!IsAuthenticated)
        {
            throw new Exception("Already logged out");
        }

        CurrentUserName = null;
    }
}

