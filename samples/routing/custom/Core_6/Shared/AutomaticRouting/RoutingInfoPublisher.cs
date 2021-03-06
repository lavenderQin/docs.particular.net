﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Settings;

class RoutingInfoPublisher : FeatureStartupTask
{
    RoutingInfoCommunicator dataBackplane;
    IReadOnlyCollection<Type> hanledMessageTypes;
    IReadOnlyCollection<Type> publishedMessageTypes;
    ReadOnlySettings settings;
    TimeSpan heartbeatPeriod;
    RoutingInfo publication;
    Timer timer;

    public RoutingInfoPublisher(RoutingInfoCommunicator dataBackplane, IReadOnlyCollection<Type> hanledMessageTypes, IReadOnlyCollection<Type> publishedMessageTypes, ReadOnlySettings settings, TimeSpan heartbeatPeriod)
    {
        this.dataBackplane = dataBackplane;
        this.hanledMessageTypes = hanledMessageTypes;
        this.publishedMessageTypes = publishedMessageTypes;
        this.settings = settings;
        this.heartbeatPeriod = heartbeatPeriod;
    }

    protected override Task OnStart(IMessageSession context)
    {
        publication = new RoutingInfo
        {
            EndpointName = settings.EndpointName().ToString(),
            Discriminator = settings.GetOrDefault<string>("EndpointInstanceDiscriminator"),
            InstanceProperties = settings.EndpointInstanceName().Properties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            HandledMessageTypes = hanledMessageTypes.Select(m => m.AssemblyQualifiedName).ToArray(),
            PublishedMessageTypes = publishedMessageTypes.Select(m => m.AssemblyQualifiedName).ToArray(),
            Active = true,
        };

        timer = new Timer(state =>
        {
            Publish().ConfigureAwait(false).GetAwaiter().GetResult();
        }, null, TimeSpan.Zero, heartbeatPeriod);

        return Publish();
    }

    Task Publish()
    {
        publication.Timestamp = DateTime.UtcNow;
        var dataJson = JsonConvert.SerializeObject(publication);
        return dataBackplane.Publish(dataJson);
    }

    protected override Task OnStop(IMessageSession context)
    {
        using (var waitHandle = new ManualResetEvent(false))
        {
            timer.Dispose(waitHandle);
            waitHandle.WaitOne();
        }

        publication.Active = false;
        return Publish();
    }
}
