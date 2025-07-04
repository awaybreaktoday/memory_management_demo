#!/bin/bash

# Health Check Real-Time Monitoring Dashboard
# Displays advanced health check information in a live dashboard

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Configuration
HEALTH_URL=${HEALTH_URL:-"http://localhost:5000"}
REFRESH_INTERVAL=${REFRESH_INTERVAL:-5}

# Check if jq is available
if ! command -v jq &> /dev/null; then
    echo -e "${RED}❌ jq is required but not installed. Please install jq first.${NC}"
    exit 1
fi

# Check if curl is available  
if ! command -v curl &> /dev/null; then
    echo -e "${RED}❌ curl is required but not installed. Please install curl first.${NC}"
    exit 1
fi

# Function to get health status color
get_status_color() {
    local status="$1"
    case "$status" in
        "Healthy") echo "$GREEN" ;;
        "Degraded") echo "$YELLOW" ;;
        "Unhealthy") echo "$RED" ;;
        *) echo "$NC" ;;
    esac
}

# Function to format percentage with color
format_percentage() {
    local value="$1"
    local warning_threshold="$2"
    local critical_threshold="$3"
    
    if (( $(echo "$value > $critical_threshold" | bc -l) )); then
        echo -e "${RED}${value}%${NC}"
    elif (( $(echo "$value > $warning_threshold" | bc -l) )); then
        echo -e "${YELLOW}${value}%${NC}"
    else
        echo -e "${GREEN}${value}%${NC}"
    fi
}

# Function to test endpoint availability
test_endpoint() {
    local endpoint="$1"
    local timeout=3
    
    if curl -s --max-time $timeout "$HEALTH_URL$endpoint" >/dev/null 2>&1; then
        return 0
    else
        return 1
    fi
}

# Function to safely get JSON value
safe_jq() {
    local data="$1"
    local query="$2"
    local default="$3"
    
    echo "$data" | jq -r "$query" 2>/dev/null || echo "$default"
}

# Function to display the dashboard
display_dashboard() {
    clear
    
    # Header
    echo -e "${BLUE}╔══════════════════════════════════════════════════════════════════════════════╗${NC}"
    echo -e "${BLUE}║${NC}                    ${CYAN}🏥 .NET Memory App Health Dashboard${NC}                     ${BLUE}║${NC}"
    echo -e "${BLUE}╠══════════════════════════════════════════════════════════════════════════════╣${NC}"
    echo -e "${BLUE}║${NC} $(date '+%Y-%m-%d %H:%M:%S UTC')                                   Refresh: ${REFRESH_INTERVAL}s ${BLUE}║${NC}"
    echo -e "${BLUE}╚══════════════════════════════════════════════════════════════════════════════╝${NC}"
    echo ""
    
    # Test connectivity first
    if ! test_endpoint "/health"; then
        echo -e "${RED}❌ Cannot connect to health endpoints at $HEALTH_URL${NC}"
        echo -e "${YELLOW}💡 Make sure the application is running and accessible${NC}"
        echo ""
        echo "Trying to connect..."
        return
    fi
    
    # Get health data
    local quick_status=$(curl -s --max-time 3 "$HEALTH_URL/status" 2>/dev/null)
    local detailed_health=$(curl -s --max-time 5 "$HEALTH_URL/health/detailed" 2>/dev/null)
    local memory_health=$(curl -s --max-time 3 "$HEALTH_URL/health/memory" 2>/dev/null)
    local gc_health=$(curl -s --max-time 3 "$HEALTH_URL/health/gc" 2>/dev/null)
    local container_health=$(curl -s --max-time 3 "$HEALTH_URL/health/container" 2>/dev/null)
    
    # Overall Status Section
    echo -e "${PURPLE}▊ OVERALL STATUS${NC}"
    echo "════════════════"
    
    local overall_status=$(safe_jq "$detailed_health" ".status" "Unknown")
    local status_color=$(get_status_color "$overall_status")
    local uptime=$(safe_jq "$detailed_health" ".checks.application.uptime" "Unknown")
    local version=$(safe_jq "$detailed_health" ".version" "Unknown")
    
    echo -e "Status: ${status_color}●${NC} $overall_status"
    echo -e "Version: $version"
    echo -e "Uptime: $uptime"
    
    # Issues section
    local issues=$(safe_jq "$detailed_health" ".issues | length" "0")
    if [ "$issues" != "0" ] && [ "$issues" != "null" ]; then
        echo -e "${RED}Issues:${NC}"
        echo "$detailed_health" | jq -r '.issues[]?' 2>/dev/null | sed 's/^/  • /'
    fi
    echo ""
    
    # Health Checks Grid
    echo -e "${PURPLE}▊ HEALTH CHECKS${NC}"
    echo "═══════════════"
    
    # Row 1: Memory and GC
    printf "%-25s %-25s\n" "🧠 MEMORY HEALTH" "🗑️  GARBAGE COLLECTION"
    printf "%-25s %-25s\n" "─────────────────────────" "─────────────────────────"
    
    local mem_status=$(safe_jq "$memory_health" ".status" "Unknown")
    local mem_color=$(get_status_color "$mem_status")
    local mem_pressure=$(safe_jq "$memory_health" ".metrics.memoryPressurePercent" "0")
    local working_set=$(safe_jq "$memory_health" ".metrics.workingSetMB" "0")
    
    local gc_status=$(safe_jq "$gc_health" ".status" "Unknown") 
    local gc_color=$(get_status_color "$gc_status")
    local gen2_collections=$(safe_jq "$gc_health" ".collections.generation2" "0")
    local gen2_ratio=$(safe_jq "$gc_health" ".collections.gen2Ratio" "0")
    
    printf "%-34s %-34s\n" "Status: ${mem_color}●${NC} $mem_status" "Status: ${gc_color}●${NC} $gc_status"
    printf "%-34s %-34s\n" "Pressure: $(format_percentage "$mem_pressure" 70 85)" "Gen2 Collections: $gen2_collections"
    printf "%-34s %-34s\n" "Working Set: ${working_set}MB" "Gen2 Ratio: ${gen2_ratio}%"
    echo ""
    
    # Row 2: Container and Performance  
    printf "%-25s %-25s\n" "📦 CONTAINER HEALTH" "⚡ PERFORMANCE"
    printf "%-25s %-25s\n" "─────────────────────────" "─────────────────────────"
    
    local container_status=$(safe_jq "$container_health" ".status" "Unknown")
    local container_color=$(get_status_color "$container_status")
    local utilization=$(safe_jq "$container_health" ".container.utilizationPercent" "0")
    local limit_mb=$(safe_jq "$container_health" ".container.estimatedLimitMB" "0")
    
    # Get performance data from detailed health
    local perf_status=$(safe_jq "$detailed_health" ".checks.performance.status" "Unknown")
    local perf_color=$(get_status_color "$perf_status")
    local thread_count=$(safe_jq "$detailed_health" ".checks.performance.process.threadCount" "0")
    local handle_count=$(safe_jq "$detailed_health" ".checks.performance.process.handleCount" "0")
    
    printf "%-34s %-34s\n" "Status: ${container_color}●${NC} $container_status" "Status: ${perf_color}●${NC} $perf_status"
    printf "%-34s %-34s\n" "Utilization: $(format_percentage "$utilization" 80 90)" "Threads: $thread_count"
    printf "%-34s %-34s\n" "Limit: ${limit_mb}MB" "Handles: $handle_count"
    echo ""
    
    # Application Metrics
    echo -e "${PURPLE}▊ APPLICATION METRICS${NC}"
    echo "═══════════════════════"
    
    local iteration=$(safe_jq "$quick_status" ".iteration" "0")
    local allocated_mb=$(safe_jq "$quick_status" ".allocatedMB" "0") 
    local container_util=$(safe_jq "$quick_status" ".containerUtilization" "0")
    local mem_press=$(safe_jq "$quick_status" ".memoryPressure" "0")
    
    echo "Iteration: $iteration"
    echo "Allocated Memory: ${allocated_mb}MB"
    echo "Container Utilization: $(format_percentage "$container_util" 80 90)"
    echo "Memory Pressure: $(format_percentage "$mem_press" 70 85)"
    echo ""
    
    # Kubernetes Health Endpoints Status
    echo -e "${PURPLE}▊ KUBERNETES HEALTH ENDPOINTS${NC}"
    echo "═══════════════════════════════"
    
    local live_status="❌"
    local ready_status="❌"
    local startup_status="❌"
    
    if test_endpoint "/health/live"; then
        live_status="${GREEN}✅${NC}"
    fi
    
    if test_endpoint "/health/ready"; then
        ready_status="${GREEN}✅${NC}"
    fi
    
    # Test if we can get a good response from main health endpoint
    if curl -s --max-time 3 "$HEALTH_URL/health" | jq -e '.status' >/dev/null 2>&1; then
        startup_status="${GREEN}✅${NC}"
    fi
    
    echo -e "Liveness Probe (/health/live):  $live_status"
    echo -e "Readiness Probe (/health/ready): $ready_status" 
    echo -e "Health Endpoint (/health):       $startup_status"
    echo ""
    
    # Warnings and Alerts
    echo -e "${PURPLE}▊ ALERTS & WARNINGS${NC}"
    echo "═══════════════════"
    
    local has_alerts=false
    
    # Check memory warnings
    if echo "$memory_health" | jq -e '.warnings[]?' >/dev/null 2>&1; then
        echo -e "${YELLOW}Memory Warnings:${NC}"
        echo "$memory_health" | jq -r '.warnings[]?' 2>/dev/null | sed 's/^/  ⚠️  /'
        has_alerts=true
    fi
    
    # Check container alerts
    if echo "$container_health" | jq -e '.alerts[]?' >/dev/null 2>&1; then
        echo -e "${RED}Container Alerts:${NC}"
        echo "$container_health" | jq -r '.alerts[]?' 2>/dev/null | sed 's/^/  🚨 /'
        has_alerts=true
    fi
    
    # Check GC issues
    if echo "$gc_health" | jq -e '.issues[]?' >/dev/null 2>&1; then
        echo -e "${YELLOW}GC Issues:${NC}"
        echo "$gc_health" | jq -r '.issues[]?' 2>/dev/null | sed 's/^/  ⚡ /'
        has_alerts=true
    fi
    
    if [ "$has_alerts" = false ]; then
        echo -e "${GREEN}✅ No alerts or warnings${NC}"
    fi
    echo ""
    
    # Footer
    echo -e "${BLUE}────────────────────────────────────────────────────────────────────────────────${NC}"
    echo -e "${CYAN}💡 Health Endpoints: /health/live | /health/ready | /health/detailed | /health/memory${NC}"
    echo -e "${CYAN}🔗 App URL: $HEALTH_URL | Press Ctrl+C to exit${NC}"
    echo -e "${BLUE}────────────────────────────────────────────────────────────────────────────────${NC}"
}

# Main monitoring loop
main() {
    echo -e "${GREEN}🚀 Starting Health Check Monitoring Dashboard...${NC}"
    echo -e "${BLUE}📊 Connecting to: $HEALTH_URL${NC}"
    echo -e "${YELLOW}⏰ Refresh interval: ${REFRESH_INTERVAL} seconds${NC}"
    echo ""
    echo "Press Ctrl+C to exit"
    sleep 2
    
    # Trap Ctrl+C for clean exit
    trap 'echo -e "\n${GREEN}✅ Health monitoring stopped${NC}"; exit 0' INT
    
    while true; do
        display_dashboard
        sleep $REFRESH_INTERVAL
    done
}

# Check for command line arguments
case "${1:-}" in
    -h|--help)
        echo "Health Check Monitoring Dashboard"
        echo ""
        echo "Usage: $0 [options]"
        echo ""
        echo "Options:"
        echo "  -h, --help     Show this help message"
        echo ""
        echo "Environment Variables:"
        echo "  HEALTH_URL           Health endpoint URL (default: http://localhost:5000)"
        echo "  REFRESH_INTERVAL     Refresh interval in seconds (default: 5)"
        echo ""
        echo "Examples:"
        echo "  $0                                    # Use defaults"
        echo "  HEALTH_URL=http://app:5000 $0       # Custom URL"
        echo "  REFRESH_INTERVAL=10 $0              # 10 second refresh"
        exit 0
        ;;
    *)
        main
        ;;
esac
