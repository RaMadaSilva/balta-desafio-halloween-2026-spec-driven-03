using System.Threading.RateLimiting;
using System.Globalization;
using System.Net;

namespace UrlShortener.Api.Shared;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddCreationRateLimiting(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddRateLimiter(options =>
        {
            options.AddPolicy("create-links", context => RateLimitPartition.Get(
                context.Connection.RemoteIpAddress?.MapToIPv6() ?? IPAddress.IPv6None,
                _ => new ExactWindowLimiter(context.RequestServices.GetRequiredService<TimeProvider>())));
            options.OnRejected = async (rejected, _) =>
            {
                rejected.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry);
                rejected.HttpContext.Response.Headers.RetryAfter = Math.Max(1, Math.Ceiling(retry.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                await Results.Problem(statusCode: 429, title: "Limite de criação excedido.", detail: "Aguarde antes de voltar a criar um link.").ExecuteAsync(rejected.HttpContext);
            };
        });
        return services;
    }

    internal sealed class ExactWindowLimiter(TimeProvider clock) : RateLimiter
    {
        private readonly object gate = new();
        private readonly Queue<long> admitted = new();
        private long lastAttempt = clock.GetTimestamp();
        private long succeeded;
        private long failed;

        public override TimeSpan? IdleDuration
        {
            get
            {
                lock (gate)
                {
                    Prune(clock.GetTimestamp());
                    return admitted.Count == 0 ? clock.GetElapsedTime(lastAttempt) : null;
                }
            }
        }

        public override RateLimiterStatistics GetStatistics()
        {
            lock (gate)
            {
                Prune(clock.GetTimestamp());
                return new RateLimiterStatistics
                {
                    CurrentAvailablePermits = 10 - admitted.Count,
                    CurrentQueuedCount = 0,
                    TotalSuccessfulLeases = succeeded,
                    TotalFailedLeases = failed
                };
            }
        }

        protected override RateLimitLease AttemptAcquireCore(int permitCount)
        {
            lock (gate)
            {
                var now = clock.GetTimestamp();
                lastAttempt = now;
                Prune(now);
                if (permitCount > 10 - admitted.Count)
                {
                    failed++;
                    return new Lease(false, admitted.Count == 0 ? TimeSpan.FromSeconds(60) : TimeSpan.FromSeconds(60) - clock.GetElapsedTime(admitted.Peek(), now));
                }

                for (var i = 0; i < permitCount; i++)
                    admitted.Enqueue(now);
                succeeded++;
                return new Lease(true, TimeSpan.Zero);
            }
        }

        protected override ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken) =>
            ValueTask.FromResult(AttemptAcquireCore(permitCount));

        private void Prune(long now)
        {
            while (admitted.TryPeek(out var oldest) && clock.GetElapsedTime(oldest, now) >= TimeSpan.FromSeconds(60))
                admitted.Dequeue();
        }

        private sealed class Lease(bool acquired, TimeSpan retry) : RateLimitLease
        {
            public override bool IsAcquired => acquired;
            public override IEnumerable<string> MetadataNames => acquired ? [] : [MetadataName.RetryAfter.Name];
            public override bool TryGetMetadata(string metadataName, out object? metadata)
            {
                metadata = !acquired && metadataName == MetadataName.RetryAfter.Name ? retry : null;
                return metadata is not null;
            }
        }
    }
}
