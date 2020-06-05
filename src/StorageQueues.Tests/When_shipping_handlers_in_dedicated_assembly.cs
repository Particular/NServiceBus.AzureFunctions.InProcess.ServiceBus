namespace StorageQueues.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Loader;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Newtonsoft.Json;
    using NServiceBus;
    using NServiceBus.Azure.Transports.WindowsAzureStorageQueues;
    using NServiceBus.Configuration.AdvancedExtensibility;
    using NServiceBus.Settings;
    using NServiceBus.Unicast;
    using NUnit.Framework;

    public class When_shipping_handlers_in_dedicated_assembly
    {
        [Test]
        public async Task Should_load_handlers_from_assembly()
        {
            // The message handler assembly shouldn't be loaded at this point because there is no reference in the code to it.
            Assert.False(AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName == "Testing.Handlers, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"));

            SettingsHolder settings = null;
            var endpoint = new TestableFunctionEndpoint(configuration =>
            {
                var endpoint = new StorageQueueTriggeredEndpointConfiguration("assemblyTest");
                endpoint.UseSerialization<XmlSerializer>();
                settings = endpoint.AdvancedConfiguration.GetSettings();
                return endpoint;
            });
            endpoint.AssemblyDirectoryResolver = _ => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ExternalHandlers");

            // we need to process an actual message to have the endpoint being created
            await endpoint.Process(GenerateMessage(), new ExecutionContext());

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

        CloudQueueMessage GenerateMessage()
        {
            var messageWrapper = new MessageWrapper();
            messageWrapper.Body = Encoding.UTF8.GetBytes("<DummyMessage/>");
            messageWrapper.Headers = new Dictionary<string, string> { { "NServiceBus.EnclosedMessageTypes", "Testing.Handlers.DummyMessage" } };

            var message = new CloudQueueMessage(JsonConvert.SerializeObject(messageWrapper));
            return message;
        }
    }
}