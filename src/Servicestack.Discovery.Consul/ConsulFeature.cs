// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
namespace ServiceStack.Discovery.Consul
{
    using System;
    using Funq;

    using ServiceStack;
    using ServiceStack.Web;

    /// <summary>
    /// Enables remote service calls by dynamically looking up remote service url
    /// </summary>
    public class ConsulFeature : IPlugin
    {
        public ConsulFeatureSettings Settings { get; }

        /// <summary>
        /// Enables service discovery using consul to resolve the correct url for a remote RequestDTO
        /// </summary>
        public ConsulFeature(ConsulSettings settings = null)
        {
            Settings = new ConsulFeatureSettings();
            settings?.Invoke(Settings);
        }
        
        private ConsulServiceRegistration Registration { get; set; }

        public void Register(IAppHost appHost)
        {
            // HACK: not great but unsure how to improve
            // throws exception if WebHostUrl isn't set as this is how we get endpoint url:port
            if (appHost.Config?.WebHostUrl == null)
                throw new ApplicationException("appHost.Config.WebHostUrl must be set to use the Consul plugin, this is so consul will know the full external http://url:port for the service");

            // register callbacks
            appHost.AfterInitCallbacks.Add(RegisterService);
            appHost.OnDisposeCallbacks.Add(UnRegisterService);

            // register plugin link
            appHost.GetPlugin<MetadataFeature>()?.AddPluginLink(ConsulUris.LocalAgent.CombineWith("ui"), "Consul Agent WebUI");
        }

        private void RegisterService(IAppHost host)
        {
            ConsulClient.DiscoveryRequestResolver = Settings.GetDiscoveryTypeResolver();
            Registration = ConsulClient.RegisterService(host, Settings.GetServiceChecks(), Settings.GetHealthCheck(), Settings.GetCustomTags(), Settings.IncludeDefaultServiceHealth);

            host.GetContainer()
                .Register<IServiceGatewayFactory>(x => new ConsulServiceGatewayFactory(Settings.GetGateway(), Settings.GetDiscoveryTypeResolver()))
                .ReusedWithin(ReuseScope.None);
        }

        private void UnRegisterService(IAppHost host = null)
        {
            ConsulClient.DeregisterService(Registration);
        }
    }

    public delegate HealthCheck HealthCheckDelegate(IAppHost appHost);

    public delegate IServiceGateway DefaultGatewayDelegate(string baseUri);

    public delegate void ConsulSettings(ConsulFeatureSettings settings);
}