using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace Super.RPC;

[Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
public enum FunctionReturnBehavior
{
    [EnumMember(Value = "void")]
    Void,
    [EnumMember(Value = "sync")]
    Sync,
    [EnumMember(Value = "async")]
    Async
}

public abstract record Descriptor(string type);

public record FunctionDescriptor() : Descriptor("function")
{
    [JsonProperty("name")]
    public string? Name;

    [JsonProperty("arguments", NullValueHandling = NullValueHandling.Ignore)]
    public ArgumentDescriptor[]? Arguments;

    [JsonProperty("returns", NullValueHandling = NullValueHandling.Ignore)]
    public FunctionReturnBehavior Returns = FunctionReturnBehavior.Async;

    public static implicit operator FunctionDescriptor(string name) => new FunctionDescriptor { Name = name };
}

public record PropertyDescriptor() : Descriptor("property")
{
    [JsonProperty("name")]
    public string? Name;

    [JsonProperty("get", NullValueHandling = NullValueHandling.Ignore)]
    public FunctionDescriptor? Get;

    [JsonProperty("set", NullValueHandling = NullValueHandling.Ignore)]
    public FunctionDescriptor? Set;

    [JsonProperty("getOnly", NullValueHandling = NullValueHandling.Ignore)]
    public bool? GetOnly;

    public static implicit operator PropertyDescriptor(string name) => new PropertyDescriptor { Name = name };
}

public record ArgumentDescriptor : FunctionDescriptor   // TODO: Func or Object ? deserialize?
{
    public int? idx;
}

public record ObjectDescriptor() : Descriptor("object")
{
    [JsonProperty("functions", NullValueHandling = NullValueHandling.Ignore)]
    public FunctionDescriptor[]? Functions;

    [JsonProperty("proxiedProperties", NullValueHandling = NullValueHandling.Ignore)]
    public PropertyDescriptor[]? ProxiedProperties;

    [JsonProperty("readonlyProperties", NullValueHandling = NullValueHandling.Ignore)]
    public string[]? ReadonlyProperties;

    [JsonProperty("events", NullValueHandling = NullValueHandling.Ignore)]
    public FunctionDescriptor[]? Events;

}

public record ObjectDescriptorWithProps : ObjectDescriptor
{
    [JsonProperty("props")]
    public Dictionary<string, object?>? Props;

    public static ObjectDescriptorWithProps From(ObjectDescriptor other, Dictionary<string, object?> props)
    {
        return new ObjectDescriptorWithProps
        {
            Functions = other.Functions,
            ReadonlyProperties = other.ReadonlyProperties,
            ProxiedProperties = other.ProxiedProperties,
            Events = other.Events,
            Props = props
        };
    }
}

public record ClassDescriptor() : Descriptor("class")
{
    [JsonProperty("classId")]
    public string? ClassId;

    [JsonProperty("ctor", NullValueHandling = NullValueHandling.Ignore)]
    public FunctionDescriptor? Ctor;

    [JsonProperty("static", NullValueHandling = NullValueHandling.Ignore)]
    public ObjectDescriptor? Static;

    [JsonProperty("instance", NullValueHandling = NullValueHandling.Ignore)]
    public ObjectDescriptor? Instance;
}
