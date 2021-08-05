namespace ServiceBus.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AzureFunctions.InProcess.ServiceBus;
    using NServiceBus.Transport;
    using NUnit.Framework;

    [TestFixture]
    public class FunctionEndpointTests
    {
        [Test]
        public async Task When_processing_successful_should_complete_message()
        {
            var onCompleteCalled = false;
            var message = MessageHelper.GenerateMessage(new TestMessage());
            MessageContext messageContext = null;
            TransportTransaction transportTransaction = null;
            var pipelineInvoker = await CreatePipeline(
                ctx =>
                {
                    messageContext = ctx;
                    return Task.CompletedTask;
                });


            await FunctionEndpoint.ProcessInternal(
                message,
                tx =>
                {
                    onCompleteCalled = true;
                    return Task.CompletedTask;
                }, () => null,
                _ => transportTransaction = new TransportTransaction(),
                pipelineInvoker);

            Assert.IsTrue(onCompleteCalled);
            Assert.AreSame(message.GetMessageId(), messageContext.MessageId);
            Assert.AreSame(message.Body, messageContext.Body);
            CollectionAssert.IsSubsetOf(message.GetHeaders(), messageContext.Headers); // the IncomingMessage has an additional MessageId header
            Assert.AreSame(transportTransaction, messageContext.TransportTransaction);
        }

        [Test]
        public async Task When_processing_fails_should_provide_error_context()
        {
            var message = MessageHelper.GenerateMessage(new TestMessage());
            var pipelineException = new Exception("test exception");
            var transportTransactions = new List<TransportTransaction>();
            ErrorContext errorContext = null;
            var pipelineInvoker = await CreatePipeline(
                _ => throw pipelineException,
                errCtx =>
                {
                    errorContext = errCtx;
                    return Task.FromResult(ErrorHandleResult.Handled);
                });

            await FunctionEndpoint.ProcessInternal(
                message,
                tx => Task.CompletedTask,
                () => null,
                _ =>
                {
                    var tx = new TransportTransaction();
                    transportTransactions.Add(tx);
                    return tx;
                },
                pipelineInvoker);

            Assert.AreSame(pipelineException, errorContext.Exception);
            Assert.AreSame(message.GetMessageId(), errorContext.Message.NativeMessageId);
            Assert.AreSame(message.Body, errorContext.Message.Body);
            CollectionAssert.IsSubsetOf(message.GetHeaders(), errorContext.Message.Headers); // the IncomingMessage has an additional MessageId header
            Assert.AreSame(transportTransactions.Last(), errorContext.TransportTransaction); // verify usage of the correct transport transaction instance
            Assert.AreEqual(2, transportTransactions.Count); // verify that a new transport transaction has been created for the error handling
        }

        [Test]
        public async Task When_error_pipeline_fails_should_throw()
        {
            var onCompleteCalled = false;
            var errorPipelineException = new Exception("error pipeline failure");
            var pipelineInvoker = await CreatePipeline(
                _ => throw new Exception("main pipeline failure"),
                _ => throw errorPipelineException);

            var exception = Assert.ThrowsAsync<Exception>(async () =>
                await FunctionEndpoint.ProcessInternal(
                    MessageHelper.GenerateMessage(new TestMessage()),
                    tx =>
                    {
                        onCompleteCalled = true;
                        return Task.CompletedTask;
                    },
                    () => null,
                    _ => new TransportTransaction(),
                    pipelineInvoker));

            Assert.IsFalse(onCompleteCalled);
            Assert.AreSame(errorPipelineException, exception);
        }

        [Test]
        public async Task When_error_pipeline_handles_error_should_complete_message()
        {
            var onCompleteCalled = false;
            var pipelineInvoker = await CreatePipeline(
                _ => throw new Exception("main pipeline failure"),
                _ => Task.FromResult(ErrorHandleResult.Handled));

            await FunctionEndpoint.ProcessInternal(
                MessageHelper.GenerateMessage(new TestMessage()),
                tx =>
                {
                    onCompleteCalled = true;
                    return Task.CompletedTask;
                },
                () => null,
                _ => new TransportTransaction(),
                pipelineInvoker);

            Assert.IsTrue(onCompleteCalled);
        }

        [Test]
        public async Task When_error_pipeline_requires_retry_should_throw()
        {
            var onCompleteCalled = false;
            var mainPipelineException = new Exception("main pipeline failure");
            var pipelineInvoker = await CreatePipeline(
                _ => throw mainPipelineException,
                _ => Task.FromResult(ErrorHandleResult.RetryRequired));

            var exception = Assert.ThrowsAsync<Exception>(async () =>
                await FunctionEndpoint.ProcessInternal(
                    MessageHelper.GenerateMessage(new TestMessage()),
                    tx =>
                    {
                        onCompleteCalled = true;
                        return Task.CompletedTask;
                    },
                    () => null,
                    _ => new TransportTransaction(),
                    pipelineInvoker));

            Assert.IsFalse(onCompleteCalled);
            Assert.AreSame(mainPipelineException, exception);
        }

        static async Task<PipelineInvoker> CreatePipeline(Func<MessageContext, Task> mainPipeline = null, Func<ErrorContext, Task<ErrorHandleResult>> errorPipeline = null)
        {
            var pipelineInvoker = new PipelineInvoker();
            await (pipelineInvoker as IPushMessages)
                .Init(
                    mainPipeline ?? (_ => Task.CompletedTask),
                    errorPipeline ?? (_ => Task.FromResult(ErrorHandleResult.Handled)),
                    null, null);
            return pipelineInvoker;
        }

        class TestMessage
        {
        }
    }
}