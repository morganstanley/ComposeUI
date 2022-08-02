namespace ModuleLoaderPrototype
{
    public struct LifecycleEvent
    {
        public string name;
        public int pid;
        public string eventType;
        public bool expected;

        public static LifecycleEvent Stopped(string name, int pid, bool expected = true) => new LifecycleEvent() { name = name, eventType = LifecycleEventType.Stopped, pid = pid, expected = expected };
        public static LifecycleEvent Started(string name, int pid) => new LifecycleEvent() { name = name, eventType = LifecycleEventType.Started, pid = pid, expected = true };
        public static LifecycleEvent StoppingCanceled(string name, int pid, bool expected) => new LifecycleEvent() { name = name, eventType = LifecycleEventType.StoppingCanceled, pid = pid, expected = expected };
    }

    public class LifecycleEventType
    {
        private LifecycleEventType() { }

        public const string Stopped = "stopped";
        public const string Started = "started";
        public const string StoppingCanceled = "stoppingCanceled";
    }
}