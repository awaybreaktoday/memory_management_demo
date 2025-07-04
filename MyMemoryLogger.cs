using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class MyMemoryLogger
{
    public static void LogMemoryInfo()
    {
        Console.WriteLine("--- .NET Memory Info (from inside the app) ---");
        
        // Environment.ProcessorCount should reflect CPU limit from cgroups
        Console.WriteLine($"  Environment.ProcessorCount: {Environment.ProcessorCount}");
        
        // Get process memory info
        var process = Process.GetCurrentProcess();
        Console.WriteLine($"  Process Working Set (MB): {process.WorkingSet64 / 1024 / 1024}");
        Console.WriteLine($"  Process Private Memory (MB): {process.PrivateMemorySize64 / 1024 / 1024}");
        Console.WriteLine($"  Process Virtual Memory (MB): {process.VirtualMemorySize64 / 1024 / 1024}");
        
        // Variables for container limit calculation
        long containerLimitMB = 0;
        long gcLimitMB = 0;
        double memoryPressure = 0;
        
        // Attempt to get GC memory info
        try
        {
            // Force a collection for a more up-to-date view of the heap
            GC.Collect();
            var gcInfo = GC.GetGCMemoryInfo();
            
            Console.WriteLine($"  GC Heap Size (MB): {gcInfo.HeapSizeBytes / 1024 / 1024}");
            Console.WriteLine($"  GC High Memory Load Threshold (MB): {gcInfo.HighMemoryLoadThresholdBytes / 1024 / 1024}");
            Console.WriteLine($"  GC Total Available Memory (MB): {gcInfo.TotalAvailableMemoryBytes / 1024 / 1024}");
            Console.WriteLine($"  GC Memory Load: {gcInfo.MemoryLoadBytes / 1024 / 1024}MB");
            
            // Calculate actual container limit from GC threshold
            // GC typically sets threshold at ~90% of available memory
            var thresholdMB = gcInfo.HighMemoryLoadThresholdBytes / 1024 / 1024;
            containerLimitMB = (long)(thresholdMB / 0.9); // Reverse calculate container limit
            
            // Calculate GC limit based on environment variable
            var gcPercentStr = Environment.GetEnvironmentVariable("DOTNET_GCHeapHardLimitPercent");
            if (int.TryParse(gcPercentStr, out int gcPercent))
            {
                gcLimitMB = (long)(containerLimitMB * gcPercent / 100.0);
            }
            else
            {
                gcLimitMB = (long)(containerLimitMB * 0.7); // Default 70%
            }
            
            // Calculate current memory pressure
            var memoryLoadMB = gcInfo.MemoryLoadBytes / 1024 / 1024;
            memoryPressure = (double)memoryLoadMB / thresholdMB * 100;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Could not get detailed GC Memory Info: {ex.Message}");
        }
        
        // Check ALL the possible environment variables
        Console.WriteLine("--- Environment Variables ---");
        Console.WriteLine($"  DOTNET_RUNNING_IN_CONTAINER: {Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") ?? "Not Set"}");
        Console.WriteLine($"  DOTNET_GCHeapHardLimit: {Environment.GetEnvironmentVariable("DOTNET_GCHeapHardLimit") ?? "Not Set"}");
        Console.WriteLine($"  DOTNET_SYSTEM_GC_HEAPLIMIT: {Environment.GetEnvironmentVariable("DOTNET_SYSTEM_GC_HEAPLIMIT") ?? "Not Set"}");
        Console.WriteLine($"  DOTNET_GCHeapHardLimitPercent: {Environment.GetEnvironmentVariable("DOTNET_GCHeapHardLimitPercent") ?? "Not Set"}");
        Console.WriteLine($"  DOTNET_GCServer: {Environment.GetEnvironmentVariable("DOTNET_GCServer") ?? "Not Set"}");
        
        // Show memory pressure information with calculated values
        if (containerLimitMB > 0)
        {
            Console.WriteLine("--- Memory Pressure Analysis ---");
            Console.WriteLine($"  Memory Load: {(long)(memoryPressure * containerLimitMB * 0.9 / 100)}MB / {(long)(containerLimitMB * 0.9)}MB ({memoryPressure:F1}%)");
            
            if (memoryPressure > 90)
                Console.WriteLine("  🚨 CRITICAL: Very high memory pressure!");
            else if (memoryPressure > 80)
                Console.WriteLine("  ⚠️  WARNING: High memory pressure");
            else if (memoryPressure > 60)
                Console.WriteLine("  ⚡ CAUTION: Moderate memory pressure");
            else
                Console.WriteLine("  ✅ OK: Normal memory pressure");
        }
        
        // Show dynamic container analysis
        Console.WriteLine("--- Container Analysis ---");
        if (containerLimitMB > 0)
        {
            Console.WriteLine($"  Detected Container Limit: {containerLimitMB}MB");
            Console.WriteLine($"  Calculated GC Limit: {gcLimitMB}MB");
            Console.WriteLine($"  Expected OutOfMemoryException around: {gcLimitMB}MB");
            
            // Calculate current utilization
            var workingSetMB = process.WorkingSet64 / 1024 / 1024;
            var containerUtilization = (double)workingSetMB / containerLimitMB * 100;
            Console.WriteLine($"  Container Utilization: {workingSetMB}MB / {containerLimitMB}MB ({containerUtilization:F1}%)");
            
            if (containerUtilization > 95)
                Console.WriteLine("  🚨 CRITICAL: Approaching container limit - OOMKill risk!");
            else if (containerUtilization > 85)
                Console.WriteLine("  ⚠️  WARNING: High container utilization");
            else if (containerUtilization > 70)
                Console.WriteLine("  ⚡ CAUTION: Moderate container utilization");
            else
                Console.WriteLine("  ✅ OK: Normal container utilization");
        }
        else
        {
            Console.WriteLine("  Container Limit: Unable to detect (using default estimates)");
            Console.WriteLine($"  Expected behavior: Monitor GC High Memory Load Threshold");
        }
        
        Console.WriteLine($"  Run 'kubectl top pod' to see actual memory usage");
        
        Console.WriteLine("--------------------------------------------");
    }
}
