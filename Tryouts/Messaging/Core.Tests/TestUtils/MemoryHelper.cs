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

using System.Buffers;

namespace MorganStanley.ComposeUI.Messaging.TestUtils;

internal static class MemoryHelper
{
    public static ReadOnlySequence<T> CreateMultipartSequence<T>(T[][] arrays)
    {
        if (arrays.Length == 1)
            return new ReadOnlySequence<T>(arrays[0]);

        var startSegment = new MultipartSequenceSegment<T>(arrays[0]);
        var endSegment = startSegment;
        
        for (var i = 1; i < arrays.Length; i++)
        {
            endSegment = endSegment.Append(arrays[i]);
        }

        return new ReadOnlySequence<T>(startSegment, 0, endSegment, endSegment.Memory.Length);
    }
}
