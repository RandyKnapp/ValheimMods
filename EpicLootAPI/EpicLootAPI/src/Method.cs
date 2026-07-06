using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace EpicLootAPI;

/// <summary>
/// Helper class for dynamically invoking static methods from external assemblies using reflection.
/// Provides caching of Type instances for improved performance and simplified method invocation.
/// Useful for calling methods in plugins or external libraries without direct references.
/// </summary>
internal class Method
{
    private const string Namespace = "EpicLoot";
    private const string ClassName = "API";
    private const string Assembly = "EpicLoot";
    private const string API_LOCATION = Namespace + "." + ClassName + ", " + Assembly;

    /// <summary>
    /// Cache of previously resolved Type instances to avoid repeated Type.GetType() calls.
    /// Key: Full type name with assembly (e.g., "MyNamespace.MyClass, MyAssembly")
    /// Value: Resolved Type instance
    /// </summary>
    private static readonly Dictionary<string, Type> CachedTypes = new();
    private readonly MethodInfo info;

    /// <summary>
    /// Invokes the cached static method with the provided arguments.
    /// This method performs no parameter validation - ensure arguments match the target method signature.
    /// </summary>
    /// <param name="args">
    /// Arguments to pass to the target method. The array length, types, and order must exactly match 
    /// the target method's parameter signature. Passing incorrect arguments will result in runtime exceptions.
    /// </param>
    /// <returns>
    /// The return value from the invoked method and the arguments passed, or null if:
    /// - The method returns void
    /// - The method could not be resolved during construction
    /// - The method invocation fails
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when argument types don't match the method signature</exception>
    /// <exception cref="TargetParameterCountException">Thrown when argument count doesn't match the method signature</exception>
    /// <exception cref="TargetInvocationException">Thrown when the invoked method throws an exception</exception>
    public object[] Invoke(params object[] args)
    {
        object result = info?.Invoke(null, args);
        object[] output = new object[args.Length + 1];
        output[0] = result;
        Array.Copy(args, 0, output, 1, args.Length);
        return output;
    }

    /// <summary>
    /// Constructs a Method helper that resolves and caches a static method for later invocation.
    /// Uses reflection to locate the specified method in the target type and assembly.
    /// </summary>
    /// <param name="typeNameWithAssembly">
    /// The fully qualified type name including assembly information.
    /// Format: "Namespace.ClassName, AssemblyName"
    /// </param>
    /// <param name="methodName">
    /// The exact name of the static method to locate within the specified type.
    /// Method names are case-sensitive and must match exactly.
    /// </param>
    /// <param name="bindingFlags"></param>
    /// <remarks>
    /// Construction Process:
    /// 1. Checks the type cache for previously resolved types
    /// 2. If not cached, uses Type.GetType() to resolve the type from the assembly
    /// 3. Caches the resolved type for future use
    /// 4. Uses reflection to find the specified static method
    /// 5. Logs warnings if type or method resolution fails
    /// 
    /// Performance Notes:
    /// - Type resolution is expensive, but results are cached
    /// - Method resolution is performed once during construction
    /// - Subsequent Invoke() calls have minimal reflection overhead
    /// 
    /// Common Failure Scenarios:
    /// - Assembly not loaded or accessible
    /// - Type name typos or incorrect namespace
    /// - Method name typos or case mismatch
    /// - Method is overloaded (this class finds the first matching name)
    /// </remarks>
    public Method(string typeNameWithAssembly, string methodName, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Static)
    {
        if (!TryGetType(typeNameWithAssembly, out Type type))
        {
            return;
        }

        if (type == null)
        {
            EpicLoot.logger.LogWarning($"Type resolution returned null for: '{typeNameWithAssembly}'");
            return;
        }

        info = type.GetMethod(methodName, bindingFlags);
        if (info == null)
        {
            EpicLoot.logger.LogWarning(
                $"Failed to find public static method '{methodName}' in type '{type.FullName}'. " +
                "Verify the method name is correct, the method exists, and it is marked as public static. ");
        }
    }

    /// <summary>
    /// Helper constructor that automatically adds type name with assembly, defined at the top of the file.
    /// </summary>
    /// <param name="methodName"></param>
    /// <param name="bindingFlags"></param>
    public Method(string methodName, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Static) : this(
        API_LOCATION, methodName, bindingFlags) { }

    /// <param name="typeNameWithAssembly"><see cref="string"/></param>
    /// <param name="type"><see cref="Type"/></param>
    /// <returns></returns>
    private static bool TryGetType(string typeNameWithAssembly, out Type type)
    {
        // Try to get cached type first for performance
        if (CachedTypes.TryGetValue(typeNameWithAssembly, out type))
        {
            return true;
        }

        // Attempt to resolve the type from the assembly
        if (Type.GetType(typeNameWithAssembly) is not { } resolvedType)
        {
            EpicLoot.logger.LogWarning($"Failed to resolve type: '{typeNameWithAssembly}'. " +
                             "Verify the namespace, class name, and assembly name are correct. " +
                             "Ensure the assembly is loaded and accessible.");
            return false;
        }

        type = resolvedType;
        CachedTypes[typeNameWithAssembly] = resolvedType;
        return true;
    }
    
    /// <summary>
    /// Searches for the specified public method whose parameters match the types
    /// </summary>
    /// <param name="typeNameWithAssembly"><see cref="string"/></param>
    /// <param name="methodName"><see cref="string"/></param>
    /// <param name="types">params array of <see cref="Type"/></param>
    public Method(string typeNameWithAssembly, string methodName, params Type[] types)
    {
        if (!TryGetType(typeNameWithAssembly, out Type type))
        {
            return;
        }

        // Additional null check (defensive programming, should not happen if TryGetValue succeeded)
        if (type == null)
        {
            EpicLoot.logger.LogWarning($"Type resolution returned null for: '{typeNameWithAssembly}'");
            return;
        }

        // Locate the static method by name
        info = type.GetMethod(methodName, types);
        if (info == null)
        {
            EpicLoot.logger.LogWarning(
                $"Failed to find public static method '{methodName}' in type '{type.FullName}'. " +
                "Verify the method name is correct, the method exists, and it is marked as public static. ");
        }
    }

    public Method (string methodName, params Type[] types) : this(API_LOCATION, methodName, types) { }

    /// <summary>
    /// Gets the parameter information for the resolved method.
    /// Useful for validating arguments before calling Invoke().
    /// </summary>
    /// <returns>Array of ParameterInfo objects describing the method parameters, or empty array if method not resolved.</returns>
    [PublicAPI]
    public ParameterInfo[] GetParameters() => info?.GetParameters() ?? Array.Empty<ParameterInfo>();

    [PublicAPI]
    public static void ClearCache() => CachedTypes.Clear();
}