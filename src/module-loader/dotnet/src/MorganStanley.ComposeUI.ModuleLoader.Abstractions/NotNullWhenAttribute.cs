namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field, Inherited = false)]
internal sealed class NotNullWhenAttribute(bool returnValue) : Attribute
{
}