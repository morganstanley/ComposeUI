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
using NP.Utilities.PluginUtils;

namespace AuthenticationViewModelPlugin;

[Implements(typeof(IPlugin), partKey: "AuthenticationVM", isSingleton: true)]
public class AuthenticationViewModel : VMBase, IPlugin
{
    [Part(typeof(IAuthenticationService))]
    // Authentication service that comes from the container
    public IAuthenticationService? TheAuthenticationService
    {
        get;
        private set;
    }

    #region UserName Property
    private string? _userName;

    // notifiable property
    public string? UserName
    {
        get
        {
            return this._userName;
        }
        set
        {
            if (this._userName == value)
            {
                return;
            }

            this._userName = value;
            this.OnPropertyChanged(nameof(UserName));
            this.OnPropertyChanged(nameof(CanAuthenticate));
        }
    }
    #endregion UserName Property


    #region Password Property
    private string? _password;

    // notifiable property
    public string? Password
    {
        get
        {
            return this._password;
        }
        set
        {
            if (this._password == value)
            {
                return;
            }

            this._password = value;
            this.OnPropertyChanged(nameof(Password));
            this.OnPropertyChanged(nameof(CanAuthenticate));
        }
    }
    #endregion Password Property

    // change notification fires when either UserName or Password change
    public bool CanAuthenticate =>
        (!string.IsNullOrEmpty(UserName)) && (!string.IsNullOrEmpty(Password));

    // method to call in order to try to authenticate a user
    public void Authenticate()
    {
        TheAuthenticationService?.Authenticate(UserName, Password);

        OnPropertyChanged(nameof(IsAuthenticated));
    }

    // method to exit the application
    public void ExitApplication()
    {
        Environment.Exit(0);
    }

    // IsAuthenticated property 
    // whose change notification fires within Authenticate() method
    public bool IsAuthenticated => TheAuthenticationService?.IsAuthenticated ?? false;
}
