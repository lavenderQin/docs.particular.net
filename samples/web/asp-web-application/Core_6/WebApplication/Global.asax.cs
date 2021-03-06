using System;
using System.Web;
using NServiceBus;

public class Global : HttpApplication
{
    public static IEndpointInstance Endpoint;

    protected void Application_End()
    {
        Endpoint?.Stop().GetAwaiter().GetResult();
    }

    protected void Application_Start(object sender, EventArgs e)
    {
        #region ApplicationStart

        var endpointConfiguration = new EndpointConfiguration("Samples.AsyncPages.WebApplication");
        endpointConfiguration.ScaleOut()
            .InstanceDiscriminator("1");
        endpointConfiguration.UseSerialization<JsonSerializer>();
        endpointConfiguration.EnableInstallers();
        endpointConfiguration.UsePersistence<InMemoryPersistence>();
        endpointConfiguration.SendFailedMessagesTo("error");

        Endpoint = NServiceBus.Endpoint.Start(endpointConfiguration).GetAwaiter().GetResult();

        #endregion
    }
}