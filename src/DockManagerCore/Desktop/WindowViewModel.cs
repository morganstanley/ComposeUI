using System;
using System.Collections.Generic;
using System.Drawing;

namespace DockManagerCore.Desktop
{
    public class WindowViewModel:ViewModelBase
    {
        public WindowViewModel(InitialWindowParameters initialParameters_)
        {
            ID = "view" + Guid.NewGuid().ToString("N");
            HeaderItems = new HeaderItemsCollection();
            InitialParameters = initialParameters_;
        }
        public string ID { get; private set; }

        public object Content { get; set; }

        public Icon Icon { get; set; }

        public string Title { get; set; }

        public IList<object> HeaderItems { get; private set; }

        public InitialWindowParameters InitialParameters { get; private set; }

        public bool IsActive { get; set; }
    }
}
