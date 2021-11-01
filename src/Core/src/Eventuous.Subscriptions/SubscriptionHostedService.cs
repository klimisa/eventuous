using Eventuous.Subscriptions.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Eventuous.Subscriptions;

public class SubscriptionHostedService : IHostedService {
    readonly CancellationTokenSource _subscriptionCts = new();
    readonly IMessageSubscription    _subscription;
    readonly ISubscriptionHealth?    _subscriptionHealth;

    public SubscriptionHostedService(
        IMessageSubscription subscription,
        ISubscriptionHealth? subscriptionHealth = null,
        ILoggerFactory?      loggerFactory      = null
    ) {
        _subscription       = subscription;
        _subscriptionHealth = subscriptionHealth;

        Log = loggerFactory?.CreateLogger<SubscriptionHostedService>();
    }

    ILogger<SubscriptionHostedService>? Log { get; }

    public virtual async Task StartAsync(CancellationToken cancellationToken) {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _subscriptionCts.Token);

        // if (Measure != null) {
        //     var monitoringCts =
        //         CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _monitoringCts.Token);
        //
        //     _measureTask = Task.Run(() => MeasureGap(monitoringCts.Token), monitoringCts.Token);
        // }

        await _subscription.Subscribe(
            id => _subscriptionHealth?.ReportHealthy(id),
            (id, _, ex) => _subscriptionHealth?.ReportUnhealthy(id, ex),
            cts.Token
        ).NoContext();

        Log?.LogInformation("Started subscription");
    }

    public virtual async Task StopAsync(CancellationToken cancellationToken) {
        // if (_measureTask != null) {
        //     _monitoringCts.Cancel();
        //
        //     try {
        //         await _measureTask.NoContext();
        //     }
        //     catch (OperationCanceledException) {
        //         // Expected
        //     }
        // }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _subscriptionCts.Token);
        await _subscription.Unsubscribe(_ => { }, cts.Token).NoContext();

        Log?.LogInformation("Stopped subscription");
    }
}