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

using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.Infrastructure.Internal;

public class Fdc3StartupPropertiesValidatorTests
{
    public static TheoryData<string> InvalidValues => new()
    {
        "",
        "app\u0000id",
        "app\nid",
        "app\tid",
        "app\u007fid",
        new string('a', 513)
    };

    [Fact]
    public void Validate_accepts_regular_values()
    {
        var action = () => Fdc3StartupPropertiesValidator.Validate(new Fdc3StartupProperties
        {
            AppId = "my-app",
            InstanceId = "6b0f5d52-6d2c-4a43-8a0e-2a4a2f0d8d11",
            ChannelId = "fdc3.channel.1",
            OpenedAppContextId = "context-1"
        });

        action.Should().NotThrow();
    }

    [Fact]
    public void Validate_accepts_values_that_are_not_set_and_the_longest_allowed_value()
    {
        var action = () => Fdc3StartupPropertiesValidator.Validate(new Fdc3StartupProperties
        {
            AppId = new string('a', 512),
            InstanceId = "instance"
        });

        action.Should().NotThrow();
    }

    [Theory]
    [MemberData(nameof(InvalidValues))]
    public void Validate_rejects_an_invalid_app_id(string value)
    {
        var action = () => Fdc3StartupPropertiesValidator.Validate(
            new Fdc3StartupProperties { AppId = value, InstanceId = "instance" });

        action.Should().Throw<ArgumentException>().WithMessage("*AppId*");
    }

    [Theory]
    [MemberData(nameof(InvalidValues))]
    public void Validate_rejects_an_invalid_channel_id(string value)
    {
        var action = () => Fdc3StartupPropertiesValidator.Validate(
            new Fdc3StartupProperties { AppId = "app", InstanceId = "instance", ChannelId = value });

        action.Should().Throw<ArgumentException>().WithMessage("*ChannelId*");
    }

    [Theory]
    [MemberData(nameof(InvalidValues))]
    public void Validate_rejects_an_invalid_opened_app_context_id(string value)
    {
        var action = () => Fdc3StartupPropertiesValidator.Validate(
            new Fdc3StartupProperties { AppId = "app", InstanceId = "instance", OpenedAppContextId = value });

        action.Should().Throw<ArgumentException>().WithMessage("*OpenedAppContextId*");
    }

    [Fact]
    public void Validate_does_not_put_the_rejected_value_in_the_message()
    {
        var action = () => Fdc3StartupPropertiesValidator.Validate(
            new Fdc3StartupProperties { AppId = "secret\nvalue", InstanceId = "instance" });

        action.Should().Throw<ArgumentException>().Which.Message.Should().NotContain("secret");
    }
}
