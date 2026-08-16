using System.Threading.Channels;
using CoreLens.Application.Abstractions;
using CoreLens.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CoreLens.Infrastructure.Persistence;

public sealed class MetricSampleBuffer : IMetricSampleBuffer
{
    private readonly Channel<MetricSample> _channel = Channel.CreateBounded<MetricSample>(new BoundedChannelOptions(50_000)
    {
        FullMode = BoundedChannelFullMode.DropOldest,
        SingleReader = true,
        SingleWriter = false
    });

    public ChannelReader<MetricSample> Reader => _channel.Reader;

    public ValueTask EnqueueAsync(IReadOnlyList<MetricSample> samples, CancellationToken cancellationToken)
    {
        foreach (var sample in samples)
        {
            _channel.Writer.TryWrite(sample);
        }

        return ValueTask.CompletedTask;
    }
}

public sealed class MetricPersistenceWorker : BackgroundService
{
    private readonly MetricSampleBuffer _buffer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MetricPersistenceWorker> _logger;

    public MetricPersistenceWorker(
        IMetricSampleBuffer buffer,
        IServiceScopeFactory scopeFactory,
        ILogger<MetricPersistenceWorker> logger)
    {
        _buffer = (MetricSampleBuffer)buffer;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batch = new List<MetricSample>(512);
        while (!stoppingToken.IsCancellationRequested)
        {
            batch.Clear();
            try
            {
                var first = await _buffer.Reader.ReadAsync(stoppingToken);
                batch.Add(first);

                var deadline = DateTime.UtcNow.AddSeconds(3);
                while (batch.Count < 500 && DateTime.UtcNow < deadline)
                {
                    if (_buffer.Reader.TryRead(out var next))
                    {
                        batch.Add(next);
                    }
                    else
                    {
                        await Task.Delay(50, stoppingToken);
                    }
                }

                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IMetricSampleRepository>();
                await repo.InsertBatchAsync(batch, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist {Count} metric samples", batch.Count);
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
