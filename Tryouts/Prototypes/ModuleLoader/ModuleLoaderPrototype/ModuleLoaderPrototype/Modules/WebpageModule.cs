using ModuleLoaderPrototype.Interfaces;

namespace ModuleLoaderPrototype.Modules;

internal class WebpageModule : ModuleBase
{
    public WebpageModule(string name, string url) : base(name)
    {
        _url = url;
    }

    private string _url;

    public override Task Initialize()
    {
        return Task.CompletedTask;
    }

    public override Task Launch()
    {
        _lifecycleEvents.OnNext(LifecycleEvent.Started(new ProcessInfo(Name, UIType.Web, _url)));
        return Task.CompletedTask;
    }

    public override Task Teardown()
    {
        _lifecycleEvents.OnNext(LifecycleEvent.Stopped(new ProcessInfo(Name, UIType.Web, _url)));
        return Task.CompletedTask;
    }
}
