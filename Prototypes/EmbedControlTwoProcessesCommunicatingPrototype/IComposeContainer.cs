using System;
using System.Reflection;

// no need for scoped
public enum ObjLifetime
{
    Singleton,
    Transient
}

public interface IComposeContainer
{
    // adds a transient object to the container
    // Transient object is created anew each time it is requested from the container
    void AddTransient(Type typeToResolve, Type implementationType, object? key = null);

    // adds a transient object to the container (generics version)
    // Transient object is created anew each time it is requested from the container
    void AddTransient<TImplementaion, TToResolve>(object? key = null);

    // adds a transient object to the container (factory method version)
    // Transient object is created anew each time it is requested from the container
    void AddTransient<TToResolve>(Func<TToResolve> factoryMethod, object? key = null);

    // adds a singleton object created by type to the container
    void AddSingleton(Type typeToResolve, Type implementationType, object? key = null);

    // adds a singleton object created by type to the container (generics version)
    void AddSingleton<TImplementaion, TToResolve>(object? key = null);

    // adds a singleton object created by type to the container (factory method version)
    void AddSingleton<TToResolve>(Func<TToResolve> factoryMethod, object? key = null);

    // adds a singleton object created outside of the container to the container
    void AddSingleton(Type typeToResolve, object singletonObject, object? key = null);

    void AddSingleton<TToResolve>(TToResolve singletonObject, object? key = null);


    // should be called after all the Type cells are already in, but before
    // the object resolution began
    // it checks for dependencise and assembles all singletons making
    // the container more performant
    void PrepareContainer();

    // returns the object by TypeToResolve and a key (if needed)
    object Resolve(Type typeToResolve, object? key = null);

    // returns the object by TypeToResolve and a key (if needed) - generics version
    TToResolve Resolve<TToResolve>(object? key = null);

    // adds the type marked with Export attribute to the container
    void AddType(Type type);

    // add all types marked by Export attribute within the assembly to the container
    void AddAssembly(Assembly assembly);

    // Load the dll dynamically and add all its types marked with Export attribute to the container
    void AddPlugin(string pathToPluginDll);
}


// attributes (similar to MEF)

// class attribute
// the class marked by this attribute will be added to the container 
[Export(Type typeToResolve, object? key, ObjLifetime lifetime)]

// should work on properties and object constructor parameters
[Import(Type typeToResolve, object? key)]

// constructors whose parameters are imported from the container should be
// marked with this attribute
[ComposingConstructor]