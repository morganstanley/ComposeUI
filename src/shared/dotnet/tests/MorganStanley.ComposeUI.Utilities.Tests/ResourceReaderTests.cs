﻿/*
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

using System.Reflection;

namespace MorganStanley.ComposeUI.Utilities.Tests;

public class ReadResourceTests
{
    [Fact]
    public void TestFdc3BundleResourceIsFound()
    {
        var resource = ResourceReader.ReadResource(@$"{Assembly.GetExecutingAssembly().ManifestModule.Assembly.GetName()?.Name}.test.js");

        Assert.NotNull(resource);
    }
}
