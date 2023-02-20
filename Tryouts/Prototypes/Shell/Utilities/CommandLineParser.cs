using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.CommandLine;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Shell.Utilities;

/// <summary>
/// Simple helper class that parses strongly typed objects from command line arguments.
/// </summary>
/// <remarks>
/// No checks or conversions are performed at the moment, the properties of the
/// deserialized class must be compatible with System.CommandLine options.
/// Nullable primitive types and strings are supported.
/// To add a description for a property, annotate with <see cref="DisplayAttribute"/> and use the <see cref="DisplayAttribute.Description"/> property.
/// </remarks>
public static class CommandLineParser
{
    public static T Parse<T>(string[] args) where T : new()
    {
        return (T)_parsers.GetOrAdd(typeof(T), CreateParser)(args);
    }

    public static bool TryParse<T>(string[] args, out T value) where T : new()
    {
        try
        {
            value = Parse<T>(args);

            return true;
        }
        catch
        {
            value = default!;

            return false;
        }
    }

    private static readonly ConcurrentDictionary<Type, Func<string[], object>> _parsers = new();

    private static Func<string[], object> CreateParser(Type type)
    {
        var rootCommand = new RootCommand();
        var optionToProperty = new Dictionary<Option, PropertyInfo>();

        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                     .Where(p => p.DeclaringType != typeof(object)))
        {
            var displayAttribute = property.GetCustomAttribute<DisplayAttribute>();
            var optionName = displayAttribute?.Name ?? ToCamelCase(property.Name);

            var option = (Option)Activator.CreateInstance(
                typeof(Option<>).MakeGenericType(property.PropertyType),
                "--" + optionName,
                displayAttribute?.Description ?? property.Name)!;

            rootCommand.Add(option);
            optionToProperty.Add(option, property);
        }

        var parser = new Parser(rootCommand);

        // TODO: Build and compile a lambda instead for better performance
        return args =>
        {
            var parseResult = parser.Parse(args);
            var result = Activator.CreateInstance(type)!;

            foreach (var mapping in optionToProperty)
            {
                var value = parseResult.GetValueForOption(mapping.Key);

                if (value != null)
                {
                    mapping.Value.SetValue(result, value);
                }
            }

            return result;
        };
    }

    private static string? ToCamelCase(string? value) =>
        value switch
        {
            null => null,
            "" => "",
            _ => char.ToLower(value[0]) + value[1..]
        };
}
