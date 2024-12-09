using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using NServiceBus;

using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

class HttpSender
{
    readonly IFunctionEndpoint functionEndpoint;

    public HttpSender(IFunctionEndpoint functionEndpoint)
    {
        this.functionEndpoint = functionEndpoint;
    }

    [FunctionName("InProcessHttpSenderV4")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest request, ExecutionContext executionContext, ILogger logger, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("C# HTTP trigger function received a request at {0}", request.GetEncodedPathAndQuery());

        var sendOptions = new SendOptions();
        sendOptions.RouteToThisEndpoint();

        await functionEndpoint.Send(new TriggerMessage(), sendOptions, executionContext, logger, cancellationToken).ConfigureAwait(false);

        return new OkObjectResult($"{nameof(TriggerMessage)} sent.");
    }
}
