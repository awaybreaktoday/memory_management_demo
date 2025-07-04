using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

public class AdvancedHealthChecks
{
    public static void ConfigureHealthChecks(WebApplication app)
    {
        // Kubernetes health check endpoints
        app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = WriteHealthCheckResponse
        });

        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteHealthCheckResponse
        });

        app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            ResponseWriter = WriteDetailedHealthCheckResponse
        });

        // Custom detailed health endpoints
        app.MapGet("/health/detailed", GetDetailedHealth);
        app.MapGet("/health/memory", GetMemoryHealth);
        app.MapGet("/health/gc", GetGCHealth);
        app.MapGet("/health/container", GetContainerHealth);
        app.MapGet("/health/performance", GetPerformanceHealth);
    }

    private static async Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
    {
        var result = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            duration = report.TotalDuration.TotalMilliseconds
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = report.Status == HealthStatus.Healthy ? 200 : 503;
        
        await context.Response.WriteAsync(JsonSerializer.Serialize(result, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        }));
    }

    private static async Task WriteDetailedHealthCheckResponse(HttpContext context, HealthReport report)
    {
        var result = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            duration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds,
                description = e.Value.Description,
                data = e.Value.Data,
                exception = e.Value.Exception?.Message
            })
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = report.Status == HealthStatus.Healthy ? 200 : 503;
        
        await context.Response.WriteAsync(JsonSerializer.Serialize(result, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        }));
    }

    private static async Task<IResult> GetDetailedHealth()
    {
        var health = await PerformDetailedHealthCheck();
        var statusCode = GetStatusCode(health.Status);
        
        return Results.Json(health, statusCode: statusCode);
    }

    private static async Task<IResult> GetMemoryHealth()
    {
        var memoryHealth = await PerformMemoryHealthCheck();
        var statusCode = GetStatusCode(memoryHealth.Status);
        
        return Results.Json(memoryHealth, statusCode: statusCode);
    }

    private static async Task<IResult> GetGCHealth()
    {
        var gcHealth = await PerformGCHealthCheck();
        var statusCode = GetStatusCode(gcHealth.Status);
        
        return Results.Json(gcHealth, statusCode: statusCode);
    }

    private static async Task<IResult> GetContainerHealth()
    {
        var containerHealth = await PerformContainerHealthCheck();
        var statusCode = GetStatusCode(containerHealth.Status);
        
        return Results.Json(containerHealth, statusCode: statusCode);
    }

    private static async Task<IResult> GetPerformanceHealth()
    {
        var perfHealth = await PerformPerformanceHealthCheck();
        var statusCode = GetStatusCode(perfHealth.Status);
        
        return Results.Json(perfHealth, statusCode: statusCode);
    }

    private static int GetStatusCode(string status)
    {
        return status switch
        {
            "Healthy" => 200,
            "Degraded" => 200,
            "Unhealthy" => 503,
            _ => 503
        };
    }

    private static async Task<HealthCheckResult> PerformDetailedHealthCheck()
    {
        await Task.Delay(1); // Make it properly async
        
        var process = Process.GetCurrentProcess();
        var gcInfo = GC.GetGCMemoryInfo();
        var uptime = DateTime.UtcNow - process.StartTime.ToUniversalTime();

        // Memory health assessment
        var workingSetMB = process.WorkingSet64 / 1024 / 1024;
        var memoryPressure = (double)(gcInfo.MemoryLoadBytes) / gcInfo.HighMemoryLoadThresholdBytes * 100;
        var containerLimitMB = (gcInfo.HighMemoryLoadThresholdBytes / 1024 / 1024) / 0.9;

        // Determine overall health status
        var status = "Healthy";
        var issues = new List<string>();

        if (memoryPressure > 90)
        {
            status = "Unhealthy";
            issues.Add("Critical memory pressure");
        }
        else if (memoryPressure > 80)
        {
            status = "Degraded";
            issues.Add("High memory pressure");
        }

        if (workingSetMB > containerLimitMB * 0.95)
        {
            status = "Unhealthy";
            issues.Add("Approaching container memory limit");
        }

        var memoryCheck = await PerformMemoryHealthCheck();
        var gcCheck = await PerformGCHealthCheck();
        var containerCheck = await PerformContainerHealthCheck();
        var performanceCheck = await PerformPerformanceHealthCheck();

        var checks = new Dictionary<string, object>
        {
            ["application"] = new
            {
                status = "Healthy",
                uptime = uptime.ToString(@"dd\.hh\:mm\:ss"),
                processId = process.Id,
                version = Environment.Version.ToString()
            },
            ["memory"] = memoryCheck,
            ["gc"] = gcCheck,
            ["container"] = containerCheck,
            ["performance"] = performanceCheck
        };

        return new HealthCheckResult
        {
            Status = status,
            Timestamp = DateTime.UtcNow,
            Application = "DotNet Memory App",
            Version = "1.0.8",
            Environment = System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production",
            Issues = issues,
            Checks = checks,
            Metrics = new
            {
                UptimeSeconds = uptime.TotalSeconds,
                WorkingSetMB = workingSetMB,
                MemoryPressurePercent = Math.Round(memoryPressure, 2),
                GCGeneration0Collections = GC.CollectionCount(0),
                GCGeneration1Collections = GC.CollectionCount(1),
                GCGeneration2Collections = GC.CollectionCount(2)
            }
        };
    }

    private static async Task<MemoryHealthResult> PerformMemoryHealthCheck()
    {
        await Task.Delay(1); // Make it properly async
        
        var process = Process.GetCurrentProcess();
        var gcInfo = GC.GetGCMemoryInfo();
        
        var workingSetMB = process.WorkingSet64 / 1024 / 1024;
        var privateMemoryMB = process.PrivateMemorySize64 / 1024 / 1024;
        var virtualMemoryMB = process.VirtualMemorySize64 / 1024 / 1024;
        var gcHeapMB = gcInfo.HeapSizeBytes / 1024 / 1024;
        var memoryLoadMB = gcInfo.MemoryLoadBytes / 1024 / 1024;
        var thresholdMB = gcInfo.HighMemoryLoadThresholdBytes / 1024 / 1024;
        var memoryPressure = (double)memoryLoadMB / thresholdMB * 100;

        var status = "Healthy";
        var warnings = new List<string>();

        if (memoryPressure > 90)
        {
            status = "Unhealthy";
            warnings.Add("Critical memory pressure - GC struggling");
        }
        else if (memoryPressure > 85)
        {
            status = "Degraded";
            warnings.Add("High memory pressure - frequent GC expected");
        }
        else if (memoryPressure > 70)
        {
            warnings.Add("Moderate memory pressure - monitoring recommended");
        }

        // Check for memory leaks (simplified)
        var memoryGrowthRate = workingSetMB / Math.Max(1, (DateTime.UtcNow - process.StartTime).TotalHours);
        if (memoryGrowthRate > 50) // More than 50MB/hour growth
        {
            warnings.Add($"Potential memory leak detected - growth rate: {memoryGrowthRate:F1} MB/hour");
        }

        return new MemoryHealthResult
        {
            Status = status,
            Warnings = warnings,
            Metrics = new
            {
                WorkingSetMB = workingSetMB,
                PrivateMemoryMB = privateMemoryMB,
                VirtualMemoryMB = virtualMemoryMB,
                GCHeapMB = gcHeapMB,
                MemoryLoadMB = memoryLoadMB,
                MemoryThresholdMB = thresholdMB,
                MemoryPressurePercent = Math.Round(memoryPressure, 2),
                MemoryGrowthRateMBPerHour = Math.Round(memoryGrowthRate, 2)
            },
            Thresholds = new
            {
                MemoryPressureWarning = 70,
                MemoryPressureDegraded = 85,
                MemoryPressureCritical = 90
            }
        };
    }

    private static async Task<GCHealthResult> PerformGCHealthCheck()
    {
        await Task.Delay(1);
        
        var gen0 = GC.CollectionCount(0);
        var gen1 = GC.CollectionCount(1);
        var gen2 = GC.CollectionCount(2);
        
        var gcInfo = GC.GetGCMemoryInfo();
        var totalMemoryMB = GC.GetTotalMemory(false) / 1024 / 1024;
        
        var status = "Healthy";
        var issues = new List<string>();

        // Calculate GC pressure based on generation ratios
        var gen2Ratio = gen2 == 0 ? 0 : (double)gen2 / Math.Max(1, gen0);
        if (gen2Ratio > 0.1) // More than 10% Gen2 collections
        {
            status = "Degraded";
            issues.Add("High Gen2 collection ratio indicates memory pressure");
        }

        // Check for excessive GC frequency (simplified - would need historical data)
        var process = Process.GetCurrentProcess();
        var uptime = DateTime.UtcNow - process.StartTime;
        var gen2PerHour = gen2 / Math.Max(1, uptime.TotalHours);
        
        if (gen2PerHour > 100)
        {
            status = "Degraded";
            issues.Add($"High Gen2 collection frequency: {gen2PerHour:F1} per hour");
        }

        return new GCHealthResult
        {
            Status = status,
            Issues = issues,
            Collections = new
            {
                Generation0 = gen0,
                Generation1 = gen1,
                Generation2 = gen2,
                Gen2Ratio = Math.Round(gen2Ratio * 100, 2)
            },
            Memory = new
            {
                TotalManagedMB = totalMemoryMB,
                HeapSizeMB = gcInfo.HeapSizeBytes / 1024 / 1024,
                FragmentedMB = gcInfo.FragmentedBytes / 1024 / 1024,
                MemoryLoadMB = gcInfo.MemoryLoadBytes / 1024 / 1024
            },
            Performance = new
            {
                Gen2CollectionsPerHour = Math.Round(gen2PerHour, 1),
                IsServerGC = GCSettings.IsServerGC,
                LatencyMode = GCSettings.LatencyMode.ToString()
            }
        };
    }

    private static async Task<ContainerHealthResult> PerformContainerHealthCheck()
    {
        await Task.Delay(1);
        
        var process = Process.GetCurrentProcess();
        var gcInfo = GC.GetGCMemoryInfo();
        
        var workingSetMB = process.WorkingSet64 / 1024 / 1024;
        var estimatedLimitMB = (gcInfo.HighMemoryLoadThresholdBytes / 1024 / 1024) / 0.9;
        var utilizationPercent = (workingSetMB / estimatedLimitMB) * 100;
        
        var status = "Healthy";
        var alerts = new List<string>();

        if (utilizationPercent > 95)
        {
            status = "Unhealthy";
            alerts.Add("Critical: Approaching container memory limit - OOMKill risk");
        }
        else if (utilizationPercent > 85)
        {
            status = "Degraded";
            alerts.Add("Warning: High container memory utilization");
        }

        // Check environment variables for container awareness
        var isContainerAware = System.Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
        var gcHeapLimit = System.Environment.GetEnvironmentVariable("DOTNET_GCHeapHardLimitPercent");

        if (!isContainerAware)
        {
            alerts.Add("Application may not be container-aware");
        }

        return new ContainerHealthResult
        {
            Status = status,
            Alerts = alerts,
            Container = new
            {
                EstimatedLimitMB = Math.Round(estimatedLimitMB),
                CurrentUsageMB = workingSetMB,
                UtilizationPercent = Math.Round(utilizationPercent, 2),
                IsContainerAware = isContainerAware,
                GCHeapLimitPercent = gcHeapLimit ?? "Not Set"
            },
            Environment = new
            {
                ProcessorCount = System.Environment.ProcessorCount,
                OSVersion = System.Environment.OSVersion.ToString(),
                RuntimeVersion = System.Environment.Version.ToString(),
                WorkingDirectory = System.Environment.CurrentDirectory
            },
            Thresholds = new
            {
                UtilizationWarning = 85,
                UtilizationCritical = 95
            }
        };
    }

    private static async Task<PerformanceHealthResult> PerformPerformanceHealthCheck()
    {
        // Performance test: measure response time for basic operations
        var stopwatch = Stopwatch.StartNew();
        
        // Simulate some work
        await Task.Delay(1);
        var operationLatency = stopwatch.ElapsedMilliseconds;
        
        // Memory allocation test
        stopwatch.Restart();
        var testArray = new byte[1024 * 1024]; // 1MB allocation
        var allocationLatency = stopwatch.ElapsedMilliseconds;
        
        // GC test
        stopwatch.Restart();
        GC.Collect(0, GCCollectionMode.Optimized);
        var gcLatency = stopwatch.ElapsedMilliseconds;
        
        var status = "Healthy";
        var issues = new List<string>();

        if (operationLatency > 100)
        {
            status = "Degraded";
            issues.Add($"High operation latency: {operationLatency}ms");
        }

        if (allocationLatency > 50)
        {
            issues.Add($"Slow memory allocation: {allocationLatency}ms");
        }

        if (gcLatency > 100)
        {
            issues.Add($"Slow GC collection: {gcLatency}ms");
        }

        var process = Process.GetCurrentProcess();
        
        return new PerformanceHealthResult
        {
            Status = status,
            Issues = issues,
            Latency = new
            {
                OperationMs = operationLatency,
                AllocationMs = allocationLatency,
                GCCollectionMs = gcLatency
            },
            Process = new
            {
                CpuUsagePercent = GetCpuUsage(),
                ThreadCount = process.Threads.Count,
                HandleCount = process.HandleCount,
                UptimeSeconds = (DateTime.UtcNow - process.StartTime).TotalSeconds
            },
            Thresholds = new
            {
                OperationLatencyWarning = 50,
                OperationLatencyCritical = 100,
                AllocationLatencyWarning = 25,
                GCLatencyWarning = 50
            }
        };
    }

    private static double GetCpuUsage()
    {
        // Simplified CPU usage calculation
        var process = Process.GetCurrentProcess();
        return Math.Round(process.TotalProcessorTime.TotalMilliseconds / Environment.TickCount * 100, 2);
    }
}

// Result classes for proper typing
public class HealthCheckResult
{
    public string Status { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string Application { get; set; } = "";
    public string Version { get; set; } = "";
    public string Environment { get; set; } = "";
    public List<string> Issues { get; set; } = new();
    public Dictionary<string, object> Checks { get; set; } = new();
    public object Metrics { get; set; } = new();
}

public class MemoryHealthResult
{
    public string Status { get; set; } = "";
    public List<string> Warnings { get; set; } = new();
    public object Metrics { get; set; } = new();
    public object Thresholds { get; set; } = new();
}

public class GCHealthResult
{
    public string Status { get; set; } = "";
    public List<string> Issues { get; set; } = new();
    public object Collections { get; set; } = new();
    public object Memory { get; set; } = new();
    public object Performance { get; set; } = new();
}

public class ContainerHealthResult
{
    public string Status { get; set; } = "";
    public List<string> Alerts { get; set; } = new();
    public object Container { get; set; } = new();
    public object Environment { get; set; } = new();
    public object Thresholds { get; set; } = new();
}

public class PerformanceHealthResult
{
    public string Status { get; set; } = "";
    public List<string> Issues { get; set; } = new();
    public object Latency { get; set; } = new();
    public object Process { get; set; } = new();
    public object Thresholds { get; set; } = new();
}

// Health check classes for dependency injection
public class MemoryHealthCheck : IHealthCheck
{
    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var gcInfo = GC.GetGCMemoryInfo();
        var memoryPressure = (double)gcInfo.MemoryLoadBytes / gcInfo.HighMemoryLoadThresholdBytes * 100;

        if (memoryPressure > 90)
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy($"Critical memory pressure: {memoryPressure:F1}%");
        
        if (memoryPressure > 80)
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded($"High memory pressure: {memoryPressure:F1}%");
        
        return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy($"Memory pressure: {memoryPressure:F1}%");
    }
}

public class GCHealthCheck : IHealthCheck
{
    private static int _lastGen2Count = 0;
    private static DateTime _lastCheck = DateTime.UtcNow;

    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var currentGen2 = GC.CollectionCount(2);
        var now = DateTime.UtcNow;
        var timeDiff = now - _lastCheck;
        var gen2Diff = currentGen2 - _lastGen2Count;

        if (timeDiff.TotalMinutes > 0)
        {
            var gen2PerMinute = gen2Diff / timeDiff.TotalMinutes;
            
            _lastGen2Count = currentGen2;
            _lastCheck = now;

            if (gen2PerMinute > 10)
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded($"High Gen2 GC frequency: {gen2PerMinute:F1}/min");
        }

        return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy($"GC healthy - Gen2 collections: {currentGen2}");
    }
}

public class ApplicationHealthCheck : IHealthCheck
{
    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // Check if application is responsive
        try
        {
            await Task.Delay(1, cancellationToken);
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Application responsive");
        }
        catch (OperationCanceledException)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Application not responding");
        }
    }
}
