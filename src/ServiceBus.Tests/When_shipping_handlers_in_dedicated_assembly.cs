namespace ServiceBus.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Loader;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.WebJobs;
    using NServiceBus;
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
            var endpoint = new TestableFunctionEndpoint(context =>
            {
                var configuration = new ServiceBusTriggeredEndpointConfiguration("assemblyTest");
                configuration.UseSerialization<XmlSerializer>();
                configuration.EndpointConfiguration.UsePersistence<LearningPersistence>();

                settings = configuration.AdvancedConfiguration.GetSettings();
                return configuration;

            })
            {
                AssemblyDirectoryResolver = _ => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ExternalHandlers")
            };


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

        Message GenerateMessage()
        {
            var bytes = Encoding.UTF8.GetBytes("<DummyMessage/>");
            var message = new Message(bytes);
            message.UserProperties["NServiceBus.EnclosedMessageTypes"] = "Testing.Handlers.DummyMessage";

            return message;
        }
    }
}