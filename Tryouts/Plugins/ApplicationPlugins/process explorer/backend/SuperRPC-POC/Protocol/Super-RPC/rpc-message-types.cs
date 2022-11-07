using System;
using System.Collections.Generic;

using ObjectDescriptors = System.Collections.Generic.Dictionary<string, Super.RPC.ObjectDescriptorWithProps>;
using FunctionDescriptors = System.Collections.Generic.Dictionary<string, Super.RPC.FunctionDescriptor>;
using ClassDescriptors = System.Collections.Generic.Dictionary<string, Super.RPC.ClassDescriptor>;

namespace Super.RPC;

public abstract record RPC_Message
{
    public string rpc_marker = "srpc";
    public string? action;

    public static readonly Dictionary<string, Type> MessageTypesByAction = new Dictionary<string, Type>
    {
        ["get_descriptors"] = typeof(RPC_GetDescriptorsMessage),
        ["descriptors"] = typeof(RPC_DescriptorsResultMessage),
        ["prop_get"] = typeof(RPC_PropGetMessage),
        ["prop_set"] = typeof(RPC_PropSetMessage),
        ["ctor_call"] = typeof(RPC_CtorCallMessage),
        ["fn_call"] = typeof(RPC_FnCallMessage),
        ["method_call"] = typeof(RPC_RpcCallMessage),
        ["obj_died"] = typeof(RPC_ObjectDiedMessage),
        ["fn_reply"] = typeof(RPC_FnResultMessageBase),
    };
}

public record RPC_GetDescriptorsMessage : RPC_Message
{
    public RPC_GetDescriptorsMessage() { action = "get_descriptors"; }
}

public record RPC_DescriptorsResultMessage : RPC_Message
{
    public RPC_DescriptorsResultMessage() { action = "descriptors"; }
    public ObjectDescriptors? objects;
    public FunctionDescriptors? functions;
    public ClassDescriptors? classes;
}

public record RPC_FnCallMessageBase : RPC_Message
{
    public string objId;
    public object?[] args;
}

public record RPC_AnyCallTypeFnCallMessage : RPC_FnCallMessageBase
{
    public FunctionReturnBehavior callType;
    public string? callId;
    // this should be in *some* of the derived records
    public string? prop;
}


public record RPC_FnCallMessage : RPC_AnyCallTypeFnCallMessage
{
    public RPC_FnCallMessage() { action = "fn_call"; }
}
public record RPC_CtorCallMessage : RPC_AnyCallTypeFnCallMessage
{
    public RPC_CtorCallMessage() { action = "ctor_call"; }
}
public record RPC_PropGetMessage : RPC_AnyCallTypeFnCallMessage
{
    public RPC_PropGetMessage() { action = "prop_get"; }
}
public record RPC_PropSetMessage : RPC_AnyCallTypeFnCallMessage
{
    public RPC_PropSetMessage() { action = "prop_set"; }
}
public record RPC_RpcCallMessage : RPC_AnyCallTypeFnCallMessage
{
    public RPC_RpcCallMessage() { action = "method_call"; }
}

public record RPC_FnResultMessageBase : RPC_Message
{
    public RPC_FnResultMessageBase() { action = "fn_reply"; }
    public bool success;
    public object? result;
    public string? callId;
    public FunctionReturnBehavior callType;
}
public record RPC_SyncFnResultMessage : RPC_FnResultMessageBase
{
    public RPC_SyncFnResultMessage() { callType = FunctionReturnBehavior.Sync; }
}
public record RPC_AsyncFnResultMessage : RPC_FnResultMessageBase
{
    public RPC_AsyncFnResultMessage() { callType = FunctionReturnBehavior.Async; }
}

public record RPC_ObjectDiedMessage : RPC_Message
{
    public RPC_ObjectDiedMessage() { action = "obj_died"; }
    public string objId;
}
public record RPC_AsyncCallbackCallMessage : RPC_FnCallMessageBase
{
    public RPC_AsyncCallbackCallMessage() { action = "async_fn"; }
}

public record RPC_BaseObj(string objId, string _rpc_type);
public record RPC_Object(string objId, string _rpc_type, Dictionary<string, object?>? props, string? classId = null) : RPC_BaseObj(objId, _rpc_type);
