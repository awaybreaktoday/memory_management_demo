#!/bin/bash

# Complete Deployment Script for .NET Memory App with Prometheus & Grafana
set -e

echo "🚀 Deploying .NET Memory App with Observability Stack"

# Configuration
NAMESPACE=${NAMESPACE:-"default"}
APP_NAME="dotnet-memory-app"
PROMETHEUS_PORT=${PROMETHEUS_PORT:-9090}
GRAFANA_PORT=${GRAFANA_PORT:-3000}

# Function to check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Check prerequisites
echo "📋 Checking prerequisites..."
if ! command_exists kubectl; then
    echo "❌ kubectl not found. Please install kubectl."
    exit 1
fi

if ! command_exists helm; then
    echo "⚠️  helm not found. Will use kubectl for deployments."
    USE_HELM=false
else
    USE_HELM=true
fi

# Deploy the application
echo "🔧 Deploying .NET Memory Application..."
kubectl apply -f - <<EOF
apiVersion: apps/v1
kind: Deployment
metadata:
  name: dotnet-memory-app-prometheus
  namespace: $NAMESPACE
  labels:
    app: dotnet-memory-app-prometheus
spec:
  replicas: 1
  selector:
    matchLabels:
      app: dotnet-memory-app-prometheus
  template:
    metadata:
      labels:
        app: dotnet-memory-app-prometheus
      annotations:
        prometheus.io/scrape: "true"
        prometheus.io/port: "5000"
        prometheus.io/path: "/metrics"
    spec:
      containers:
      - name: dotnet-memory-app
        image: awaybreaktoday/dotnet-memory-app:v1.0.5-amd64
        ports:
        - containerPort: 5000
          name: metrics
          protocol: TCP
        resources:
          limits:
            memory: "256Mi"
            cpu: "200m"
          requests:
            memory: "128Mi"
            cpu: "100m"
        env:
        - name: DOTNET_RUNNING_IN_CONTAINER
          value: "true"
        - name: DOTNET_GCHeapHardLimitPercent
          value: "70"
        - name: DOTNET_GCServer
          value: "true"
        - name: DOTNET_GCConserveMemory
          value: "9"
        livenessProbe:
          httpGet:
            path: /health
            port: 5000
          initialDelaySeconds: 30
          periodSeconds: 10
---
apiVersion: v1
kind: Service
metadata:
  name: dotnet-memory-app-service
  namespace: $NAMESPACE
  labels:
    app: dotnet-memory-app-prometheus
  annotations:
    prometheus.io/scrape: "true"
    prometheus.io/port: "5000"
    prometheus.io/path: "/metrics"
spec:
  selector:
    app: dotnet-memory-app-prometheus
  ports:
  - name: metrics
    port: 5000
    targetPort: 5000
    protocol: TCP
  type: ClusterIP
EOF

# Deploy Prometheus if using Helm
if [ "$USE_HELM" = true ]; then
    echo "🔍 Deploying Prometheus using Helm..."
    helm repo add prometheus-community https://prometheus-community.github.io/helm-charts || true
    helm repo update
    
    helm upgrade --install prometheus prometheus-community/kube-prometheus-stack \
        --namespace monitoring --create-namespace \
        --set prometheus.prometheusSpec.serviceMonitorSelectorNilUsesHelmValues=false \
        --set prometheus.prometheusSpec.podMonitorSelectorNilUsesHelmValues=false \
        --wait
else
    echo "⚠️  Please install Prometheus manually or use Helm for automated setup"
fi

# Create ServiceMonitor for Prometheus Operator
echo "📊 Creating ServiceMonitor..."
kubectl apply -f - <<EOF
apiVersion: monitoring.coreos.com/v1
kind: ServiceMonitor
metadata:
  name: dotnet-memory-app-monitor
  namespace: $NAMESPACE
  labels:
    app: dotnet-memory-app-prometheus
spec:
  selector:
    matchLabels:
      app: dotnet-memory-app-prometheus
  endpoints:
  - port: metrics
    path: /metrics
    interval: 15s
EOF

# Wait for deployment
echo "⏳ Waiting for deployment to be ready..."
kubectl wait --for=condition=available --timeout=300s deployment/dotnet-memory-app-prometheus -n $NAMESPACE

# Get pod status
echo "📱 Checking pod status..."
kubectl get pods -l app=dotnet-memory-app-prometheus -n $NAMESPACE

# Setup port forwarding
echo "🌐 Setting up port forwarding..."

# Port forward app metrics
kubectl port-forward service/dotnet-memory-app-service 5000:5000 -n $NAMESPACE &
APP_PF_PID=$!

# Port forward Prometheus (if available)
if kubectl get service prometheus-kube-prometheus-prometheus -n monitoring >/dev/null 2>&1; then
    kubectl port-forward service/prometheus-kube-prometheus-prometheus $PROMETHEUS_PORT:9090 -n monitoring &
    PROM_PF_PID=$!
    echo "📊 Prometheus available at: http://localhost:$PROMETHEUS_PORT"
fi

# Port forward Grafana (if available)
if kubectl get service prometheus-grafana -n monitoring >/dev/null 2>&1; then
    kubectl port-forward service/prometheus-grafana $GRAFANA_PORT:80 -n monitoring &
    GRAFANA_PF_PID=$!
    echo "📈 Grafana available at: http://localhost:$GRAFANA_PORT"
    echo "   Default credentials: admin/prom-operator"
fi

echo ""
echo "✅ Deployment completed successfully!"
echo ""
echo "📊 Access points:"
echo "   App Metrics: http://localhost:5000/metrics"
echo "   App Health:  http://localhost:5000/health"

if [ ! -z "$PROM_PF_PID" ]; then
    echo "   Prometheus:  http://localhost:$PROMETHEUS_PORT"
fi

if [ ! -z "$GRAFANA_PF_PID" ]; then
    echo "   Grafana:     http://localhost:$GRAFANA_PORT"
fi

echo ""
echo "🔧 Next steps:"
echo "1. Verify metrics: curl http://localhost:5000/metrics | grep dotnet_"
echo "2. Import Grafana dashboard using the provided JSON"
echo "3. Setup alert rules in Grafana"
echo "4. Monitor the application behavior"
echo ""
echo "📝 Useful commands:"
echo "   Watch pod status: kubectl get pods -l app=dotnet-memory-app-prometheus -n $NAMESPACE -w"
echo "   View logs:        kubectl logs -f deployment/dotnet-memory-app-prometheus -n $NAMESPACE"
echo "   Check metrics:    kubectl top pod -l app=dotnet-memory-app-prometheus -n $NAMESPACE"
echo ""

# Function to cleanup on exit
cleanup() {
    echo "🧹 Cleaning up port forwards..."
    [ ! -z "$APP_PF_PID" ] && kill $APP_PF_PID >/dev/null 2>&1 || true
    [ ! -z "$PROM_PF_PID" ] && kill $PROM_PF_PID >/dev/null 2>&1 || true
    [ ! -z "$GRAFANA_PF_PID" ] && kill $GRAFANA_PF_PID >/dev/null 2>&1 || true
}

# Trap cleanup on script exit
trap cleanup EXIT

echo "🎯 Press Ctrl+C to stop port forwarding and exit"
echo "📊 Monitoring memory behavior - watch for GC patterns and OutOfMemoryExceptions..."

# Keep script running
wait
