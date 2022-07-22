using NP.Utilities;
using NP.Utilities.Attributes;
using NP.Utilities.PluginUtils;
using System.Xml.Serialization;
using TestServiceInterfaces;

namespace SecurityTestViewModelPlugin;

[Implements(typeof(IPlugin), partKey: nameof(SecurityTestViewModel), isSingleton: true)]
public class SecurityTestViewModel : VMBase, IPlugin
{
    [XmlAttribute]
    public string? Symbol { get; set; }

    [XmlAttribute]
    public string? Description { get; set; }

    [XmlAttribute]
    public decimal Ask { get; set; }

    [XmlAttribute]
    public decimal Bid { get; set; }

    public override string ToString()
    {
        return $"StockViewModel: Symbol={Symbol}, Ask={Ask}";
    }
}