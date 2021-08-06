namespace ServiceBus.Tests
{
    using System;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.WebJobs;
    using NServiceBus.AzureFunctions.InProcess.ServiceBus;
    using NUnit.Framework;

    [TestFixture]
    public class ReflectionHelperTests
    {
        [Test]
        public void When_no_attributes_defined_should_throw()
        {
            var exception = Assert.Throws<Exception>(() => ReflectionHelper.GetAutoCompleteValue());

            StringAssert.Contains($"Could not locate {nameof(ServiceBusTriggerAttribute)} to infer the AutoComplete setting.", exception.Message);
        }

        [Test]
        public void When_no_function_name_attribute_defined_should_throw()
        {
            var exception = Assert.Throws<Exception>(() => FunctionWithNoFunctionNameAttribute(null));

            StringAssert.Contains($"Could not locate {nameof(ServiceBusTriggerAttribute)} to infer the AutoComplete setting.", exception.Message);

            void FunctionWithNoFunctionNameAttribute(
                [ServiceBusTrigger("queueName", "subscriptionname", AutoComplete = true)] Message _)
            {
                ReflectionHelper.GetAutoCompleteValue();
            }
        }

        [Test]
        public void When_no_trigger_attribute_defined_should_throw()
        {
            var exception = Assert.Throws<Exception>(() => FunctionWithNoServiceBusTriggerAttribute(null));

            StringAssert.Contains($"Could not locate {nameof(ServiceBusTriggerAttribute)} to infer the AutoComplete setting.", exception.Message);

            [FunctionName("TestFunction")]
            void FunctionWithNoServiceBusTriggerAttribute(
                Message _)
            {
                ReflectionHelper.GetAutoCompleteValue();
            }
        }

        [Test]
        public void When_auto_complete_set_to_false_should_return_false()
        {
            FunctionTriggerWithAutoCompleteExplicitlySetToFalse(null);

            [FunctionName("TestFunction")]
            void FunctionTriggerWithAutoCompleteExplicitlySetToFalse(
                [ServiceBusTrigger("queueName", "subscriptionname", AutoComplete = false)] Message _)
            {
                Assert.IsFalse(ReflectionHelper.GetAutoCompleteValue());
            }
        }

        [Test]
        public void When_auto_complete_set_to_true_should_return_true()
        {
            FunctionTriggerWithAutoCompleteExplicitlySetToTrue(null);

            [FunctionName("TestFunction")]
            void FunctionTriggerWithAutoCompleteExplicitlySetToTrue(
                [ServiceBusTrigger("queueName", "subscriptionname", AutoComplete = true)] Message _)
            {
                Assert.IsTrue(ReflectionHelper.GetAutoCompleteValue());
            }
        }

        [Test]
        public void When_auto_complete_not_set_should_return_true()
        {
            FunctionTriggerWithoutAutoCompleteConfiguration(null);

            [FunctionName("TestFunction")]
            void FunctionTriggerWithoutAutoCompleteConfiguration(
                [ServiceBusTrigger("queueName", "subscriptionname")] Message _)
            {
                Assert.True(ReflectionHelper.GetAutoCompleteValue());
            }
        }

        [Test]
        public void When_helper_invoked_in_nested_methods()
        {
            NestedTrigger(null);

            [FunctionName("TestFunction")]
            void NestedTrigger(
                [ServiceBusTrigger("queueName", "subscriptionname", AutoComplete = false)] Message _)
            {
                One();
            }

            void One()
            {
                Two();
            }

            void Two()
            {
                Three();
            }

            void Three()
            {
                Assert.IsFalse(ReflectionHelper.GetAutoCompleteValue());
            }
        }

        [Test]
        public void When_helper_invoked_in_local_function()
        {
            LocalFunction(null);

            [FunctionName("TestFunction")]
            void LocalFunction(
                [ServiceBusTrigger("queueName", "subscriptionname", AutoComplete = false)] Message _)
            {
                LocalFunction();

                void LocalFunction()
                {
                    Assert.IsFalse(ReflectionHelper.GetAutoCompleteValue());
                }
            }
        }

        [Test]
        public void When_helper_invoked_in_expression()
        {
            Expression(null);

            [FunctionName("TestFunction")]
            void Expression(
                [ServiceBusTrigger("queueName", "subscriptionname", AutoComplete = false)] Message _)
            {
                Func<bool> expression = () => ReflectionHelper.GetAutoCompleteValue();
                Assert.IsFalse(expression());
            }
        }
    }
}