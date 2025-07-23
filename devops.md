# DevOps Plan

## Table of Contents
- [Overview](#overview)
- [CI/CD Pipeline](#cicd-pipeline)
- [Docker Containerization](#docker-containerization)
- [Kubernetes Deployment](#kubernetes-deployment)
- [Helm Charts](#helm-charts)
- [Monitoring and Logging](#monitoring-and-logging)
- [Security Considerations](#security-considerations)
- [Infrastructure as Code](#infrastructure-as-code)
- [Disaster Recovery](#disaster-recovery)

## Overview

This document outlines the DevOps strategy for the TODO API project, including continuous integration/continuous deployment (CI/CD), containerization with Docker, orchestration with Kubernetes, and package management with Helm.

## CI/CD Pipeline

### Pipeline Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Source Code   │───▶│   Build Stage   │───▶│   Test Stage    │───▶│  Deploy Stage   │
│   Repository    │    │                 │    │                 │    │                 │
└─────────────────┘    └─────────────────┘    └─────────────────┘    └─────────────────┘
```

### Pipeline Stages

#### 1. Source Stage
- **Trigger**: Push to main branch, pull request creation
- **Tools**: GitHub Actions, Azure DevOps, or GitLab CI
- **Actions**:
  - Code quality checks
  - Security scanning
  - Dependency vulnerability assessment

#### 2. Build Stage
- **Tools**: .NET CLI, Docker
- **Actions**:
  ```yaml
  - name: Restore dependencies
    run: dotnet restore
  
  - name: Build application
    run: dotnet build --no-restore --configuration Release
  
  - name: Run tests
    run: dotnet test --no-build --verbosity normal --configuration Release
  
  - name: Publish application
    run: dotnet publish -c Release -o ./publish
  ```

#### 3. Test Stage
- **Unit Tests**: xUnit, NUnit
- **Integration Tests**: TestServer, WebApplicationFactory
- **Performance Tests**: NBomber, k6
- **Security Tests**: OWASP ZAP, SonarQube

#### 4. Deploy Stage
- **Environments**: Development, Staging, Production
- **Tools**: Kubernetes, Helm
- **Actions**:
  - Build Docker image
  - Push to container registry
  - Deploy to Kubernetes cluster

### Pipeline Configuration

#### GitHub Actions Example
```yaml
name: CI/CD Pipeline

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore --configuration Release
    
    - name: Test
      run: dotnet test --no-build --verbosity normal --configuration Release
    
    - name: Build Docker image
      run: docker build -t todo-api:${{ github.sha }} .
    
    - name: Push Docker image
      run: |
        echo ${{ secrets.DOCKER_PASSWORD }} | docker login -u ${{ secrets.DOCKER_USERNAME }} --password-stdin
        docker push todo-api:${{ github.sha }}
    
    - name: Deploy to Kubernetes
      run: |
        helm upgrade --install todo-api ./helm-charts/todo-api \
          --set image.tag=${{ github.sha }} \
          --set environment=staging
```

## Docker Containerization

### Multi-Stage Dockerfile

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["TodoApi/TodoApi.csproj", "TodoApi/"]
COPY ["TodoApi.Tests/TodoApi.Tests.csproj", "TodoApi.Tests/"]

# Restore dependencies
RUN dotnet restore "TodoApi/TodoApi.csproj"

# Copy source code
COPY . .

# Build and test
RUN dotnet build "TodoApi/TodoApi.csproj" -c Release -o /app/build
RUN dotnet test "TodoApi.Tests/TodoApi.Tests.csproj" -c Release --no-build

# Publish stage
FROM build AS publish
RUN dotnet publish "TodoApi/TodoApi.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install dependencies for SignalR
RUN apt-get update && apt-get install -y libgdiplus

# Copy published application
COPY --from=publish /app/publish .

# Create non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Expose ports
EXPOSE 80
EXPOSE 443

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "TodoApi.dll"]
```

### Docker Compose for Development

```yaml
version: '3.8'

services:
  todo-api:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "5000:80"
      - "5001:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Server=db;Database=TodoDb;User=sa;Password=YourStrong@Passw0rd;
    depends_on:
      - db
    networks:
      - todo-network

  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrong@Passw0rd
    ports:
      - "1433:1433"
    volumes:
      - sqlserver_data:/var/opt/mssql
    networks:
      - todo-network

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    networks:
      - todo-network

volumes:
  sqlserver_data:

networks:
  todo-network:
    driver: bridge
```

## Kubernetes Deployment

### Namespace Structure

```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: todo-app
  labels:
    name: todo-app
```

### ConfigMap for Application Settings

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: todo-api-config
  namespace: todo-app
data:
  ASPNETCORE_ENVIRONMENT: "Production"
  Logging__LogLevel__Default: "Information"
  Logging__LogLevel__Microsoft: "Warning"
  Logging__LogLevel__Microsoft.Hosting.Lifetime: "Information"
```

### Secret for Sensitive Data

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: todo-api-secrets
  namespace: todo-app
type: Opaque
data:
  ConnectionStrings__DefaultConnection: <base64-encoded-connection-string>
  JWT__SecretKey: <base64-encoded-jwt-secret>
```

### Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: todo-api
  namespace: todo-app
  labels:
    app: todo-api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: todo-api
  template:
    metadata:
      labels:
        app: todo-api
    spec:
      containers:
      - name: todo-api
        image: todo-api:latest
        ports:
        - containerPort: 80
          name: http
        - containerPort: 443
          name: https
        env:
        - name: ASPNETCORE_ENVIRONMENT
          valueFrom:
            configMapKeyRef:
              name: todo-api-config
              key: ASPNETCORE_ENVIRONMENT
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: todo-api-secrets
              key: ConnectionStrings__DefaultConnection
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 5
          periodSeconds: 5
```

### Service

```yaml
apiVersion: v1
kind: Service
metadata:
  name: todo-api-service
  namespace: todo-app
  labels:
    app: todo-api
spec:
  type: ClusterIP
  ports:
  - port: 80
    targetPort: 80
    protocol: TCP
    name: http
  - port: 443
    targetPort: 443
    protocol: TCP
    name: https
  selector:
    app: todo-api
```

### Ingress

```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: todo-api-ingress
  namespace: todo-app
  annotations:
    kubernetes.io/ingress.class: "nginx"
    cert-manager.io/cluster-issuer: "letsencrypt-prod"
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
spec:
  tls:
  - hosts:
    - api.todo-app.com
    secretName: todo-api-tls
  rules:
  - host: api.todo-app.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: todo-api-service
            port:
              number: 80
```

## Helm Charts

### Chart Structure

```
helm-charts/
└── todo-api/
    ├── Chart.yaml
    ├── values.yaml
    ├── templates/
    │   ├── _helpers.tpl
    │   ├── deployment.yaml
    │   ├── service.yaml
    │   ├── ingress.yaml
    │   ├── configmap.yaml
    │   ├── secret.yaml
    │   └── namespace.yaml
    └── charts/
```

### Chart.yaml

```yaml
apiVersion: v2
name: todo-api
description: A Helm chart for the TODO API application
type: application
version: 1.0.0
appVersion: "1.0.0"
keywords:
  - todo
  - api
  - aspnet-core
home: https://github.com/your-org/todo-api
sources:
  - https://github.com/your-org/todo-api
maintainers:
  - name: Your Name
    email: your.email@example.com
```

### values.yaml

```yaml
# Default values for todo-api
replicaCount: 3

image:
  repository: todo-api
  tag: "latest"
  pullPolicy: IfNotPresent

imagePullSecrets: []
nameOverride: ""
fullnameOverride: ""

serviceAccount:
  create: true
  annotations: {}
  name: ""

podAnnotations: {}

podSecurityContext: {}

securityContext: {}

service:
  type: ClusterIP
  port: 80
  httpsPort: 443

ingress:
  enabled: true
  className: "nginx"
  annotations:
    kubernetes.io/ingress.class: nginx
    cert-manager.io/cluster-issuer: "letsencrypt-prod"
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
  hosts:
    - host: api.todo-app.com
      paths:
        - path: /
          pathType: Prefix
  tls:
    - secretName: todo-api-tls
      hosts:
        - api.todo-app.com

resources:
  limits:
    cpu: 500m
    memory: 512Mi
  requests:
    cpu: 250m
    memory: 256Mi

autoscaling:
  enabled: true
  minReplicas: 2
  maxReplicas: 10
  targetCPUUtilizationPercentage: 80
  targetMemoryUtilizationPercentage: 80

nodeSelector: {}

tolerations: []

affinity: {}

environment:
  ASPNETCORE_ENVIRONMENT: "Production"
  Logging__LogLevel__Default: "Information"
  Logging__LogLevel__Microsoft: "Warning"

secrets:
  connectionString: ""
  jwtSecret: ""

database:
  enabled: true
  type: postgresql
  host: ""
  port: 5432
  name: "tododb"
  username: ""
  password: ""

redis:
  enabled: true
  host: ""
  port: 6379
```

### Deployment Template

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: {{ include "todo-api.fullname" . }}
  labels:
    {{- include "todo-api.labels" . | nindent 4 }}
spec:
  {{- if not .Values.autoscaling.enabled }}
  replicas: {{ .Values.replicaCount }}
  {{- end }}
  selector:
    matchLabels:
      {{- include "todo-api.selectorLabels" . | nindent 6 }}
  template:
    metadata:
      labels:
        {{- include "todo-api.selectorLabels" . | nindent 8 }}
      {{- with .Values.podAnnotations }}
      annotations:
        {{- toYaml . | nindent 8 }}
      {{- end }}
    spec:
      {{- with .Values.imagePullSecrets }}
      imagePullSecrets:
        {{- toYaml . | nindent 8 }}
      {{- end }}
      serviceAccountName: {{ include "todo-api.serviceAccountName" . }}
      securityContext:
        {{- toYaml .Values.podSecurityContext | nindent 8 }}
      containers:
        - name: {{ .Chart.Name }}
          securityContext:
            {{- toYaml .Values.securityContext | nindent 12 }}
          image: "{{ .Values.image.repository }}:{{ .Values.image.tag | default .Chart.AppVersion }}"
          imagePullPolicy: {{ .Values.image.pullPolicy }}
          ports:
            - name: http
              containerPort: 80
              protocol: TCP
            - name: https
              containerPort: 443
              protocol: TCP
          env:
            {{- range $key, $value := .Values.environment }}
            - name: {{ $key }}
              value: "{{ $value }}"
            {{- end }}
            - name: ConnectionStrings__DefaultConnection
              valueFrom:
                secretKeyRef:
                  name: {{ include "todo-api.fullname" $ }}-secrets
                  key: connectionString
          livenessProbe:
            httpGet:
              path: /health
              port: http
          readinessProbe:
            httpGet:
              path: /health
              port: http
          resources:
            {{- toYaml .Values.resources | nindent 12 }}
      {{- with .Values.nodeSelector }}
      nodeSelector:
        {{- toYaml . | nindent 8 }}
      {{- end }}
      {{- with .Values.affinity }}
      affinity:
        {{- toYaml . | nindent 8 }}
      {{- end }}
      {{- with .Values.tolerations }}
      tolerations:
        {{- toYaml . | nindent 8 }}
      {{- end }}
```

## Monitoring and Logging

### Prometheus Metrics

```csharp
// Program.cs
builder.Services.AddMetrics();

// In controllers
[HttpGet("metrics")]
public IActionResult GetMetrics()
{
    var metrics = new
    {
        activeConnections = _hubContext.Clients.All.Count(),
        totalTodos = _context.Todos.Count(),
        completedTodos = _context.Todos.Count(t => t.Status == TodoStatus.Completed)
    };
    return Ok(metrics);
}
```

### Grafana Dashboard

```json
{
  "dashboard": {
    "title": "TODO API Dashboard",
    "panels": [
      {
        "title": "Request Rate",
        "type": "graph",
        "targets": [
          {
            "expr": "rate(http_requests_total[5m])",
            "legendFormat": "{{method}} {{endpoint}}"
          }
        ]
      },
      {
        "title": "Response Time",
        "type": "graph",
        "targets": [
          {
            "expr": "histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))",
            "legendFormat": "95th percentile"
          }
        ]
      }
    ]
  }
}
```

### ELK Stack Configuration

```yaml
# Elasticsearch
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: elasticsearch
spec:
  serviceName: elasticsearch
  replicas: 3
  selector:
    matchLabels:
      app: elasticsearch
  template:
    metadata:
      labels:
        app: elasticsearch
    spec:
      containers:
      - name: elasticsearch
        image: docker.elastic.co/elasticsearch/elasticsearch:8.8.0
        env:
        - name: discovery.type
          value: single-node
        ports:
        - containerPort: 9200
        - containerPort: 9300
        volumeMounts:
        - name: elasticsearch-data
          mountPath: /usr/share/elasticsearch/data
  volumeClaimTemplates:
  - metadata:
      name: elasticsearch-data
    spec:
      accessModes: [ "ReadWriteOnce" ]
      resources:
        requests:
          storage: 10Gi
```

## Security Considerations

### Network Policies

```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: todo-api-network-policy
  namespace: todo-app
spec:
  podSelector:
    matchLabels:
      app: todo-api
  policyTypes:
  - Ingress
  - Egress
  ingress:
  - from:
    - namespaceSelector:
        matchLabels:
          name: ingress-nginx
    ports:
    - protocol: TCP
      port: 80
    - protocol: TCP
      port: 443
  egress:
  - to:
    - namespaceSelector:
        matchLabels:
          name: database
    ports:
    - protocol: TCP
      port: 5432
  - to:
    - namespaceSelector:
        matchLabels:
          name: redis
    ports:
    - protocol: TCP
      port: 6379
```

### Pod Security Standards

```yaml
apiVersion: v1
kind: Pod
metadata:
  name: todo-api-secure
spec:
  securityContext:
    runAsNonRoot: true
    runAsUser: 1000
    fsGroup: 1000
  containers:
  - name: todo-api
    securityContext:
      allowPrivilegeEscalation: false
      readOnlyRootFilesystem: true
      capabilities:
        drop:
        - ALL
    volumeMounts:
    - name: tmp
      mountPath: /tmp
    - name: varlog
      mountPath: /var/log
  volumes:
  - name: tmp
    emptyDir: {}
  - name: varlog
    emptyDir: {}
```

## Infrastructure as Code

### Terraform Configuration

```hcl
# main.tf
terraform {
  required_providers {
    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = "~> 2.0"
    }
    helm = {
      source  = "hashicorp/helm"
      version = "~> 2.0"
    }
  }
}

provider "kubernetes" {
  config_path = "~/.kube/config"
}

provider "helm" {
  kubernetes {
    config_path = "~/.kube/config"
  }
}

# Create namespace
resource "kubernetes_namespace" "todo_app" {
  metadata {
    name = "todo-app"
    labels = {
      name = "todo-app"
    }
  }
}

# Deploy with Helm
resource "helm_release" "todo_api" {
  name       = "todo-api"
  chart      = "./helm-charts/todo-api"
  namespace  = kubernetes_namespace.todo_app.metadata[0].name
  
  set {
    name  = "replicaCount"
    value = "3"
  }
  
  set {
    name  = "image.tag"
    value = var.image_tag
  }
  
  set {
    name  = "environment.ASPNETCORE_ENVIRONMENT"
    value = "Production"
  }
  
  depends_on = [kubernetes_namespace.todo_app]
}
```

## Disaster Recovery

### Backup Strategy

```yaml
# Velero backup configuration
apiVersion: velero.io/v1
kind: Schedule
metadata:
  name: todo-app-backup
  namespace: velero
spec:
  schedule: "0 */6 * * *"  # Every 6 hours
  template:
    includedNamespaces:
    - todo-app
    includedResources:
    - deployments
    - services
    - configmaps
    - secrets
    - persistentvolumeclaims
    storageLocation: default
    volumeSnapshotLocations:
    - default
```

### Recovery Procedures

1. **Database Recovery**
   ```bash
   # Restore database from backup
   kubectl exec -it <postgres-pod> -- pg_restore -d tododb /backup/tododb_backup.sql
   ```

2. **Application Recovery**
   ```bash
   # Restore application from Velero backup
   velero restore create --from-schedule todo-app-backup
   ```

3. **Rollback Procedure**
   ```bash
   # Rollback to previous deployment
   kubectl rollout undo deployment/todo-api
   
   # Or rollback Helm release
   helm rollback todo-api 1
   ```

### High Availability

```yaml
# Horizontal Pod Autoscaler
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: todo-api-hpa
  namespace: todo-app
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: todo-api
  minReplicas: 3
  maxReplicas: 10
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

This DevOps plan provides a comprehensive strategy for deploying, monitoring, and maintaining the TODO API in a production environment with proper CI/CD practices, containerization, orchestration, and disaster recovery procedures. 