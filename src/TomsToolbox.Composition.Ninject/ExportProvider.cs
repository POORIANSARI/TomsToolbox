﻿namespace TomsToolbox.Composition.Ninject;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using global::Ninject;
using global::Ninject.Planning.Bindings;

/// <summary>
/// Implements the <see cref="IExportProvider"/> interface for the Ninject DI container
/// </summary>
/// <seealso cref="IExportProvider" />
public class ExportProvider : IExportProvider
{
    /// <summary>
    /// The default master binding name
    /// </summary>
    internal const string DefaultMasterBindingName = "71751FFE-46C5-465A-9F50-6AEFD1C14232";

    /// <summary>
    /// The key under which the export metadata is stored.
    /// </summary>
    public const string ExportMetadataKey = "ExportMetadata";

    private readonly IKernel _kernel;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportProvider"/> class.
    /// </summary>
    /// <param name="kernel">The kernel.</param>
    public ExportProvider(IKernel kernel)
    {
        _kernel = kernel;
    }

#pragma warning disable CS0067
    /// <inheritdoc />
    public event EventHandler<EventArgs>? ExportsChanged;

    T IExportProvider.GetExportedValue<T>(string? contractName) where T : class
    {
        return (contractName != null ? _kernel.Get<T>(contractName) : _kernel.Get<T>());
    }

    T? IExportProvider.GetExportedValueOrDefault<T>(string? contractName) where T : class
    {
        return GetExportedValues<T>(contractName).SingleOrDefault();
    }

    bool IExportProvider.TryGetExportedValue<T>(string? contractName, [NotNullWhen(true)] out T? value) where T : class
    {
        var values = GetExportedValues<T>(contractName).ToList();

        if (values.Count != 1)
        {
            value = default;
            return false;
        }

        value = values[0];
        return true;
    }

    IEnumerable<T> IExportProvider.GetExportedValues<T>(string? contractName) where T : class
    {
        return GetExportedValues<T>(contractName);
    }

    IEnumerable<object> IExportProvider.GetExportedValues(Type contractType, string? contractName)
    {
        return (contractName != null ? _kernel.GetAll(contractType, contractName) : _kernel.GetAll(contractType))
            .ToList().AsReadOnly();
    }

    IEnumerable<IExport<object>> IExportProvider.GetExports(Type contractType, string? contractName)
    {
        if (contractName == string.Empty)
            contractName = null;

        var bindings = _kernel.GetBindings(contractType)
            .Where(binding => GetEffectiveContractName(binding.Metadata.Name) == contractName);

        var result = bindings.Select(binding => new ExportAdapter<object>(() => GetExportedValue(binding), binding.Metadata.Get<IMetadata>(ExportMetadataKey)));

        return result.ToList().AsReadOnly();
    }

    IEnumerable<IExport<T>> IExportProvider.GetExports<T>(string? contractName) where T : class
    {
        if (contractName == string.Empty)
            contractName = null;

        var bindings = _kernel.GetBindings(typeof(T))
            .Where(binding => GetEffectiveContractName(binding.Metadata.Name) == contractName);

        var result = bindings.Select(binding => new ExportAdapter<T>(() => (T)GetExportedValue(binding), binding.Metadata.Get<IMetadata>(ExportMetadataKey)));

        return result.ToList().AsReadOnly();
    }

    IEnumerable<IExport<T, TMetadataView>> IExportProvider.GetExports<T, TMetadataView>(string? contractName) where T : class where TMetadataView :class
    {
#if NET6_0_OR_GREATER
        if (contractName == string.Empty)
            contractName = null;

        var bindings = _kernel.GetBindings(typeof(T))
            .Where(binding => GetEffectiveContractName(binding.Metadata.Name) == contractName);

        var result = bindings.Select(binding => new ExportAdapter<T, TMetadataView>(() => (T)GetExportedValue(binding), MetadataAdapter.Create<TMetadataView>(binding.Metadata.Get<IMetadata>(ExportMetadataKey))));

        return result.ToList().AsReadOnly();
#else
        throw new NotSupportedException("This method is not supported in this target framework.");
#endif
    }

    private static string? GetEffectiveContractName(string? name)
    {
        return name == DefaultMasterBindingName ? null : name;
    }

    private IEnumerable<T> GetExportedValues<T>(string? contractName)
    {
        return (contractName != null ? _kernel.GetAll<T>(contractName) : _kernel.GetAll<T>()).ToList().AsReadOnly();
    }

    private object GetExportedValue(IBinding binding)
    {
        bool Constraint(IBindingMetadata bindingMetadata)
        {
            return bindingMetadata == binding.Metadata;
        }

        var request = _kernel.CreateRequest(binding.Service, Constraint, binding.Parameters, false, true);

        var result = _kernel.Resolve(request).Single();

        return result;
    }
}
