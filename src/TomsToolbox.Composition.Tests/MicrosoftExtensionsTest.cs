﻿namespace TomsToolbox.Composition.Tests;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using TomsToolbox.Composition.MicrosoftExtensions;

using Xunit;

using TomsToolbox.Essentials;

public class MicrosoftExtensionsTest
{
    [Fact]
    public void RegistrationBehavior()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection
            .AddSingleton<SomeDependency>()
            .AddSingleton<SomeSingleton>()
            .AddTransient(provider => new Lazy<SomeSingleton, IMetadata>(provider.GetRequiredService<SomeSingleton>, new MetadataAdapter(new Dictionary<string, object?> { ["Test"] = "Value1" })))
            .AddTransient<IMetadata>(provider => provider.GetRequiredService<SomeSingleton>())
            .AddTransient(provider => new Lazy<IMetadata, IMetadata>(provider.GetRequiredService<SomeSingleton>, new MetadataAdapter(new Dictionary<string, object?> { ["Test"] = "Value1" })))
            .AddTransient(provider => new Lazy<IMetadata, IMetadata>(provider.GetRequiredService<SomeSingleton>, new MetadataAdapter(new Dictionary<string, object?> { ["Test"] = "Value2" })))
            ;

        var serviceCollection2 = new ServiceCollection();

        serviceCollection2.AddRange(serviceCollection);

        var serviceProvider = serviceCollection2.BuildServiceProvider();

        var x1 = serviceProvider.GetService<IMetadata>();
        var x2 = serviceProvider.GetService<IMetadata>();

        var all = serviceProvider.GetServices<Lazy<IMetadata, IMetadata>>();
        var specific = all.Single(item => item.Metadata.GetValue("Test") as string == "Value2");
        var t4 = specific.Value;

        var lazy1 = serviceProvider.GetRequiredService<Lazy<SomeSingleton, IMetadata>>();
        var metadata1 = lazy1.Metadata;
        var value1 = metadata1.GetValue("Test");

        var lazy2 = serviceProvider.GetRequiredService<Lazy<IMetadata, IMetadata>>();
        var lazy3 = serviceProvider.GetRequiredService<Lazy<IMetadata, IMetadata>>();
        var metadata2 = lazy2.Metadata;
        var value2 = metadata2.GetValue("Test");

        var t1 = lazy1.Value;
        var t2 = lazy2.Value;
        var t3 = serviceProvider.GetRequiredService<IMetadata>();

        Assert.Equal("Test1", t1.GetValue("Test"));
        Assert.Equal("Test1", t2.GetValue("Test"));
        Assert.Equal("Test1", t3.GetValue("Test"));
        Assert.Equal("Test1", t4.GetValue("Test"));
    }

    [Fact]
    public void GetTypedServiceInfoWithMetadata()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection
            .AddSingleton<SomeDependency>()
            .BindMetadata(typeof(SomeDependency), typeof(SomeDependency).GetDefaultMetadata("Test"))
            .AddSingleton<SomeSingleton>()
            .BindMetadata(typeof(SomeSingleton));

        var info = serviceCollection.GetServiceInfo<SomeDependency>();

        Assert.Single(info);
        Assert.Equal("Test", info[0].Metadata?.GetValue("ContractName"));
    }

    [Fact]
    public void GetTypedServiceInfoWithoutMetadata()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection
            .AddSingleton<SomeDependency>()
            .BindMetadata(typeof(SomeDependency), typeof(SomeDependency).GetDefaultMetadata("Test"))
            .AddSingleton<SomeSingleton>()
            .BindMetadata(typeof(SomeSingleton));

        var info = serviceCollection.GetServiceInfo<SomeSingleton>();

        Assert.Single(info);
        Assert.Null(info[0].Metadata?.GetValue("ContractName"));
    }

    [Fact]
    public void GetAllServiceInfo()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection
            .AddSingleton<SomeDependency>()
            .BindMetadata(typeof(SomeDependency), typeof(SomeDependency).GetDefaultMetadata("Test"))
            .AddSingleton<SomeSingleton>()
            .BindMetadata(typeof(SomeSingleton));

        var info = serviceCollection.GetServiceInfo();

        Assert.Equal(2, info.Count);
        Assert.Equal("Test", info[0].Metadata?.GetValue("ContractName"));
        Assert.Null(info[1].Metadata?.GetValue("ContractName"));
    }

    [Fact]
    public void GetTypedServiceInfoWithMetadataAndContractName()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.BindExports(GetType().Assembly);

        var serviceProvider = serviceCollection.BuildServiceProvider();

        var exportProvider = (IExportProvider)new ExportProviderAdapter(serviceProvider);

        var c1n = exportProvider.GetExportedValue<IInterface1>();
        Assert.Equal("NoContract", c1n.Name);
        var c1c = exportProvider.GetExportedValue<IInterface1>("Contract");
        Assert.Equal("Contract", c1c.Name);
        var c2n = exportProvider.GetExportedValue<IInterface2>();
        Assert.Equal("NoContract", c2n.Name);
        var c2c = exportProvider.GetExportedValue<IInterface2>("Contract");
        Assert.Equal("Contract", c2c.Name);

    }

    private class SomeDependency : INotifyChanged
    {
#pragma warning disable CS0067
        public event EventHandler? Changed;
    }

    private class SomeSingleton : IMetadata
    {
        private static int _instanceCounter;

        private readonly SomeDependency _dependency;
        private readonly int _instanceId;

        public SomeSingleton(SomeDependency dependency)
        {
            _dependency = dependency;
            _instanceId = Interlocked.Increment(ref _instanceCounter);
        }

        public object GetValue(string name)
        {
            return name + _instanceId;
        }

        public bool TryGetValue(string name, [NotNullWhen(true)] out object? value)
        {
            value = name + _instanceId;
            return true;
        }
    }
}
