using Shell.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ShellTests
{
    public class ReadResourceTests
    {
        [Fact]
        public void ResourceNotAvailable()
        {
            var resource = ResourceReader.ReadResource("NotAvailableResource");

            Assert.Null(resource);
        }
    }
}
