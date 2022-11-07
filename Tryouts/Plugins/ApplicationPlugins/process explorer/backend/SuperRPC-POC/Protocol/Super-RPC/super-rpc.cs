using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ObjectDescriptors = System.Collections.Generic.Dictionary<string, Super.RPC.ObjectDescriptorWithProps>;
using FunctionDescriptors = System.Collections.Generic.Dictionary<string, Super.RPC.FunctionDescriptor>;
using ClassDescriptors = System.Collections.Generic.Dictionary<string, Super.RPC.ClassDescriptor>;
using System.Diagnostics;

namespace Super.RPC;

record AsyncCallbackEntry(Task task, Action<object?> complete, Action<Exception> fail, Type? type);
public record CallContext(IRPCChannel replyChannel, object? data = null)
{
    public TaskCompletionSource? replySent;
}

public class SuperRPC
{
    private AsyncLocal<CallContext?> _currentContextAsyncLocal = new AsyncLocal<CallContext?>();
    public CallContext? CurrentContext
    {
        get => _currentContextAsyncLocal.Value;
        private set => _currentContextAsyncLocal.Value = value;
    }

    protected readonly Func<string> ObjectIDGenerator;
    protected IRPCChannel? Channel;

    private ObjectDescriptors? remoteObjectDescriptors;
    private FunctionDescriptors? remoteFunctionDescriptors;
    private ClassDescriptors? remoteClassDescriptors;
    private TaskCompletionSource<bool>? remoteDescriptorsReceived = null;

    private readonly Dictionary<string, AsyncCallbackEntry> asyncCallbacks = new Dictionary<string, AsyncCallbackEntry>();
    private readonly Dictionary<string, object?> asyncCallbackValues = new Dictionary<string, object?>();
    private volatile int callId = 0;

    private readonly ProxyObjectRegistry proxyObjectRegistry = new ProxyObjectRegistry();
    private readonly ObjectIdDictionary<string, Type, Func<string, object>?> proxyClassRegistry = new ObjectIdDictionary<string, Type, Func<string, object>?>();

    private readonly ObjectIdDictionary<string, object, ObjectDescriptor> hostObjectRegistry = new ObjectIdDictionary<string, object, ObjectDescriptor>();
    private readonly ObjectIdDictionary<string, Delegate, FunctionDescriptor> hostFunctionRegistry = new ObjectIdDictionary<string, Delegate, FunctionDescriptor>();
    private readonly ObjectIdDictionary<string, Type, ClassDescriptor> hostClassRegistry = new ObjectIdDictionary<string, Type, ClassDescriptor>();


    public SuperRPC(Func<string> objectIdGenerator)
    {
        ObjectIDGenerator = objectIdGenerator;
    }

    private string DebugId = "";
    public SuperRPC(Func<string> objectIdGenerator, string debugId) : this(objectIdGenerator)
    {
        DebugId = debugId;
    }

    public void Connect(IRPCChannel channel)
    {
        Channel = channel;
        if (channel is IRPCReceiveChannel receiveChannel)
        {
            receiveChannel.MessageReceived += MessageReceived;
        }
    }

    public void RegisterHostObject(string objId, object target, ObjectDescriptor descriptor)
    {
        hostObjectRegistry.Add(objId, target, descriptor);
    }

    public void RegisterHostFunction(string objId, Delegate target, FunctionDescriptor? descriptor = null)
    {
        hostFunctionRegistry.Add(objId, target, descriptor ?? new FunctionDescriptor());
    }

    public void RegisterHostClass(string classId, Type clazz, ClassDescriptor descriptor)
    {
        descriptor.ClassId = classId;
        if (descriptor.Static is not null)
        {
            RegisterHostObject(classId, clazz, descriptor.Static);
        }

        hostClassRegistry.Add(classId, clazz, descriptor);
    }

    public void RegisterHostClass<TClass>(string classId, ClassDescriptor descriptor)
    {
        RegisterHostClass(classId, typeof(TClass), descriptor);
    }

    public void RegisterProxyClass<TInterface>(string classId)
    {
        RegisterProxyClass(classId, typeof(TInterface));
    }

    public void RegisterProxyClass(string classId, Type ifType)
    {
        proxyClassRegistry.Add(classId, ifType, null);
    }

    protected void MessageReceived(object? sender, MessageReceivedEventArgs eventArgs)
    {
        var message = eventArgs.message;
        var replyChannel = eventArgs.replyChannel ?? Channel;
        CurrentContext = new CallContext(replyChannel!, eventArgs.context);

        if (message.rpc_marker != "srpc") return;   // TODO: throw?

        switch (message)
        {
            case RPC_GetDescriptorsMessage:
                SendRemoteDescriptors(replyChannel);
                break;
            case RPC_DescriptorsResultMessage descriptors:
                SetRemoteDescriptors(descriptors);
                if (remoteDescriptorsReceived is not null)
                {
                    remoteDescriptorsReceived.SetResult(true);
                    remoteDescriptorsReceived = null;
                }
                break;
            case RPC_AnyCallTypeFnCallMessage functionCall:
                CallTargetFunction(functionCall, CurrentContext);
                break;
            case RPC_ObjectDiedMessage objectDied:
                hostObjectRegistry.RemoveById(objectDied.objId);
                hostFunctionRegistry.RemoveById(objectDied.objId);
                break;
            case RPC_FnResultMessageBase fnResult:
                {
                    if (fnResult.callType == FunctionReturnBehavior.Async)
                    {

                        AsyncCallbackEntry? entry = null;
                        lock (asyncCallbacks)
                        {
                            if (!asyncCallbacks.Remove(fnResult.callId!, out entry))
                            {
                                // If the Task is not registered yet, we still store the value (result) that we're going to use to complete it.
                                // In case it's a success=true, we cannot process it yet, because we don't know the exact type (resultType)
                                // but if it's an exception, we store an Exception object, so later we can figure out the success state based on that.
                                asyncCallbackValues.Add(fnResult.callId!, fnResult.success ? fnResult.result : new ArgumentException(fnResult.result?.ToString()));
                            }
                        }

                        if (entry is not null)
                        {
                            if (fnResult.success)
                            {
                                var result = ProcessValueAfterDeserialization(fnResult.result, CurrentContext, entry.type);
                                entry.complete(result);
                            }
                            else
                            {
                                entry.fail(new ArgumentException(fnResult.result?.ToString()));
                            }
                        }
                    }
                    break;
                }
            default:
                throw new ArgumentException("Invalid message received");
        }
    }

    private T GetHostObject<T>(string objId, IDictionary<string, T> registry)
    {
        if (!registry.TryGetValue(objId, out var entry))
        {
            throw new ArgumentException($"No object found with ID '{objId}'.");
        }
        return entry;
    }

    protected void CallTargetFunction(RPC_AnyCallTypeFnCallMessage message, CallContext context)
    {
        object? result = null;
        bool success = true;

        context.replySent = new TaskCompletionSource();

        try
        {
            switch (message.action)
            {
                case "prop_get":
                    {
                        var entry = GetHostObject(message.objId, hostObjectRegistry.ById);
                        result = (entry.obj as Type ?? entry.obj.GetType()).GetProperty(message.prop!)?.GetValue(entry.obj);
                        break;
                    }
                case "prop_set":
                    {
                        var entry = GetHostObject(message.objId, hostObjectRegistry.ById);
                        var propInfo = (entry.obj as Type ?? entry.obj.GetType()).GetProperty(message.prop!);
                        if (propInfo is null)
                        {
                            throw new ArgumentException($"Could not find property '{message.prop}' on object '{message.objId}'.");
                        }
                        var argDescriptors = entry.value.ProxiedProperties?.FirstOrDefault(pd => pd.Name == message.prop)?.Set?.Arguments;
                        var argDescriptor = argDescriptors?.Length > 0 ? argDescriptors[0] : null;
                        var value = ProcessValueAfterDeserialization(message.args[0], context, propInfo.PropertyType, argDescriptor);
                        propInfo.SetValue(entry.obj, value);
                        break;
                    }
                case "method_call":
                    {
                        var entry = GetHostObject(message.objId, hostObjectRegistry.ById);
                        var objType = entry.obj as Type ?? entry.obj.GetType();
                        var method = objType.GetMethod(message.prop!);
                        if (method is null)
                        {
                            throw new ArgumentException($"Method '{message.prop}' not found on object '{message.objId}'.");
                        }
                        var argDescriptors = entry.value.Functions?.FirstOrDefault(fd => fd.Name == message.prop)?.Arguments;
                        var args = ProcessArgumentsAfterDeserialization(message.args, context, method.GetParameters().Select(param => param.ParameterType).ToArray(), argDescriptors);
                        result = method.Invoke(entry.obj, args);
                        break;
                    }
                case "fn_call":
                    {
                        var entry = GetHostObject(message.objId, hostFunctionRegistry.ById);
                        var method = entry.obj.Method;
                        var args = ProcessArgumentsAfterDeserialization(
                            message.args,
                            context,
                            method.GetParameters().Select(param => param.ParameterType).ToArray(),
                            entry.value?.Arguments);
                        result = entry.obj.DynamicInvoke(args);
                        break;
                    }
                case "ctor_call":
                    {
                        var classId = message.objId;
                        if (!hostClassRegistry.ById.TryGetValue(classId, out var entry))
                        {
                            throw new ArgumentException($"No class found with ID '{classId}'.");
                        }
                        var method = entry.obj.GetConstructors()[0];
                        var args = ProcessArgumentsAfterDeserialization(
                            message.args,
                            context,
                            method.GetParameters().Select(param => param.ParameterType).ToArray(),
                            entry.value.Ctor?.Arguments);
                        result = method.Invoke(args);
                        break;
                    }
                default:
                    {
                        throw new ArgumentException($"Invalid message received, action={message.action}");
                    }
            }
        }
        catch (Exception e)
        {
            success = false;
            result = e.ToString();
        }

        if (message.callType == FunctionReturnBehavior.Async)
        {

            void SendAsyncResult(bool success, object? result)
            {
                SendAsyncIfPossible(new RPC_AsyncFnResultMessage
                {
                    success = success,
                    result = result,
                    callId = message.callId
                }, context.replyChannel);
            }

            if (result is Task task)
            {
                SendResultOnTaskCompletion(task, SendAsyncResult, context);
            }
            else
            {
                SendAsyncResult(success, ProcessValueBeforeSerialization(result, context));
            }
            context.replySent.SetResult();
        }
        else if (message.callType == FunctionReturnBehavior.Sync)
        {
            SendSyncIfPossible(new RPC_SyncFnResultMessage
            {
                success = success,
                result = ProcessValueBeforeSerialization(result, context),
            }, context.replyChannel);
            context.replySent.SetResult();
        }
    }

    /**
    * Send a request to get the descriptors for the registered host objects from the other side.
    * Uses synchronous communication if possible and returns `true`/`false` based on if the descriptors were received.
    * If sync is not available, it uses async messaging and returns a Task.
    */
    public ValueTask<bool> RequestRemoteDescriptors()
    {
        if (Channel is IRPCSendSyncChannel syncChannel)
        {
            var response = syncChannel.SendSync(new RPC_GetDescriptorsMessage());
            if (response is RPC_DescriptorsResultMessage descriptors)
            {
                SetRemoteDescriptors(descriptors);
                return new ValueTask<bool>(true);
            }
        }

        if (Channel is IRPCSendAsyncChannel asyncChannel)
        {
            remoteDescriptorsReceived = new TaskCompletionSource<bool>();
            asyncChannel.SendAsync(new RPC_GetDescriptorsMessage());
            return new ValueTask<bool>(remoteDescriptorsReceived.Task);
        }

        return new ValueTask<bool>(false);
    }

    private void SetRemoteDescriptors(RPC_DescriptorsResultMessage response)
    {
        if (response.objects is not null)
        {
            this.remoteObjectDescriptors = response.objects;
        }
        if (response.functions is not null)
        {
            this.remoteFunctionDescriptors = response.functions;
        }
        if (response.classes is not null)
        {
            this.remoteClassDescriptors = response.classes;
        }
    }

    /**
    * Send the descriptors for the registered host objects to the other side.
    * If possible, the message is sent synchronously.
    * This is a "push" style message, for "pull" see [[requestRemoteDescriptors]].
    */
    public void SendRemoteDescriptors(IRPCChannel? replyChannel = null)
    {
        replyChannel ??= Channel;
        SendSyncIfPossible(new RPC_DescriptorsResultMessage
        {
            objects = GetLocalObjectDescriptors(),
            functions = hostFunctionRegistry.ById.ToDictionary(x => x.Key, x => x.Value.value),
            classes = hostClassRegistry.ById.ToDictionary(x => x.Key, x => x.Value.value),
        }, replyChannel);
    }

    private ObjectDescriptors GetLocalObjectDescriptors()
    {
        var descriptors = new ObjectDescriptors();

        foreach (var (key, entry) in hostObjectRegistry.ById)
        {
            if (entry.value is ObjectDescriptor objectDescriptor)
            {
                var props = new Dictionary<string, object?>();
                if (objectDescriptor.ReadonlyProperties is not null)
                {
                    foreach (var prop in objectDescriptor.ReadonlyProperties)
                    {
                        var value = entry.obj.GetType().GetProperty(prop, BindingFlags.Public | BindingFlags.Instance)?.GetValue(entry.obj);
                        if (value is not null) props.Add(prop, value);
                    }
                }
                descriptors.Add(key, ObjectDescriptorWithProps.From(objectDescriptor, props));
            }
        }

        return descriptors;
    }

    private object? SendSyncIfPossible(RPC_Message message, IRPCChannel? channel = null)
    {
        channel ??= Channel;

        if (channel is IRPCSendSyncChannel syncChannel)
        {
            return syncChannel.SendSync(message);
        }
        else if (channel is IRPCSendAsyncChannel asyncChannel)
        {
            asyncChannel.SendAsync(message);
        }
        return null;
    }


    private object? SendAsyncIfPossible(RPC_Message message, IRPCChannel? channel = null)
    {
        channel ??= Channel;

        if (channel is IRPCSendAsyncChannel asyncChannel)
        {
            asyncChannel.SendAsync(message);
        }
        else if (channel is IRPCSendSyncChannel syncChannel)
        {
            return syncChannel.SendSync(message);
        }
        return null;
    }

    private string RegisterLocalObj(object obj, ObjectDescriptor? descriptor = null)
    {
        descriptor ??= new ObjectDescriptor();

        if (hostObjectRegistry.ByObj.TryGetValue(obj, out var entry))
        {
            return entry.id;
        }
        var objId = ObjectIDGenerator();
        hostObjectRegistry.Add(objId, obj, descriptor);
        return objId;
    }

    private string RegisterLocalFunc(Delegate obj, FunctionDescriptor descriptor)
    {
        if (hostFunctionRegistry.ByObj.TryGetValue(obj, out var entry))
        {
            return entry.id;
        }
        var objId = ObjectIDGenerator();
        hostFunctionRegistry.Add(objId, obj, descriptor);
        return objId;
    }

    private Dictionary<Type, Func<object, Type, object?>> deserializers = new Dictionary<Type, Func<object, Type, object?>>();

    public void RegisterDeserializer(Type type, Func<object, Type, object?> deserializer)
    {
        deserializers.Add(type, deserializer);
    }

    private void CallAfterReplySent(CallContext context, Action action)
    {
        if (context.replySent is not null)
        {
            context.replySent.Task.ContinueWith(_ => action());
        }
        else
        {
            action();
        }
    }

    private async void SendResultOnTaskCompletion(Task task, Action<bool, object?> sendResult, CallContext context)
    {
        var taskType = task.GetType();
        try
        {
            await task;
        }
        catch (Exception)
        {
            // Could not find another way to wait for the task, but ignore any exceptions/failures.
        }

        if (taskType.IsGenericType && taskType.GetGenericArguments()[0].Name != "VoidTaskResult")
        {
            sendResult(!task.IsFaulted,
                task.IsFaulted ? task.Exception?.ToString() :
                ProcessValueBeforeSerialization(((dynamic)task).Result, context)
            );
        }
        else
        {
            sendResult(!task.IsFaulted, null);
        }
    }

    private bool ProcessPropertyValuesBeforeSerialization(object obj, PropertyInfo[] properties, Dictionary<string, object?> propertyBag, CallContext context, HashSet<object> marked)
    {
        var needToConvert = false;
        foreach (var propInfo in properties)
        {
            if (!propInfo.CanRead || propInfo.GetIndexParameters().Length > 0) continue;

            var value = propInfo.GetValue(obj);
            var newValue = ProcessValueBeforeSerialization(value, context, marked);
            propertyBag.Add(propInfo.Name, newValue);

            if (value is null ? newValue is not null : !value.Equals(newValue))
            {
                needToConvert = true;
            }
        }
        return needToConvert;
    }

    private object?[] ProcessArgumentsBeforeSerialization(object?[] args/* , Type[] parameterTypes */, FunctionDescriptor? func, CallContext context)
    {
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            // var type = parameterTypes[i];
            args[i] = ProcessValueBeforeSerialization(arg, context);
        }
        return args;
    }

    private RPC_Object? ProcessHostObject(object obj, Type? objType, CallContext context, HashSet<object> marked)
    {
        while (objType != null)
        {
            if (hostClassRegistry.ByObj.TryGetValue(objType, out var entry))
            {
                var descriptor = entry.value;
                var objId = RegisterLocalObj(obj, descriptor.Instance);
                var propertyBag = new Dictionary<string, object?>();

                if (descriptor.Instance?.ReadonlyProperties is not null)
                {
                    var propertyInfos = descriptor.Instance.ReadonlyProperties.Select(
                        prop => objType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance)
                    ).ToArray();
                    ProcessPropertyValuesBeforeSerialization(obj, propertyInfos!, propertyBag, context, marked);
                }
                return new RPC_Object(objId, "object", propertyBag, entry.id);
            }
            objType = objType.BaseType;
        }
        return null;
    }

    private object? ProcessValueBeforeSerialization(object? obj, CallContext context, HashSet<object>? marked = null)
    {
        marked ??= new HashSet<object>();

        if (obj is null) return null;

        if (marked.Contains(obj))
        {
            return null;
        }

        try
        {
            marked.Add(obj);

            var proxyId = proxyObjectRegistry.GetId(obj);
            if (proxyId is not null)
            {
                return obj is Delegate ? new RPC_BaseObj(proxyId, "hostfunction") : new RPC_BaseObj(proxyId, "hostobject");
            }

            var objType = obj.GetType();
            const BindingFlags PropBindFlags = BindingFlags.Public | BindingFlags.Instance;

            if (obj is Task task)
            {
                string? objId = null;
                if (!hostObjectRegistry.ByObj.ContainsKey(obj))
                {

                    void SendResult(bool success, object? result)
                    {
                        SendAsyncIfPossible(new RPC_AsyncFnResultMessage
                        {
                            success = success,
                            result = result,
                            callId = objId
                        }, context.replyChannel);
                    }

                    CallAfterReplySent(context, () => SendResultOnTaskCompletion(task, SendResult, context));
                }
                objId = RegisterLocalObj(obj);
                return new RPC_Object(objId, "object", null, "Promise");
            }

            var rpcObj = ProcessHostObject(obj, objType, context, marked);
            if (rpcObj is not null) return rpcObj;

            if (obj is Delegate func)
            {
                var objId = RegisterLocalFunc(func, new FunctionDescriptor());
                return new RPC_BaseObj(objId, "function");
            }

            if ((objType.IsArray || (objType.IsGenericType && objType.GetGenericTypeDefinition() == typeof(List<>)))
                && obj is System.Collections.IList array)
            {
                List<object?> list = new List<object?>();
                for (var i = 0; i < array.Count; i++)
                {
                    list.Add(ProcessValueBeforeSerialization(array[i], context, marked));
                }
                return list;
            }
            else
            if (objType.IsClass && objType != typeof(string))
            {
                var propertyInfos = objType.GetProperties(PropBindFlags);
                var propertyBag = new Dictionary<string, object?>();

                if (ProcessPropertyValuesBeforeSerialization(obj, propertyInfos, propertyBag, context, marked))
                {
                    var objId = RegisterLocalObj(obj);
                    return new RPC_Object(objId, "object", propertyBag);
                }
            }

            return obj;
        }
        finally
        {
            marked.Remove(obj);
        }
    }

    private object?[] ProcessArgumentsAfterDeserialization(object?[] args, CallContext context, Type[] parameterTypes, ArgumentDescriptor[]? argumentDescriptors)
    {
        if (args.Length != parameterTypes.Length)
        {
            throw new ArgumentException($"Method argument number mismatch. Expected {parameterTypes.Length} and got {args.Length}.");
        }

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            var descr = argumentDescriptors?.FirstOrDefault(ad => ad.idx == i || ad.idx is null);
            var type = parameterTypes[i];
            args[i] = ProcessValueAfterDeserialization(args[i], context, parameterTypes[i], descr);
        }

        return args;
    }

    private Func<object, Type, object?>? GetDeserializer(Type type)
    {
        if (deserializers.TryGetValue(type, out var deserializer))
        {
            return deserializer;
        }
        else if (deserializers.TryGetValue(typeof(object), out deserializer))
        {
            return deserializer;
        }
        return null;
    }

    private static Action<object?> CreateSetResultDelegate<T>(dynamic source)
    {
        return (object? result) => source.SetResult((T?)result);
    }

    private AsyncCallbackEntry CreateAsyncCallback(Type returnType)
    {
        dynamic source = typeof(TaskCompletionSource<>).MakeGenericType(returnType).GetConstructor(Type.EmptyTypes).Invoke(null);
        return new AsyncCallbackEntry(source.Task,
            (Action<object?>)GetType().GetMethod("CreateSetResultDelegate", BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(returnType).Invoke(null, new object[] { source }),
            (Action<Exception>)((Exception ex) => source.SetException(ex)),
            returnType);
    }

    private object? ProcessValueAfterDeserialization(object? obj, CallContext context, Type? type = null, ArgumentDescriptor? argDescriptor = null)
    {
        if (obj is null)
        {
            if (type?.IsValueType == true)
            {
                throw new ArgumentException("null cannot be passed as a value type");
            }
        }
        else
        {
            var objType = obj.GetType();

            if (obj is not RPC_BaseObj)
            {
                RPC_BaseObj? rpcObj1 = null;
                var rpcObjDeserializer = GetDeserializer(typeof(RPC_Object));
                if (rpcObjDeserializer is not null)
                {
                    rpcObj1 = rpcObjDeserializer(obj, typeof(RPC_Object)) as RPC_Object;
                }
                if (rpcObj1 is null)
                {
                    rpcObjDeserializer = GetDeserializer(typeof(RPC_BaseObj));
                    if (rpcObjDeserializer is not null)
                    {
                        rpcObj1 = rpcObjDeserializer(obj, typeof(RPC_BaseObj)) as RPC_BaseObj;
                    }
                }
                if (rpcObj1 is not null && rpcObj1._rpc_type is not null)
                {
                    obj = rpcObj1;
                    objType = obj.GetType();
                }
            }

            if (obj is RPC_BaseObj rpcObj && rpcObj?.objId is not null)
            {

                if (rpcObj._rpc_type == "object" && type is null &&
                    rpcObj is RPC_Object rpcObj2 && rpcObj2.classId is not null &&
                    proxyClassRegistry.ById.TryGetValue(rpcObj2.classId, out var proxyEntry))
                {
                    type = proxyEntry.obj;
                }

                // special cases for _rpc_type=[host]object/function
                switch (rpcObj._rpc_type)
                {
                    case "function":
                        {
                            // If the proxy function is "sync" we don't want to reply on the replyChannel, 
                            // because the fn call that this proxy function is passed into
                            // might also want to return its result synchronously.
                            var ctx = argDescriptor?.Returns == FunctionReturnBehavior.Sync || context.replyChannel is not IRPCSendAsyncChannel ?
                                new CallContext(Channel!) { replySent = context.replySent } : context;
                            if (type is not null)
                            {
                                obj = GetOrCreateProxyFunction(rpcObj.objId, type, ctx, argDescriptor);
                            }
                            break;
                        }
                    case "object":
                        {
                            // If a function on the proxy object is "sync" we don't want to reply on the replyChannel, 
                            // because the fn call that this proxy object is passed into
                            // might also want to return its result synchronously.
                            var channel = context.replyChannel as IRPCSendAsyncChannel ?? Channel;
                            if (type is not null && rpcObj is RPC_Object rpcObj3)
                            {
                                var props = ProcessValueAfterDeserialization(rpcObj3.props, context) as Dictionary<string, object?>;
                                obj = GetOrCreateProxyInstance(rpcObj.objId, rpcObj3.classId, props, type, channel!);
                            }
                            break;
                        }
                    case "hostobject":
                        {
                            if (hostObjectRegistry.ById.TryGetValue(rpcObj.objId, out var entry))
                            {
                                obj = entry.obj;
                            }
                            else
                            {
                                throw new ArgumentException($"No host object found with ID '{rpcObj.objId}'.");
                            }
                            break;
                        }
                    case "hostfunction":
                        {
                            if (hostFunctionRegistry.ById.TryGetValue(rpcObj.objId, out var entry))
                            {
                                obj = entry.obj;
                            }
                            else
                            {
                                throw new ArgumentException($"No host function found with ID '{rpcObj.objId}'.");
                            }
                            break;
                        }
                    default: throw new ArgumentException($"Invalid _rpc_type '{rpcObj._rpc_type}'.");
                }
                objType = obj.GetType();
            }

            if (type is not null)
            {
                // custom deserializers
                var deserializer = GetDeserializer(type);
                if (deserializer is not null)
                {
                    var obj1 = deserializer(obj, type);
                    if (obj1 is not null)
                    {
                        obj = obj1;
                        objType = obj.GetType();
                    }
                }

                if (!objType.IsAssignableTo(type) && obj is IConvertible)
                {
                    obj = Convert.ChangeType(obj, type);
                }
            }

            // recursive call for Dictionary
            if (obj is IDictionary<string, object?> dict)
            {
                foreach (var (key, value) in dict)
                {
                    dict[key] = ProcessValueAfterDeserialization(value, context);
                }
            }
        }
        return obj;
    }

    private void SendObjectDied(string objId, IRPCChannel? replyChannel = null)
    {
        SendAsyncIfPossible(new RPC_ObjectDiedMessage { objId = objId }, replyChannel ?? Channel);
    }

    private void AddPropsToObject(Dictionary<string, object?>? props, object obj)
    {
        if (props is not null)
        {
            var objType = obj.GetType();
            foreach (var (name, value) in props)
            {
                // We know that the generated dynamic class has a private backing field with name "_<PropertyName>"
                var fieldInfo = objType.GetField("_" + name, BindingFlags.NonPublic | BindingFlags.Instance);
                if (fieldInfo is not null)
                {
                    fieldInfo.SetValue(obj, value);
                    continue;
                }
                var propInfo = objType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (propInfo is not null)
                {
                    propInfo.SetValue(obj, value);
                }
            }
        }
    }

    public T GetProxyObject<T>(string objId, IRPCChannel? channel = null)
    {
        var obj = (T?)proxyObjectRegistry.Get(objId);
        if (obj is not null) return obj;

        if (remoteObjectDescriptors?.TryGetValue(objId, out var descriptor) is not true)
        {
            throw new ArgumentException($"No descriptor found for object ID {objId}.");
        }

        var factory = GetProxyClassFactory(objId + ".class", typeof(T), descriptor, channel);
        obj = (T)factory(objId);

        AddPropsToObject(descriptor?.Props, obj);

        proxyObjectRegistry.Register(objId, obj);
        return obj;
    }

    private object GetOrCreateProxyInstance(string objId, string? classId, Dictionary<string, object?>? props, Type type, IRPCChannel replyChannel)
    {
        var obj = proxyObjectRegistry.Get(objId);
        if (obj is not null) return obj;

        if (classId == "Promise")
        {
            var resultType = type == typeof(Task) ? typeof(object) : UnwrapTaskReturnType(type);
            var asyncCallback = CreateAsyncCallback(resultType);

            object? asyncResult = null;
            lock (asyncCallbacks)
            {
                if (!asyncCallbackValues.Remove(objId, out asyncResult))
                {
                    asyncCallbacks.Add(objId, asyncCallback);
                }
            }

            if (asyncResult is not null)
            {
                if (asyncResult is Exception asyncFailure)
                {
                    asyncCallback.fail(asyncFailure);
                }
                else
                {
                    var result = ProcessValueAfterDeserialization(asyncResult, CurrentContext!, resultType);
                    asyncCallback.complete(result);
                }
            }

            obj = asyncCallback.task;
        }
        else
        if (proxyClassRegistry.ByObj.TryGetValue(type, out var proxyClassEntry))
        {
            var factory = proxyClassEntry.value;
            if (factory is null)
            {
                factory = GetProxyClassFactory(proxyClassEntry.id, replyChannel);
                proxyClassRegistry.Add(proxyClassEntry.id, proxyClassEntry.obj, factory);
            }
            obj = factory(objId);
            AddPropsToObject(props, obj);
        }
        else
            throw new ArgumentException($"No proxy class registered for type {type.FullName}.");

        proxyObjectRegistry.Register(objId, obj, () => SendObjectDied(objId, replyChannel));
        return obj;
    }

    private Delegate GetOrCreateProxyFunction(string objId, Type type, CallContext context, ArgumentDescriptor? argDescriptor)
    {
        var obj = (Delegate?)proxyObjectRegistry.Get(objId);
        if (obj is not null) return obj;

        obj = CreateProxyFunctionFromDelegateType(type, objId, argDescriptor ?? new FunctionDescriptor(), context);
        proxyObjectRegistry.Register(objId, obj, () => SendObjectDied(objId, context.replyChannel));
        return obj;
    }

    private Delegate CreateVoidProxyFunction<TReturn>(string? objId, FunctionDescriptor? func, string action, CallContext context)
    {

        TReturn? ProxyFunction(string instanceObjId, object?[] args)
        {
            SendAsyncIfPossible(new RPC_AnyCallTypeFnCallMessage
            {
                action = action,
                callType = FunctionReturnBehavior.Void,
                objId = objId ?? instanceObjId,
                prop = func?.Name,
                args = ProcessArgumentsBeforeSerialization(args, func, context)
            }, context.replyChannel);
            return default;
        }

        return ProxyFunction;
    }

    private Delegate CreateSyncProxyFunction<TReturn>(string? objId, FunctionDescriptor? func, string action, CallContext context)
    {

        TReturn? ProxyFunction(string instanceObjId, object?[] args)
        {
            var response = (RPC_SyncFnResultMessage?)SendSyncIfPossible(new RPC_AnyCallTypeFnCallMessage
            {
                action = action,
                callType = FunctionReturnBehavior.Sync,
                objId = objId ?? instanceObjId,
                prop = func?.Name,
                args = ProcessArgumentsBeforeSerialization(args, func, context)
            }, context.replyChannel);
            if (response is null)
            {
                throw new ArgumentException($"No response received");
            }
            if (!response.success)
            {
                throw new ArgumentException(response.result?.ToString());
            }

            return (TReturn?)ProcessValueAfterDeserialization(response.result, context);
        }

        return ProxyFunction;
    }

    private Delegate CreateAsyncProxyFunction<TReturn>(string? objId, FunctionDescriptor? func, string action, CallContext context)
    {

        Task<TReturn?> ProxyFunction(string instanceObjId, object?[] args)
        {
            callId++;

            var ownCtx = context.replySent is null;
            var ctx = ownCtx ? new CallContext(context.replyChannel) { replySent = new TaskCompletionSource() } : context;

            var asyncCallback = CreateAsyncCallback(UnwrapTaskReturnType(typeof(TReturn)));
            try
            {
                lock (asyncCallbacks)
                    asyncCallbacks.Add(callId.ToString(), asyncCallback);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            SendAsyncIfPossible(new RPC_AnyCallTypeFnCallMessage
            {
                action = action,
                callType = FunctionReturnBehavior.Async,
                callId = callId.ToString(),
                objId = objId ?? instanceObjId,
                prop = func?.Name,
                args = ProcessArgumentsBeforeSerialization(args, func, ctx)
            }, ctx.replyChannel);

            if (ownCtx)
            {
                ctx.replySent!.SetResult();
            }

            return (Task<TReturn?>)asyncCallback.task;
        }

        return ProxyFunction;
    }

    private Delegate CreateProxyFunction<TReturn>(
        string objId,
        FunctionDescriptor? descriptor,
        string action,
        CallContext context,
        FunctionReturnBehavior defaultCallType = FunctionReturnBehavior.Async)
    {
        var replyChannel = context.replyChannel ?? Channel;
        var callType = descriptor?.Returns ?? defaultCallType;

        if (callType == FunctionReturnBehavior.Async && replyChannel is not IRPCSendAsyncChannel) callType = FunctionReturnBehavior.Sync;
        if (callType == FunctionReturnBehavior.Sync && replyChannel is not IRPCSendSyncChannel) callType = FunctionReturnBehavior.Async;

        return callType switch
        {
            FunctionReturnBehavior.Void => CreateVoidProxyFunction<TReturn>(objId, descriptor, action, context),
            FunctionReturnBehavior.Sync => CreateSyncProxyFunction<TReturn>(objId, descriptor, action, context),
            _ => CreateAsyncProxyFunction<TReturn>(objId, descriptor, action, context)
        };
    }

    private Delegate CreateProxyFunctionWithReturnType(
        Type returnType,
        string? objId,
        FunctionDescriptor? descriptor,
        string action,
        CallContext context,
        FunctionReturnBehavior defaultCallType = FunctionReturnBehavior.Async)
    {
        if (returnType == typeof(void))
        {
            returnType = typeof(object);
        }
        return (Delegate)(typeof(SuperRPC))
            .GetMethod("CreateProxyFunction", BindingFlags.NonPublic | BindingFlags.Instance)
            .MakeGenericMethod(returnType)
            .Invoke(this, new object[] { objId, descriptor, action, context, defaultCallType });
    }

    private Delegate CreateDynamicWrapperMethod(string methodName, Delegate proxyFunction, Type delegateType, MethodInfo method)
    {
        var paramTypes = method.GetParameters().Select(pi => pi.ParameterType).ToArray();

        var dmethod = new DynamicMethod(methodName,
            method.ReturnType,
            paramTypes.Prepend(proxyFunction.Target!.GetType()).ToArray(),
            proxyFunction.Target.GetType(), true);

        var il = dmethod.GetILGenerator();

        il.Emit(OpCodes.Ldarg_0);       // "this" (ref of the instance of the class generated for proxyFunction)

        il.Emit(OpCodes.Ldnull);        // "null" to the stack -> instanceObjId

        il.Emit(OpCodes.Ldc_I4, paramTypes.Length);
        il.Emit(OpCodes.Newarr, typeof(object));    //arr = new object[paramTypes.Length]

        for (var i = 0; i < paramTypes.Length; i++)
        {
            il.Emit(OpCodes.Dup);               // arr ref
            il.Emit(OpCodes.Ldc_I4, i);         // int32: idx
            il.Emit(OpCodes.Ldarg, i + 1);      // arg(i+1)
            if (paramTypes[i].IsValueType)
            {
                il.Emit(OpCodes.Box, paramTypes[i]);
            }
            il.Emit(OpCodes.Stelem_Ref);        // arr[idx] = arg
        }

        il.Emit(OpCodes.Call, proxyFunction.Method);
        if (method.ReturnType == typeof(void))
        {
            il.Emit(OpCodes.Pop);
        }
        il.Emit(OpCodes.Ret);

        return dmethod.CreateDelegate(delegateType, proxyFunction.Target);
    }

    private static bool IsTaskType(Type type)
    {
        return type == typeof(Task) || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>);
    }
    private static Type UnwrapTaskReturnType(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
        {
            type = type.GetGenericArguments()[0];
        }
        return type;
    }

    public T GetProxyFunction<T>(string objId, CallContext? context = null) where T : Delegate
    {
        context ??= new CallContext(Channel!);

        var obj = (T?)proxyObjectRegistry.Get(objId);
        if (obj is not null) return obj;

        if (remoteFunctionDescriptors?.TryGetValue(objId, out var descriptor) is not true)
        {
            throw new ArgumentException($"No object descriptor found with ID '{objId}'.");
        }

        obj = (T)CreateProxyFunctionFromDelegateType(typeof(T), objId, descriptor, context);
        proxyObjectRegistry.Register(objId, obj);
        return obj;
    }

    private Delegate CreateProxyFunctionFromDelegateType(Type delegateType, string objId, FunctionDescriptor descriptor, CallContext context)
    {
        var method = delegateType.GetMethod("Invoke");
        if (method is null)
        {
            throw new ArgumentException($"Given generic type is not a generic delegate ({delegateType.FullName})");
        }

        var proxyFunc = CreateProxyFunctionWithReturnType(UnwrapTaskReturnType(method.ReturnType), objId, descriptor, "fn_call", context);

        return CreateDynamicWrapperMethod(objId + "_" + descriptor.Name, proxyFunc, delegateType, method);
    }

    private ObjectIdDictionary<string, Type, Func<string, object>?>.Entry GetProxyClassEntry(string classId)
    {
        if (!proxyClassRegistry.ById.TryGetValue(classId, out var proxyClassEntry))
        {
            throw new ArgumentException($"No proxy class interface registered with ID '{classId}'.");
        }
        return proxyClassEntry;
    }

    public Func<string, T> GetProxyClass<T>(string classId)
    {
        if (remoteClassDescriptors?.TryGetValue(classId, out var descriptor) is not true)
        {
            throw new ArgumentException($"No class descriptor found with ID '{classId}'.");
        }

        var proxyFunc = CreateProxyFunction<T>(classId, descriptor.Ctor, "ctor_call", new CallContext(Channel!));
        return (string id) => (T)proxyFunc.DynamicInvoke(id, new object[0])!;
    }

    private Func<string, object> GetProxyClassFactory(string classId, IRPCChannel? channel = null)
    {
        var entry = GetProxyClassEntry(classId);
        if (entry.value is not null)
        {
            return entry.value;
        }

        var factory = CreateProxyClass(classId, channel);
        proxyClassRegistry.Add(classId, entry.obj, factory);
        return factory;
    }

    private Func<string, object> GetProxyClassFactory(string classId, Type ifType, ObjectDescriptor descriptor, IRPCChannel? channel = null)
    {
        if (proxyClassRegistry.ById.TryGetValue(classId, out var proxyClassEntry) && proxyClassEntry?.value is not null)
        {
            return proxyClassEntry.value;
        }

        var factory = CreateProxyClass(classId, ifType, descriptor, channel);
        proxyClassRegistry.Add(classId, ifType, factory);
        return factory;
    }

    private Func<string, object> CreateProxyClass(string classId, IRPCChannel? channel = null)
    {
        if (remoteClassDescriptors?.TryGetValue(classId, out var descriptor) is not true)
        {
            throw new ArgumentException($"No class descriptor found with ID '{classId}'.");
        }

        var ifType = GetProxyClassEntry(classId).obj;

        if (ifType.IsClass)
        {
            var ctor = ifType.GetConstructor(Type.EmptyTypes);
            if (ctor is null) throw new InvalidOperationException($"Proxy class {ifType.FullName} has no parameterless constructor.");
            return (string objId) => ctor.Invoke(null);
        }

        if (!ifType.IsInterface)
        {
            throw new InvalidOperationException($"Proxy class {ifType.FullName} is not a class or an interface.");
        }

        return CreateProxyClass(classId, ifType, descriptor.Instance!, channel);
    }

    private Func<string, object> CreateProxyClass(string classId, Type ifType, ObjectDescriptor descriptor, IRPCChannel? channel = null)
    {
        channel ??= Channel!;

        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"SuperRPC_dynamic({Guid.NewGuid()})"), AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
        var typeBuilder = moduleBuilder.DefineType(classId,
                TypeAttributes.Public |
                TypeAttributes.Class |
                TypeAttributes.AutoClass |
                TypeAttributes.AnsiClass |
                TypeAttributes.BeforeFieldInit |
                TypeAttributes.AutoLayout,
                null, new[] { ifType });

        var objIdField = typeBuilder.DefineField("objId", typeof(string), FieldAttributes.Public | FieldAttributes.InitOnly);
        var proxyFunctionsField = typeBuilder.DefineField("proxyFunctions", typeof(Delegate[]), FieldAttributes.Public | FieldAttributes.Static);

        var proxyFunctions = new List<Delegate>();
        var skipMethods = new List<string>();

        var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(string) });
        var ctorIL = constructorBuilder.GetILGenerator();

        // call base()
        ctorIL.Emit(OpCodes.Ldarg_0);
        ConstructorInfo superConstructor = typeof(Object).GetConstructor(Type.EmptyTypes)!;

        ctorIL.Emit(OpCodes.Call, superConstructor);

        ctorIL.Emit(OpCodes.Ldarg_0);   // this
        ctorIL.Emit(OpCodes.Ldarg_1);   // objId ref
        ctorIL.Emit(OpCodes.Stfld, objIdField); // this.objId = arg1

        // ctorIL.Emit(OpCodes.Ldarg_0);   // this
        // ctorIL.Emit(OpCodes.Ldarg_2);   // proxyFunctions ref
        // ctorIL.Emit(OpCodes.Stfld, proxyFunctionsField); // this.proxyFunctions = arg2

        ctorIL.Emit(OpCodes.Ret);

        ClassDescriptor? classDescriptor = null;
        remoteClassDescriptors?.TryGetValue(classId, out classDescriptor);

        // Static methods
        // var smethods = ifType.GetMethods(BindingFlags.Public | BindingFlags.Static);

        // foreach (var methodInfo in smethods) {
        //     if (skipMethods.Contains(methodInfo.Name)) continue;

        //     var funcDescriptor = classDescriptor?.Static?.Functions?.FirstOrDefault(desc => desc.Name == methodInfo.Name);    // TODO: camelCase <-> PascalCase ?
        //     if (funcDescriptor is null) {
        //         throw new ArgumentException($"No function descriptor found for method '{methodInfo.Name}' in class '{classId}'.");
        //     }

        //     var paramInfos = methodInfo.GetParameters();
        //     var paramTypes = paramInfos.Select(pi => pi.ParameterType).ToArray();

        //     var methodBuilder = typeBuilder.DefineMethod(methodInfo.Name,
        //         MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.Virtual,
        //         CallingConventions.Standard,
        //         methodInfo.ReturnType,
        //         // methodInfo.ReturnParameter.GetRequiredCustomModifiers(),
        //         // methodInfo.ReturnParameter.GetOptionalCustomModifiers(),
        //         paramTypes
        //         // paramInfos.Select(pi => pi.GetRequiredCustomModifiers()).ToArray(),
        //         // paramInfos.Select(pi => pi.GetOptionalCustomModifiers()).ToArray()
        //     );

        //     GenerateILMethod(methodBuilder.GetILGenerator(), objIdField, proxyFunctionsField, proxyFunctions.Count, paramTypes, methodInfo.ReturnType, isStatic: true);
        //     proxyFunctions.Add(CreateProxyFunctionWithReturnType(UnwrapTaskReturnType(methodInfo.ReturnType), classId, funcDescriptor, "method_call", new CallContext(channel)));
        // }

        // Events
        var events = ifType.GetEvents(BindingFlags.Public | BindingFlags.Instance);

        foreach (var eventInfo in events)
        {
            if (eventInfo.EventHandlerType is null)
            {
                throw new InvalidOperationException($"EventHandlerType is null in EventInfo {eventInfo.Name}");
            }

            var addMethodName = "add_" + eventInfo.Name;
            var removeMethodName = "remove_" + eventInfo.Name;

            skipMethods.Add(addMethodName);
            skipMethods.Add(removeMethodName);

            var eventBuilder = typeBuilder.DefineEvent(eventInfo.Name, EventAttributes.None, eventInfo.EventHandlerType);

            // add method
            var addMethodParams = new[] { eventInfo.EventHandlerType };
            var addMethodBuilder = typeBuilder.DefineMethod(addMethodName,
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                CallingConventions.Standard | CallingConventions.HasThis, typeof(void), addMethodParams);

            eventBuilder.SetAddOnMethod(addMethodBuilder);

            var addDescriptor = descriptor?.Functions?.FirstOrDefault(func => func.Name == addMethodName);

            GenerateILMethod(addMethodBuilder.GetILGenerator(), objIdField, proxyFunctionsField, proxyFunctions.Count, addMethodParams, typeof(void));
            proxyFunctions.Add(CreateProxyFunctionWithReturnType(typeof(void), null,
                addDescriptor ?? new FunctionDescriptor { Name = addMethodName, Returns = FunctionReturnBehavior.Sync }, "method_call", new CallContext(channel)));

            // remove method
            var removeMethodParams = new[] { eventInfo.EventHandlerType };
            var removeMethodBuilder = typeBuilder.DefineMethod(removeMethodName,
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                CallingConventions.Standard | CallingConventions.HasThis, typeof(void), removeMethodParams);

            eventBuilder.SetRemoveOnMethod(removeMethodBuilder);

            var removeDescriptor = descriptor?.Functions?.FirstOrDefault(func => func.Name == removeMethodName);

            GenerateILMethod(removeMethodBuilder.GetILGenerator(), objIdField, proxyFunctionsField, proxyFunctions.Count, removeMethodParams, typeof(void));
            proxyFunctions.Add(CreateProxyFunctionWithReturnType(typeof(void), null,
                removeDescriptor ?? new FunctionDescriptor { Name = removeMethodName, Returns = FunctionReturnBehavior.Sync /*TODO: args? */ }, "method_call", new CallContext(channel)));
        }

        // Properties
        var properties = ifType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var propertyInfo in properties)
        {
            var isProxied = true;

            var propDescriptor = descriptor?.ProxiedProperties?.FirstOrDefault(desc => desc.Name == propertyInfo.Name);    // TODO: camelCase <-> PascalCase ?
            if (propDescriptor is null)
            {
                if (descriptor?.ReadonlyProperties?.Contains(propertyInfo.Name) is not true)
                { // TODO: camelCase <-> PascalCase ?
                    throw new ArgumentException($"No property descriptor found for property '{propertyInfo.Name}' in class '{classId}'.");
                }
                isProxied = false;
            }

            var isGetOnly = propDescriptor?.GetOnly ?? false;

            var propertyBuilder = typeBuilder.DefineProperty(propertyInfo.Name,
                PropertyAttributes.HasDefault,
                propertyInfo.PropertyType,
                null);

            // The property set and property get methods require a special
            // set of attributes.
            var getSetAttr = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

            // Define the "get" accessor method
            var getterName = "get_" + propertyInfo.Name;
            skipMethods.Add(getterName);
            var getPropMthdBldr = typeBuilder.DefineMethod(getterName,
                getSetAttr,
                propertyInfo.PropertyType,
                Type.EmptyTypes);

            var getterIL = getPropMthdBldr.GetILGenerator();
            propertyBuilder.SetGetMethod(getPropMthdBldr);

            ILGenerator? setterIL = null;
            if (!isGetOnly)
            {
                // Define the "set" accessor method 
                var setterName = "set_" + propertyInfo.Name;
                skipMethods.Add(setterName);
                var setPropMthdBldr = typeBuilder.DefineMethod(setterName,
                    getSetAttr,
                    null,
                    new[] { propertyInfo.PropertyType });

                setterIL = setPropMthdBldr.GetILGenerator();
                propertyBuilder.SetSetMethod(setPropMthdBldr);
            }

            if (isProxied)
            {
                GenerateILMethod(getterIL, objIdField, proxyFunctionsField, proxyFunctions.Count, Type.EmptyTypes, propertyInfo.PropertyType);
                proxyFunctions.Add(CreateProxyFunctionWithReturnType(
                    UnwrapTaskReturnType(propertyInfo.PropertyType),
                    null,
                    propDescriptor?.Get ?? new FunctionDescriptor { Name = propDescriptor!.Name, Returns = FunctionReturnBehavior.Sync },
                    "prop_get",
                    new CallContext(channel)));

                if (!isGetOnly)
                {
                    GenerateILMethod(setterIL!, objIdField, proxyFunctionsField, proxyFunctions.Count, new[] { propertyInfo.PropertyType }, typeof(void));
                    proxyFunctions.Add(CreateProxyFunctionWithReturnType(typeof(void),
                        null,
                        propDescriptor?.Set ?? new FunctionDescriptor
                        {
                            Name = propDescriptor!.Name,
                            Returns = channel is IRPCSendSyncChannel ? FunctionReturnBehavior.Sync : FunctionReturnBehavior.Void
                        },
                        "prop_set",
                        new CallContext(channel)));
                }
            }
            else
            {    // not proxied -> "readonly"
                // backing field
                var backingFieldBuilder = typeBuilder.DefineField("_" + propertyInfo.Name,
                    propertyInfo.PropertyType,
                    FieldAttributes.Private);

                getterIL.Emit(OpCodes.Ldarg_0);
                getterIL.Emit(OpCodes.Ldfld, backingFieldBuilder);
                getterIL.Emit(OpCodes.Ret);

                setterIL!.Emit(OpCodes.Ldarg_0);
                setterIL.Emit(OpCodes.Ldarg_1);
                setterIL.Emit(OpCodes.Stfld, backingFieldBuilder);
                setterIL.Emit(OpCodes.Ret);
            }
        }

        // Methods
        var methods = ifType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        foreach (var methodInfo in methods)
        {
            if (skipMethods.Contains(methodInfo.Name)) continue;

            var funcDescriptor = descriptor?.Functions?.FirstOrDefault(desc => desc.Name == methodInfo.Name);    // TODO: camelCase <-> PascalCase ?
            if (funcDescriptor is null)
            {
                throw new ArgumentException($"No function descriptor found for method '{methodInfo.Name}' in class '{classId}'.");
            }

            var paramInfos = methodInfo.GetParameters();
            var paramTypes = paramInfos.Select(pi => pi.ParameterType).ToArray();

            var methodBuilder = typeBuilder.DefineMethod(methodInfo.Name,
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final,
                CallingConventions.HasThis,
                methodInfo.ReturnType,
                methodInfo.ReturnParameter.GetRequiredCustomModifiers(),
                methodInfo.ReturnParameter.GetOptionalCustomModifiers(),
                paramTypes,
                paramInfos.Select(pi => pi.GetRequiredCustomModifiers()).ToArray(),
                paramInfos.Select(pi => pi.GetOptionalCustomModifiers()).ToArray()
            );

            GenerateILMethod(methodBuilder.GetILGenerator(), objIdField, proxyFunctionsField, proxyFunctions.Count, paramTypes, methodInfo.ReturnType);
            proxyFunctions.Add(CreateProxyFunctionWithReturnType(UnwrapTaskReturnType(methodInfo.ReturnType), null, funcDescriptor, "method_call", new CallContext(channel)));
        }

        var proxyFunctionsArr = proxyFunctions.ToArray();
        try
        {
            var type = typeBuilder.CreateType();
            type!.GetField(proxyFunctionsField.Name, BindingFlags.Public | BindingFlags.Static)!.SetValue(type, proxyFunctionsArr);

            object CreateInstance(string objId)
            {
                return Activator.CreateInstance(type, objId)!;
            }
            return CreateInstance;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }

        return null;
       
    }

    void GenerateILMethod(ILGenerator il, FieldBuilder objIdField, FieldBuilder proxyFunctionsField, int funcIdx, Type[] paramTypes, Type returnType, bool isStatic = false)
    {
        il.Emit(OpCodes.Ldsfld, proxyFunctionsField);    // ref of Delegate[] containing proxy functions
        il.Emit(OpCodes.Ldc_I4, funcIdx);
        il.Emit(OpCodes.Ldelem_Ref);                    // proxyFunction [Delegate] is on the stack now

        il.Emit(OpCodes.Ldc_I4_2);
        il.Emit(OpCodes.Newarr, typeof(object));    //arr2 = new object[2]

        il.Emit(OpCodes.Dup);               // arr2
        il.Emit(OpCodes.Ldc_I4_0);          // 0

        if (isStatic)
        {
            il.Emit(OpCodes.Ldnull);
        }
        else
        {
            il.Emit(OpCodes.Ldarg_0);           // "this" 
            il.Emit(OpCodes.Ldfld, objIdField); // push(this.objId)
        }
        il.Emit(OpCodes.Stelem_Ref);        // arr2[0] = objId

        il.Emit(OpCodes.Dup);               // arr2
        il.Emit(OpCodes.Ldc_I4_1);          // 1

        il.Emit(OpCodes.Ldc_I4, paramTypes.Length);
        il.Emit(OpCodes.Newarr, typeof(object));    //arr1 = new object[paramTypes.Length]

        for (var i = 0; i < paramTypes.Length; i++)
        {
            il.Emit(OpCodes.Dup);               // arr ref
            il.Emit(OpCodes.Ldc_I4, i);         // int32: idx
            il.Emit(OpCodes.Ldarg, i + 1);      // arg(i+1)
            if (paramTypes[i].IsValueType)
            {
                il.Emit(OpCodes.Box, paramTypes[i]);
            }
            il.Emit(OpCodes.Stelem_Ref);        // arr1[idx] = arg
        }

        il.Emit(OpCodes.Stelem_Ref);    // arr2[1] = arr1

        il.Emit(OpCodes.Callvirt, typeof(Delegate).GetMethod("DynamicInvoke")!);

        if (returnType == typeof(void))
        {
            il.Emit(OpCodes.Pop);
        }
        else if (returnType.IsValueType)
        {
            il.Emit(OpCodes.Unbox_Any, returnType);
        }

        il.Emit(OpCodes.Ret);
    }
}
