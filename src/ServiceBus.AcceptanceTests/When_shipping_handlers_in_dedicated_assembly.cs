namespace ServiceBus.Tests
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Loader;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.Configuration.AdvancedExtensibility;
    using NServiceBus.Settings;
    using NServiceBus.Unicast;
    using NUnit.Framework;

    [TestFixture]
    public class When_shipping_handlers_in_dedicated_assembly
    {
        [Test]
        public async Task Should_load_handlers_from_assembly()
        {
            // The message handler assembly shouldn't be loaded at this point because there is no reference in the code to it.
            Assert.That(AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName == "Testing.Handlers, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"), Is.False);

            var functionWithHandlersInDedicatedAssembly = new FunctionWithHandlersInDedicatedAssembly();

            await Scenario.Define<ScenarioContext>()
                .WithComponent(functionWithHandlersInDedicatedAssembly)
                .Done(c => c.EndpointsStarted)
                .Run();

            // The message handler assembly should be loaded now because scanning should find and load the handler assembly
            Assert.That(AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName == "Testing.Handlers, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"), Is.True);

            // // Verify the handler and message type have been identified and loaded:
            var registry = functionWithHandlersInDedicatedAssembly.SettingsHolder.Get<MessageHandlerRegistry>();
            var dummyMessageType = registry.GetMessageTypes().FirstOrDefault(t => t.FullName == "Testing.Handlers.DummyMessage");
            Assert.That(dummyMessageType, Is.Not.Null);
            var dummyMessageHandler = registry.GetHandlersFor(dummyMessageType).SingleOrDefault();
            Assert.That(dummyMessageHandler.HandlerType.FullName, Is.EqualTo("Testing.Handlers.DummyMessageHandler"));

            // ensure the assembly is loaded into the right context
            Assert.That(AssemblyLoadContext.GetLoadContext(dummyMessageType.Assembly), Is.EqualTo(AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly())));
        }

        class FunctionWithHandlersInDedicatedAssembly : FunctionEndpointComponent
        {
            public FunctionWithHandlersInDedicatedAssembly()
            {
                TypesScopedByTestClassAssemblyScanningEnabled = false;
                CustomizeConfiguration = configuration =>
                {
                    configuration.AdvancedConfiguration.UsePersistence<LearningPersistence>();

                    SettingsHolder = configuration.AdvancedConfiguration.GetSettings();
                    // This is using a backdoor to set the assembly directory name to the one that contains the handlers
                    SettingsHolder.Set("NServiceBus.AzureFunctions.InProcess.ServiceBus.AssemblyDirectoryName", "ExternalHandlers");
                };
            }

            public SettingsHolder SettingsHolder { get; private set; }
        }
    }
}