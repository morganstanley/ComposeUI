using System.Collections.Generic;

namespace DockManagerCore
{
    public interface IPaneContainer
    {
        ContentPane ActivePane { get; } 
        void AddPane(ContentPane pane_); 
        IList<ContentPane> GetChildrenPanes(bool leafOnly_=false);
        IList<PaneContainer> GetChildrenContainers();
    }
 
     
 

}
