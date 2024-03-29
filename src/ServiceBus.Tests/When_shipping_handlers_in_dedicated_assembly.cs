﻿namespace ServiceBus.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Loader;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus;
    using NServiceBus.Configuration.AdvancedExtensibility;
    using NServiceBus.Unicast;
    using NUnit.Framework;

    [TestFixture]
    public class When_shipping_handlers_in_dedicated_assembly
    {
        [Test]
        public async Task Should_load_handlers_from_assembly_when_using_FunctionsHostBuilder()
        {
            // The message handler assembly shouldn't be loaded at this point because there is no reference in the code to it.
            Assert.False(AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName == "Testing.Handlers, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"));

            var serviceCollection = new ServiceCollection();

            var configuration = new ServiceBusTriggeredEndpointConfiguration("assemblyTest", default, null);
            configuration.UseSerialization<XmlSerializer>();

            var endpointConfiguration = configuration.AdvancedConfiguration;
            endpointConfiguration.UsePersistence<LearningPersistence>();
            endpointConfiguration.EnableInstallers();
            var settings = endpointConfiguration.GetSettings();
            var serverless = configuration.MakeServerless();

            var startableEndpoint = FunctionsHostBuilderExtensions.Configure(
                configuration.AdvancedConfiguration,
                serviceCollection,
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ExternalHandlers"));

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var endpoint = new InProcessFunctionEndpoint(startableEndpoint, serverless, serviceProvider);

            // we need to process an actual message to have the endpoint being created
            await endpoint.InitializeEndpointIfNecessary(CancellationToken.None);

            // The message handler assembly should be loaded now because scanning should find and load the handler assembly
            Assert.True(AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName == "Testing.Handlers, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"));

            // Verify the handler and message type have been identified and loaded:
            var registry = settings.Get<MessageHandlerRegistry>();
            var dummyMessageType = registry.GetMessageTypes().FirstOrDefault(t => t.FullName == "Testing.Handlers.DummyMessage");
            Assert.NotNull(dummyMessageType);
            var dummyMessageHandler = registry.GetHandlersFor(dummyMessageType).SingleOrDefault();
            Assert.AreEqual("Testing.Handlers.DummyMessageHandler", dummyMessageHandler.HandlerType.FullName);

            // ensure the assembly is loaded into the right context
            Assert.AreEqual(AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly()), AssemblyLoadContext.GetLoadContext(dummyMessageType.Assembly));
        }
    }
}