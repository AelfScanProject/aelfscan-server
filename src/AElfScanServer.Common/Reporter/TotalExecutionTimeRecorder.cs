using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using AElf.OpenTelemetry;
using Volo.Abp.DependencyInjection;

namespace AElfScanServer.Common.Reporter;

public class TotalExecutionTimeRecorder: ISingletonDependency, IInterceptor
{
    private readonly Meter _meter;
    private readonly Dictionary<string, Histogram<long>> _histogramMapCache = new Dictionary<string, Histogram<long>>();
    private readonly Histogram<long> _totalHistogram;

    public TotalExecutionTimeRecorder(IInstrumentationProvider instrumentationProvider)
    {
        _meter = instrumentationProvider.Meter;
        _totalHistogram = _meter.CreateHistogram<long>("aelfScanTotal", "ms", "Histogram for total execution time");
    }

    public async Task InterceptAsync(string className, string methodName, Func<Task> invocation)
    {
        Histogram<long> histogram = GetHistogram(className, methodName);
        Stopwatch stopwatch = Stopwatch.StartNew();
        await invocation();
        stopwatch.Stop();
        histogram.Record(stopwatch.ElapsedMilliseconds);
        _totalHistogram.Record(stopwatch.ElapsedMilliseconds);
    }

    private Histogram<long> GetHistogram(string className, string methodName)
    {
        var key = $"{className}.{methodName}.execution.time";

        if (_histogramMapCache.TryGetValue(key, out var rtKeyCache))
        {
            return rtKeyCache;
        }
        
        var histogram = _meter.CreateHistogram<long>(
            name: key,
            description: "Histogram for method execution time",
            unit: "ms"
        );
        _histogramMapCache.TryAdd(key, histogram);
        return histogram;
    }

}