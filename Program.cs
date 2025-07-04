using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime;
using System.Threading;
using Prometheus;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public class Program
{
    private static List<byte[]> _memoryHog = new List<byte[]>();
    
    // Configuration from environment variables
    private static int ALLOCATION_SIZE_MB => int.TryParse(Environment.GetEnvironmentVariable("ALLOCATION_SIZE_MB"), out var size) ? size : 10;
    private static int ALLOCATION_INTERVAL_SECONDS => int.TryParse(Environment.GetEnvironmentVariable("ALLOCATION_INTERVAL_SECONDS"), out var interval) ? interval : 2;
    
    // Prometheus Metrics (keeping existing metrics)
    private static readonly Counter _memoryAllocationsTotal = Metrics
        .CreateCounter("dotnet_memory_allocations_total", "Total number of memory allocations");
    
    private static readonly Gauge _currentAllocatedMB = Metrics
        .CreateGauge("dotnet_memory_current_allocated_mb", "Current allocated memory in MB");
    
    private static readonly Gauge _workingSetMB = Metrics
        .CreateGauge("dotnet_process_working_set_mb", "Process working set in MB");
    
    private static readonly Gauge _gcMemoryPressurePercent = Metrics
        .CreateGauge("dotnet_gc_memory_pressure_percent", "Memory pressure as percentage of threshold");
    
    private static readonly Counter _outOfMemoryExceptionsTotal = Metrics
        .CreateCounter("dotnet_out_of_memory_exceptions_total", "Total OutOfMemoryExceptions thrown");
    
    private static readonly Gauge _containerUtilizationPercent = Metrics
        .CreateGauge("dotnet_container_utilization_percent", "Container memory utilization percentage");
    
    private static readonly Gauge _iterationNumber = Metrics
        .CreateGauge("dotnet_app_iteration_number", "Current iteration number");
    
    private static readonly Gauge _containerLimitMB = Metrics
        .CreateGauge("dotnet_container_memory_limit_mb", "Container memory limit in MB");
    
    // Health check metrics
    private static readonly Gauge _healthCheckStatus = Metrics
        .CreateGauge("dotnet_health_check_status", "Health check status (1=healthy, 0.5=degraded, 0=unhealthy)", new[] { "check_name" });
    
    private static readonly Histogram _healthCheckDuration = Metrics
        .CreateHistogram("dotnet_health_check_duration_seconds", "Health check duration", new[] { "check_name" });

    private static long _detectedContainerLimitMB = 0;

    public static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 DotNetMemoryApp with Advanced Health Checks starting...");
        
        DetectContainerLimit();
        
        var builder = WebApplication.CreateBuilder(args);
        
        // Configure health checks
        builder.Services.AddHealthChecks()
            .AddCheck<ApplicationHealthCheck>("application", tags: new[] { "live", "ready" })
            .AddCheck<MemoryHealthCheck>("memory", tags: new[] { "ready" })
            .AddCheck<GCHealthCheck>("gc", tags: new[] { "ready" });
        
        var app = builder.Build();
        
        // Configure advanced health check endpoints
        AdvancedHealthChecks.ConfigureHealthChecks(app);
        
        // Configure standard endpoints
        app.UseRouting();
        app.UseHttpMetrics();
        app.MapMetrics();
        
        // Application status endpoints
        app.MapGet("/", () => new { 
            application = "DotNet Memory App with Advanced Health Checks",
            version = "1.0.8",
            endpoints = new {
                health = "/health",
                healthLive = "/health/live",
                healthReady = "/health/ready",
                healthDetailed = "/health/detailed",
                healthMemory = "/health/memory",
                healthGC = "/health/gc",
                healthContainer = "/health/container",
                healthPerformance = "/health/performance",
                metrics = "/metrics",
                status = "/status"
            }
        });
        
        app.MapGet("/status", () => new {
            timestamp = DateTime.UtcNow,
            iteration = _iterationNumber.Value,
            allocatedMB = _currentAllocatedMB.Value,
            memoryPressure = _gcMemoryPressurePercent.Value,
            containerUtilization = _containerUtilizationPercent.Value,
            healthStatus = GetQuickHealthStatus()
        });
        
        Console.WriteLine("🔍 Advanced health checks available:");
        Console.WriteLine("   /health/live      - Kubernetes liveness probe");
        Console.WriteLine("   /health/ready     - Kubernetes readiness probe");
        Console.WriteLine("   /health           - Overall health status");
        Console.WriteLine("   /health/detailed  - Comprehensive health report");
        Console.WriteLine("   /health/memory    - Memory-specific health");
        Console.WriteLine("   /health/gc        - Garbage collection health");
        Console.WriteLine("   /health/container - Container resource health");
        Console.WriteLine("   /health/performance - Performance metrics");
        
        // Start background tasks
        var appTask = RunApplication();
        var healthMetricsTask = UpdateHealthMetrics();
        var webTask = app.RunAsync("http://0.0.0.0:5000");
        
        await Task.WhenAny(appTask, healthMetricsTask, webTask);
    }
    
    private static void DetectContainerLimit()
    {
        try
        {
            var gcInfo = GC.GetGCMemoryInfo();
            var thresholdMB = gcInfo.HighMemoryLoadThresholdBytes / 1024 / 1024;
            _detectedContainerLimitMB = (long)(thresholdMB / 0.9);
            
            Console.WriteLine($"🔍 Detected container memory limit: {_detectedContainerLimitMB}MB");
            _containerLimitMB.Set(_detectedContainerLimitMB);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Could not detect container limit: {ex.Message}");
            _detectedContainerLimitMB = 1024;
            _containerLimitMB.Set(_detectedContainerLimitMB);
        }
    }
    
    private static async Task UpdateHealthMetrics()
    {
        while (true)
        {
            try
            {
                // Update health check metrics for Prometheus
                var stopwatch = Stopwatch.StartNew();
                
                // Memory health
                var memoryHealth = await PerformMemoryHealthAssessment();
                stopwatch.Stop();
                _healthCheckDuration.WithLabels("memory").Observe(stopwatch.Elapsed.TotalSeconds);
                _healthCheckStatus.WithLabels("memory").Set(GetHealthStatusValue(memoryHealth));
                
                // GC health
                stopwatch.Restart();
                var gcHealth = await PerformGCHealthAssessment();
                stopwatch.Stop();
                _healthCheckDuration.WithLabels("gc").Observe(stopwatch.Elapsed.TotalSeconds);
                _healthCheckStatus.WithLabels("gc").Set(GetHealthStatusValue(gcHealth));
                
                // Container health
                stopwatch.Restart();
                var containerHealth = await PerformContainerHealthAssessment();
                stopwatch.Stop();
                _healthCheckDuration.WithLabels("container").Observe(stopwatch.Elapsed.TotalSeconds);
                _healthCheckStatus.WithLabels("container").Set(GetHealthStatusValue(containerHealth));
                
                await Task.Delay(TimeSpan.FromSeconds(10)); // Update every 10 seconds
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating health metrics: {ex.Message}");
                await Task.Delay(TimeSpan.FromSeconds(30));
            }
        }
    }
    
    private static double GetHealthStatusValue(string status)
    {
        return status switch
        {
            "Healthy" => 1.0,
            "Degraded" => 0.5,
            "Unhealthy" => 0.0,
            _ => 0.0
        };
    }
    
    private static async Task<string> PerformMemoryHealthAssessment()
    {
        await Task.Delay(1);
        var gcInfo = GC.GetGCMemoryInfo();
        var memoryPressure = (double)gcInfo.MemoryLoadBytes / gcInfo.HighMemoryLoadThresholdBytes * 100;
        
        if (memoryPressure > 90) return "Unhealthy";
        if (memoryPressure > 80) return "Degraded";
        return "Healthy";
    }
    
    private static async Task<string> PerformGCHealthAssessment()
    {
        await Task.Delay(1);
        var gen2Collections = GC.CollectionCount(2);
        var process = Process.GetCurrentProcess();
        var uptime = DateTime.UtcNow - process.StartTime;
        var gen2PerHour = gen2Collections / Math.Max(1, uptime.TotalHours);
        
        if (gen2PerHour > 100) return "Degraded";
        return "Healthy";
    }
    
    private static async Task<string> PerformContainerHealthAssessment()
    {
        await Task.Delay(1);
        var process = Process.GetCurrentProcess();
        var workingSetMB = process.WorkingSet64 / 1024 / 1024;
        var utilizationPercent = (double)workingSetMB / _detectedContainerLimitMB * 100;
        
        if (utilizationPercent > 95) return "Unhealthy";
        if (utilizationPercent > 85) return "Degraded";
        return "Healthy";
    }
    
    private static object GetQuickHealthStatus()
    {
        var gcInfo = GC.GetGCMemoryInfo();
        var process = Process.GetCurrentProcess();
        var memoryPressure = (double)gcInfo.MemoryLoadBytes / gcInfo.HighMemoryLoadThresholdBytes * 100;
        var workingSetMB = process.WorkingSet64 / 1024 / 1024;
        var containerUtilization = (double)workingSetMB / _detectedContainerLimitMB * 100;
        
        var overallStatus = "Healthy";
        if (memoryPressure > 90 || containerUtilization > 95) overallStatus = "Unhealthy";
        else if (memoryPressure > 80 || containerUtilization > 85) overallStatus = "Degraded";
        
        return new
        {
            overall = overallStatus,
            memoryPressure = Math.Round(memoryPressure, 1),
            containerUtilization = Math.Round(containerUtilization, 1),
            uptime = (DateTime.UtcNow - process.StartTime).ToString(@"dd\.hh\:mm\:ss")
        };
    }
    
    private static async Task RunApplication()
    {
        Console.WriteLine("🚀 Application loop starting with health monitoring...");
        
        int iteration = 0;
        while (true)
        {
            iteration++;
            _iterationNumber.Set(iteration);
            
            Console.WriteLine($"\n--- Iteration {iteration} ---");
            
            try
            {
                Console.WriteLine($"  Allocating {ALLOCATION_SIZE_MB}MB and writing to it...");
                
                // Allocate and write to memory to force physical memory commitment
                var buffer = new byte[ALLOCATION_SIZE_MB * 1024 * 1024];
                
                // Write to every 4KB page to ensure OS commits physical memory
                for (int i = 0; i < buffer.Length; i += 4096)
                {
                    buffer[i] = (byte)(i % 256);
                }
                
                _memoryHog.Add(buffer);
                
                _memoryAllocationsTotal.Inc();
                _currentAllocatedMB.Set(_memoryHog.Count * ALLOCATION_SIZE_MB);
                
                // Update metrics
                UpdateMetrics();
                
                Console.WriteLine($"  Successfully allocated {ALLOCATION_SIZE_MB}MB");
                Console.WriteLine($"  Total allocated: {_memoryHog.Count * ALLOCATION_SIZE_MB}MB");
                Console.WriteLine($"  Health: {GetQuickHealthStatus()}");
            }
            catch (OutOfMemoryException)
            {
                Console.WriteLine($"  🚨 OutOfMemoryException - clearing memory");
                _outOfMemoryExceptionsTotal.Inc();
                _memoryHog.Clear();
                _currentAllocatedMB.Set(0);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            
            MyMemoryLogger.LogMemoryInfo();
            
            await Task.Delay(TimeSpan.FromSeconds(ALLOCATION_INTERVAL_SECONDS));
        }
    }
    
    private static void UpdateMetrics()
    {
        var process = Process.GetCurrentProcess();
        var gcInfo = GC.GetGCMemoryInfo();
        
        var workingSetMB = process.WorkingSet64 / 1024 / 1024;
        var memoryPressure = (double)gcInfo.MemoryLoadBytes / gcInfo.HighMemoryLoadThresholdBytes * 100;
        var containerUtilization = (double)workingSetMB / _detectedContainerLimitMB * 100;
        
        _workingSetMB.Set(workingSetMB);
        _gcMemoryPressurePercent.Set(memoryPressure);
        _containerUtilizationPercent.Set(containerUtilization);
    }
}
