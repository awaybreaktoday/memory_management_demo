# .NET Memory Management Demo for Kubernetes

A comprehensive demonstration of .NET container memory management, Kubernetes orchestration, and production observability patterns.

[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![Kubernetes](https://img.shields.io/badge/Kubernetes-1.25+-blue.svg)](https://kubernetes.io/)
[![Prometheus](https://img.shields.io/badge/Prometheus-Metrics-orange.svg)](https://prometheus.io/)
[![Docker](https://img.shields.io/badge/Docker-Container-blue.svg)](https://docker.com/)

## 🎯 Project Overview

This project demonstrates **enterprise-grade container memory management** using .NET 8 applications in Kubernetes. It showcases the evolution from basic containerization to production-ready applications with comprehensive observability, health monitoring, and automatic scaling.

### What This Project Demonstrates

- **🧠 Container Memory Management**: How .NET applications handle memory within Kubernetes constraints
- **📊 Advanced Observability**: Prometheus metrics, Grafana dashboards, and health monitoring
- **🔄 Horizontal Pod Autoscaling**: Automatic scaling based on resource usage and custom metrics  
- **🏥 Production Health Checks**: Comprehensive health monitoring with liveness/readiness probes
- **⚡ Garbage Collection Optimization**: Container-aware GC configuration and monitoring
- **🛡️ Resource Protection**: Preventing OOMKills through intelligent memory management

## 🚀 Quick Start

### Prerequisites

- Docker Desktop or compatible container runtime
- Kubernetes cluster (local or cloud)
- kubectl configured
- Helm 3.x (optional, for monitoring stack)

### 1-Minute Demo

```bash
# Clone and deploy
git clone <repository-url>
cd DotNetMemoryApp

# Build and deploy
docker build -t dotnet-memory-app:demo .
kubectl apply -f advanced-health-deployment.yaml

# Monitor in real-time
kubectl port-forward service/dotnet-memory-app-advanced-health-service 5000:5000 &
./health-monitor.sh
```

## 📋 Features

### Core Functionality
- ✅ **Memory Allocation Simulation** - Configurable memory pressure generation
- ✅ **Container-Aware GC** - Intelligent garbage collection within container limits
- ✅ **Dynamic Memory Detection** - Automatic container limit discovery
- ✅ **OutOfMemoryException Handling** - Graceful memory pressure management

### Observability & Monitoring
- ✅ **Prometheus Metrics** - 25+ custom metrics for memory, GC, and performance
- ✅ **Grafana Dashboard** - Real-time visualization of memory behavior
- ✅ **Health Check Endpoints** - 8 specialized health monitoring endpoints
- ✅ **Real-time Monitoring** - Live dashboard for operations teams

### Kubernetes Integration
- ✅ **Advanced Health Probes** - Liveness, readiness, and startup probes
- ✅ **Horizontal Pod Autoscaling** - Resource and custom metric-based scaling
- ✅ **Service Monitoring** - Prometheus ServiceMonitor integration
- ✅ **Resource Management** - Proper requests/limits configuration

### Production Readiness
- ✅ **Security Context** - Non-root user, capability dropping
- ✅ **Pod Disruption Budget** - High availability configuration  
- ✅ **ConfigMap Integration** - Externalized configuration
- ✅ **Performance Monitoring** - Latency and throughput tracking

## 🏗️ Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Grafana       │    │   Prometheus    │    │   AlertManager  │
│   Dashboard     │◄───┤   Metrics       │◄───┤   Alerts        │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Kubernetes Cluster                          │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐            │
│  │    Pod 1    │  │    Pod 2    │  │    Pod N    │            │
│  │             │  │             │  │             │            │
│  │ ┌─────────┐ │  │ ┌─────────┐ │  │ ┌─────────┐ │            │
│  │ │ .NET    │ │  │ │ .NET    │ │  │ │ .NET    │ │            │
│  │ │ Memory  │ │  │ │ Memory  │ │  │ │ Memory  │ │            │
│  │ │ App     │ │  │ │ App     │ │  │ │ App     │ │            │
│  │ └─────────┘ │  │ └─────────┘ │  │ └─────────┘ │            │
│  └─────────────┘  └─────────────┘  └─────────────┘            │
│           ▲                ▲                ▲                 │
│           │                │                │                 │
│  ┌─────────────────────────────────────────────────────────┐  │
│  │              Horizontal Pod Autoscaler                  │  │
│  │     Scales based on CPU, Memory, and Custom Metrics    │  │
│  └─────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

## 📁 Project Structure

```
DotNetMemoryApp/
├── 📄 README.md                           # This file
├── 🔧 Core Application Files
│   ├── Program.cs                         # Main application with Prometheus & health checks
│   ├── AdvancedHealthChecks.cs           # Comprehensive health monitoring system
│   ├── MyMemoryLogger.cs                 # Dynamic container-aware logging
│   ├── DotNetMemoryApp.csproj            # Project configuration
│   └── Dockerfile                        # Container build configuration
├── 🚢 Kubernetes Deployments
│   ├── advanced-health-deployment.yaml   # Production deployment with health checks
│   ├── basic-hpa.yaml                    # Horizontal Pod Autoscaler config
│   └── prometheus-deployment.yaml        # Monitoring stack deployment
├── 📊 Monitoring & Observability
│   ├── grafana-dashboard.json            # Memory monitoring dashboard
│   ├── prometheus-config.yaml           # Metrics scraping configuration
│   └── alert-rules.yaml                 # Alerting rules
├── 🛠️ Tools & Scripts
│   ├── health-monitor.sh                 # Real-time health monitoring dashboard
│   ├── deploy-script.sh                  # Automated deployment script
│   └── test-script.sh                    # End-to-end testing
└── 📚 Documentation
    ├── docs/                             # Detailed documentation
    ├── examples/                         # Usage examples
    └── troubleshooting.md               # Common issues and solutions
```

## 🔧 Installation & Setup

### Method 1: Automated Setup (Recommended)

```bash
# Clone the repository
git clone <repository-url>
cd DotNetMemoryApp

# Run automated setup
chmod +x deploy-script.sh
./deploy-script.sh

# Start monitoring
./health-monitor.sh
```

### Method 2: Manual Setup

#### Step 1: Build the Application

```bash
# Build Docker image
docker build -t awaybreaktoday/dotnet-memory-app:v1.0.8 .

# Push to registry (optional)
docker push awaybreaktoday/dotnet-memory-app:v1.0.8
```

#### Step 2: Deploy to Kubernetes

```bash
# Deploy application with health checks
kubectl apply -f advanced-health-deployment.yaml

# Deploy HPA (optional)
kubectl apply -f basic-hpa.yaml

# Verify deployment
kubectl get pods -l app=dotnet-memory-app-advanced-health
```

#### Step 3: Install Monitoring Stack

```bash
# Add Prometheus Helm repository
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo update

# Install Prometheus and Grafana
helm install prometheus prometheus-community/kube-prometheus-stack \
  --namespace monitoring --create-namespace

# Import Grafana dashboard
kubectl port-forward -n monitoring service/prometheus-grafana 3000:80
# Open http://localhost:3000 and import grafana-dashboard.json
```

## 📊 Monitoring & Observability

### Prometheus Metrics

The application exposes 25+ custom metrics:

#### Memory Metrics
```promql
# Current memory allocation
dotnet_memory_current_allocated_mb

# Memory pressure percentage
dotnet_gc_memory_pressure_percent

# Container utilization
dotnet_container_utilization_percent

# Working set memory
dotnet_process_working_set_mb
```

#### Health Metrics
```promql
# Health check status (1=healthy, 0.5=degraded, 0=unhealthy)
dotnet_health_check_status{check_name="memory"}

# Health check duration
dotnet_health_check_duration_seconds{check_name="gc"}
```

#### GC Metrics
```promql
# Garbage collection frequency
rate(dotnet_gc_collections_total[1m])

# OutOfMemoryException rate
rate(dotnet_out_of_memory_exceptions_total[5m])
```

### Grafana Dashboard

Import the provided dashboard (`grafana-dashboard.json`) to get:

- **Memory Usage Overview** - Real-time memory consumption
- **GC Performance** - Garbage collection efficiency
- **Health Status** - Application health indicators
- **Container Utilization** - Resource usage vs limits
- **Performance Metrics** - Latency and throughput

### Health Check Endpoints

| Endpoint | Purpose | Use Case |
|----------|---------|----------|
| `/health/live` | Liveness probe | Kubernetes pod restart decisions |
| `/health/ready` | Readiness probe | Load balancer traffic routing |
| `/health/detailed` | Comprehensive status | Operations dashboard |
| `/health/memory` | Memory analysis | Memory leak detection |
| `/health/gc` | GC performance | .NET optimization |
| `/health/container` | Resource utilization | Container monitoring |
| `/health/performance` | Latency metrics | Performance analysis |

## 🔄 Horizontal Pod Autoscaling

### Resource-Based Scaling

```yaml
# CPU and Memory scaling
metrics:
- type: Resource
  resource:
    name: cpu
    target:
      type: Utilization
      averageUtilization: 70
- type: Resource
  resource:
    name: memory
    target:
      type: Utilization
      averageUtilization: 80
```

### Custom Metrics Scaling

```yaml
# Application-specific scaling
- type: Pods
  pods:
    metric:
      name: dotnet_gc_memory_pressure_percent
    target:
      type: AverageValue
      averageValue: "85"
```

### Testing HPA

```bash
# Increase load to trigger scaling
kubectl patch deployment dotnet-memory-app-advanced-health \
  -p='{"spec":{"template":{"spec":{"containers":[{"name":"dotnet-memory-app","env":[{"name":"ALLOCATION_SIZE_MB","value":"25"}]}]}}}}'

# Monitor scaling
watch kubectl get hpa
```

## 🧪 Testing & Validation

### Health Check Testing

```bash
# Test all health endpoints
curl http://localhost:5000/health/live
curl http://localhost:5000/health/ready
curl http://localhost:5000/health/detailed | jq .
curl http://localhost:5000/health/memory | jq .
```

### Memory Pressure Testing

```bash
# Generate memory pressure
kubectl set env deployment/dotnet-memory-app-advanced-health \
  ALLOCATION_SIZE_MB=30 ALLOCATION_INTERVAL_SECONDS=1

# Monitor health status
watch 'curl -s http://localhost:5000/health/memory | jq ".status, .metrics.memoryPressurePercent"'
```

### Load Testing

```bash
# Run load test
./test-script.sh

# Expected behavior:
# 1. Memory allocation grows
# 2. GC pressure increases
# 3. Health status changes to "Degraded"
# 4. OutOfMemoryException occurs
# 5. Memory clears and cycle repeats
```

## 🎛️ Configuration

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `ALLOCATION_SIZE_MB` | `10` | Memory allocated per iteration |
| `ALLOCATION_INTERVAL_SECONDS` | `2` | Delay between allocations |
| `DOTNET_GCHeapHardLimitPercent` | `70` | GC heap limit as % of container |
| `DOTNET_GCServer` | `true` | Enable server GC mode |
| `ENABLE_CPU_LOAD` | `false` | Generate artificial CPU load |

### Container Limits

```yaml
resources:
  limits:
    memory: "1024Mi"    # Container memory limit
    cpu: "1000m"        # CPU limit
  requests:
    memory: "256Mi"     # Guaranteed memory
    cpu: "200m"         # Guaranteed CPU
```

### Health Check Thresholds

```yaml
# ConfigMap: dotnet-memory-app-health-config
data:
  memory-pressure-warning: "70"
  memory-pressure-critical: "85"
  container-utilization-warning: "80"
  container-utilization-critical: "90"
```

## 🐛 Troubleshooting

### Common Issues

#### 1. Pod OOMKilled Despite GC Configuration

**Symptoms:** Pod restarts with OOMKilled status
```bash
kubectl describe pod <pod-name> | grep -i oom
```

**Solutions:**
- Verify GC environment variables are set
- Check container memory limits
- Review memory allocation rate
- Examine GC effectiveness in logs

#### 2. Health Checks Failing

**Symptoms:** Pod not ready or restarting frequently
```bash
kubectl describe pod <pod-name> | grep -A 10 Events
```

**Solutions:**
- Check health endpoint responses
- Verify port configuration
- Review health check timeouts
- Examine application logs

#### 3. Metrics Not Available

**Symptoms:** Prometheus not scraping metrics
```bash
kubectl get servicemonitor
curl http://pod-ip:5000/metrics
```

**Solutions:**
- Verify ServiceMonitor configuration
- Check Prometheus targets
- Validate metrics endpoint
- Review network policies

#### 4. HPA Not Scaling

**Symptoms:** No scaling despite high resource usage
```bash
kubectl describe hpa dotnet-memory-app-advanced-health-hpa
```

**Solutions:**
- Verify metrics server is running
- Check resource requests are set
- Review HPA metrics
- Validate scaling policies

### Debugging Commands

```bash
# Application logs
kubectl logs -f deployment/dotnet-memory-app-advanced-health

# Health check status
kubectl exec -it <pod-name> -- curl localhost:5000/health/detailed

# Resource usage
kubectl top pods -l app=dotnet-memory-app-advanced-health

# HPA status
kubectl get hpa -w

# Prometheus targets
kubectl port-forward -n monitoring service/prometheus-kube-prometheus-prometheus 9090:9090
# Open http://localhost:9090/targets
```

## 📈 Performance Characteristics

### Memory Behavior

| Phase | Duration | Memory Usage | GC Activity | Status |
|-------|----------|--------------|-------------|---------|
| Startup | 0-30s | 50-100MB | Low | Healthy |
| Growth | 30s-5min | 100-600MB | Moderate | Healthy |
| Pressure | 5-8min | 600-700MB | High | Degraded |
| Cleanup | 8min+ | 100-200MB | Very High | Recovering |

### Scaling Behavior

```
Pods: 1 → 2 → 3 → 2 → 1
Time: 0min → 2min → 5min → 10min → 15min
Load: Low → High → Peak → Moderate → Low
```

### Resource Utilization

- **CPU**: 20-90% (variable with artificial load)
- **Memory**: 100MB-700MB (sawtooth pattern)
- **Network**: <1MB/s (metrics and health checks)
- **Storage**: <100MB (container image)

## 🔐 Security Considerations

### Container Security

```yaml
securityContext:
  allowPrivilegeEscalation: false
  runAsNonRoot: true
  runAsUser: 1000
  readOnlyRootFilesystem: false
  capabilities:
    drop:
    - ALL
```

### Network Security

- Health endpoints exposed only within cluster
- Metrics endpoint protected by Kubernetes RBAC
- No external traffic required for operation

### RBAC Requirements

```yaml
# Minimal permissions required
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRole
metadata:
  name: dotnet-memory-app-viewer
rules:
- apiGroups: [""]
  resources: ["pods", "services"]
  verbs: ["get", "list"]
```

## 🚀 Advanced Usage

### Custom Metrics Development

```csharp
// Add new metrics
private static readonly Gauge _customMetric = Metrics
    .CreateGauge("dotnet_custom_metric", "Description");

// Update in application logic
_customMetric.Set(value);
```

### Health Check Extensions

```csharp
// Add custom health check
builder.Services.AddHealthChecks()
    .AddCheck<CustomHealthCheck>("custom", tags: new[] { "ready" });
```

### Alert Rule Configuration

```yaml
# Custom alert rules
groups:
- name: dotnet-memory-app
  rules:
  - alert: HighMemoryPressure
    expr: dotnet_gc_memory_pressure_percent > 85
    for: 2m
    labels:
      severity: warning
```

## 🤝 Contributing

### Development Setup

```bash
# Clone repository
git clone <repository-url>
cd DotNetMemoryApp

# Install dependencies
dotnet restore

# Run locally
dotnet run

# Run tests
dotnet test
```

### Code Standards

- Follow C# coding conventions
- Include XML documentation
- Add unit tests for new features
- Update README for significant changes

### Pull Request Process

1. Fork the repository
2. Create feature branch
3. Implement changes with tests
4. Update documentation
5. Submit pull request

## 📚 Additional Resources

### Documentation Links

- [.NET Container Configuration](https://docs.microsoft.com/en-us/dotnet/core/runtime-config/)
- [Kubernetes Health Checks](https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-startup-probes/)
- [Prometheus Metrics](https://prometheus.io/docs/concepts/metric_types/)
- [Grafana Dashboards](https://grafana.com/docs/grafana/latest/dashboards/)

### Related Projects

- [.NET Performance Samples](https://github.com/dotnet/performance)
- [Kubernetes Examples](https://github.com/kubernetes/examples)
- [Prometheus .NET Client](https://github.com/prometheus-net/prometheus-net)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🏷️ Version History

- **v1.0.8** - Advanced health checks and monitoring
- **v1.0.7** - HPA integration and custom metrics
- **v1.0.6** - Dynamic container detection
- **v1.0.5** - Prometheus metrics integration
- **v1.0.4** - Basic health checks
- **v1.0.3** - Container-aware GC configuration
- **v1.0.2** - Memory pressure simulation
- **v1.0.1** - Basic Kubernetes deployment
- **v1.0.0** - Initial release

---

## 🎯 Project Goals Achieved

✅ **Container Memory Management** - Demonstrated .NET memory behavior in containers  
✅ **Production Observability** - Comprehensive metrics and monitoring  
✅ **Kubernetes Integration** - Advanced health checks and scaling  
✅ **Enterprise Readiness** - Security, reliability, and operations  
✅ **Educational Value** - Clear examples and documentation  

---

**Made with ❤️ for the Kubernetes and .NET communities**
