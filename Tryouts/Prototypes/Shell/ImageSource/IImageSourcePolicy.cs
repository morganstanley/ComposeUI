using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shell.ImageSource
{
    public interface IImageSourcePolicy
    {
        bool IsAllowed(Uri uri, Uri appUri);
    }
}
