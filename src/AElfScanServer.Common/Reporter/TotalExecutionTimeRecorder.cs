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
    private  readonly Counter<long> _apiTimeoutCounter;

    public TotalExecutionTimeRecorder(IInstrumentationProvider instrumentationProvider)
    {
        _meter = instrumentationProvider.Meter;
        _totalHistogram = _meter.CreateHistogram<long>("aelfScanTotal", "ms", "Histogram for total execution time");
        _apiTimeoutCounter = _meter.CreateCounter<long>(
        "aelfScanApiTimeoutCount", 
        "counts", 
        "The number of API timeouts"
    );
    }

    public async Task InterceptAsync(string className, string methodName, Func<Task> invocation)
    {
        Histogram<long> histogram = GetHistogram(className, methodName);
        Stopwatch stopwatch = Stopwatch.StartNew();
        await invocation();
        stopwatch.Stop();
        var stopwatchElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
        histogram.Record(stopwatchElapsedMilliseconds);
        if (methodName.Contains("OnActionExecutionAsync"))
        {
            _totalHistogram.Record(stopwatchElapsedMilliseconds);
        }

        if (stopwatchElapsedMilliseconds > 1000)
        {
            _apiTimeoutCounter.Add(1,new KeyValuePair<string, object>("methodName",className + methodName));
        }
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