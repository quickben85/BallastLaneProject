using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace TaskManager.IntegrationTests;

internal sealed class FailingExceptionHandler : IExceptionHandler
{
    public ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Internal exception-handler marker.");
}
