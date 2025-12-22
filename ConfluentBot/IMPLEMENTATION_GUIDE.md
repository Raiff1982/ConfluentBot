# ConfluentBot Implementation Guide

## Table of Contents
1. [System Architecture](#system-architecture)
2. [Component Overview](#component-overview)
3. [Data Flow](#data-flow)
4. [Configuration Guide](#configuration-guide)
5. [Deployment Strategies](#deployment-strategies)
6. [Monitoring and Observability](#monitoring-and-observability)
7. [Troubleshooting](#troubleshooting)
8. [Performance Tuning](#performance-tuning)

## System Architecture

### Layered Design

ConfluentBot implements a clean, layered architecture separating concerns:

```
??????????????????????????????????????????????
?         Presentation Layer                 ?
?  (Bot Dialogs, REST Controllers)           ?
????????????????????????????????????????????
                 ?
????????????????????????????????????????????
?      Application Layer                    ?
?  (Business Logic, Orchestration)          ?
?  - StreamAnalyticsDialog                  ?
?  - StreamAnalyticsController              ?
????????????????????????????????????????????
                 ?
????????????????????????????????????????????
?      Service Layer                        ?
?  (Integration & Processing)               ?
?  - StreamProcessingPipeline               ?
?  - IStreamTelemetry                       ?
?  - IKafkaConsumerService                  ?
?  - IVertexAIPredictionService             ?
????????????????????????????????????????????
                 ?
????????????????????????????????????????????
?      Data Layer                           ?
?  (External Systems)                       ?
?  - Confluent Kafka                        ?
?  - Google Cloud Vertex AI                 ?
??????????????????????????????????????????
```

### Dependency Injection

The system uses ASP.NET Core's built-in DI container configured in `Startup.cs`:

```csharp
services.AddSingleton<IKafkaConsumerService, KafkaConsumerService>();
services.AddSingleton<IVertexAIPredictionService, VertexAIPredictionService>();
services.AddSingleton<IStreamProcessingPipeline, StreamProcessingPipeline>();
services.AddSingleton<IStreamTelemetry, StreamTelemetry>();
```

This ensures:
- **Loose coupling** between components
- **Easy testing** with mock implementations
- **Centralized configuration** management
- **Lifecycle management** of resources

## Component Overview

### 1. KafkaConsumerService

**Purpose**: Manage real-time data consumption from Confluent Kafka

**Key Responsibilities**:
- Subscribe to multiple topics
- Buffer messages in memory (configurable size)
- Provide event-driven subscription callbacks
- Track topic statistics and health

**Configuration**:
```json
{
  "Kafka": {
    "BootstrapServers": "kafka-broker:9092",
    "GroupId": "ConfluentBot-Consumer-Group",
    "Topics": ["transactions", "events", "metrics", "anomalies"],
    "BufferSize": 100
  }
}
```

**Usage Pattern**:
```csharp
// Get latest messages
var messages = await kafkaConsumer.GetLatestMessagesAsync<MyType>("topic-name", 10);

// Subscribe to topic
kafkaConsumer.SubscribeToTopic("topic-name", async (message) => {
    await ProcessAsync(message);
});

// Get statistics
var stats = await kafkaConsumer.GetTopicStatisticsAsync("topic-name");
```

### 2. VertexAIPredictionService

**Purpose**: Interface with Google Cloud Vertex AI for ML predictions

**Key Responsibilities**:
- Make online predictions (single instance)
- Make batch predictions (multiple instances)
- Handle feature type conversion
- Extract confidence scores

**Configuration**:
```json
{
  "VertexAI": {
    "ProjectId": "my-gcp-project",
    "Location": "us-central1",
    "EndpointId": "1234567890"
  }
}
```

**Authentication**:
```bash
# Using Application Default Credentials
gcloud auth application-default login

# Or using service account key
export GOOGLE_APPLICATION_CREDENTIALS=/path/to/key.json
```

**Usage Pattern**:
```csharp
var features = new Dictionary<string, object>
{
    { "amount", 1000 },
    { "category", "purchase" },
    { "country", "US" }
};

var result = await vertexAI.PredictAsync(features);
if (result.Success)
{
    var confidence = result.Confidence;
    var prediction = result.Prediction;
}
```

### 3. StreamProcessingPipeline

**Purpose**: Orchestrate the complete data processing workflow

**Four Processing Stages**:

1. **Feature Extraction**
   - Parse JSON payload
   - Extract relevant fields
   - Handle malformed data gracefully

2. **Enrichment**
   - Add metadata (topic, partition, offset)
   - Calculate derived features (processing delay)
   - Add timestamps

3. **Prediction**
   - Call Vertex AI with enriched features
   - Handle prediction failures
   - Record confidence scores

4. **Metrics**
   - Track processing duration
   - Record success/failure
   - Update pipeline metrics

**Processing Time Breakdown**:
```
Total ~35ms
?? Feature Extraction: ~2ms
?? Enrichment: ~3ms
?? Vertex AI Call: ~25ms
?? Metrics Recording: ~5ms
```

**Usage Pattern**:
```csharp
var streamItem = new StreamItem
{
    Topic = "transactions",
    Payload = jsonString,
    Partition = 0,
    Offset = 12345
};

var result = await pipeline.ProcessAsync(streamItem);
// result contains enriched features, prediction, metrics
```

### 4. StreamTelemetry

**Purpose**: Collect and aggregate system metrics

**Metrics Categories**:

| Category | Metrics | Use Case |
|----------|---------|----------|
| **Throughput** | Messages/sec, predictions/sec | Capacity planning |
| **Latency** | p50, p75, p90, p95, p99 | Performance SLAs |
| **Accuracy** | Success rate, error rate | Model quality |
| **Confidence** | Distribution, average | Prediction reliability |
| **Health** | System status, error trends | Alerting |

**Usage Pattern**:
```csharp
// Record metrics
telemetry.RecordMessage("topic", 25.5, success: true);
telemetry.RecordPrediction("topic", 0.95, success: true);

// Get current metrics
var metrics = telemetry.GetMetrics("topic");

// Get system health
var health = telemetry.GetSystemHealth();

// Get historical data
var historical = telemetry.GetHistoricalMetrics("topic", 300);
```

## Data Flow

### Request Flow Example

```
1. User Query
   ?
2. StreamAnalyticsDialog
   - Prompt for topic
   - Prompt for analysis type
   - Confirm selection
   ?
3. StreamAnalyticsController (or Dialog)
   - Call appropriate service
   ?
4. Service Layer
   a) KafkaConsumerService
      - Query message buffer
      - Return recent messages
   
   b) StreamProcessingPipeline (for predictions)
      - Extract features from messages
      - Call VertexAI with features
      - Enrich with metadata
      - Return predictions
   
   c) StreamTelemetry
      - Record metrics
      - Calculate percentiles
      - Determine health status
   ?
5. Format Response
   - Convert to JSON
   - Add metadata
   - Return with timestamps
   ?
6. Display to User
   - Formatted message
   - Metrics visualization
   - Emoji indicators
```

### Message Buffer Lifecycle

```
Kafka Topic
   ?
[Consumed by Consumer Thread]
   ?
Message Buffer (Queue, max 100)
   ?? Oldest messages dequeued when full
   ?? New messages enqueued
   ?
Available for:
- Latest message queries
- Prediction pipeline
- Subscriber callbacks
   ?
Telemetry Recording
```

## Configuration Guide

### Environment Variables

```bash
# Kafka Configuration
export Kafka__BootstrapServers=kafka:9092
export Kafka__GroupId=ConfluentBot
export Kafka__Topics__0=transactions
export Kafka__Topics__1=events
export Kafka__BufferSize=100

# Vertex AI Configuration
export VertexAI__ProjectId=my-project
export VertexAI__Location=us-central1
export VertexAI__EndpointId=1234567890

# GCP Authentication
export GOOGLE_APPLICATION_CREDENTIALS=/etc/secrets/gcp-key.json
```

### Production Configuration

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "Kafka": {
    "BootstrapServers": "kafka-prod-1:9092,kafka-prod-2:9092,kafka-prod-3:9092",
    "GroupId": "ConfluentBot-Production",
    "Topics": ["transactions", "events", "metrics", "anomalies"],
    "BufferSize": 500,
    "SecurityProtocol": "SaslSsl",
    "SaslMechanism": "Plain",
    "SaslUsername": "${KAFKA_USER}",
    "SaslPassword": "${KAFKA_PASSWORD}"
  },
  "VertexAI": {
    "ProjectId": "production-project-id",
    "Location": "us-central1",
    "EndpointId": "9876543210"
  }
}
```

## Deployment Strategies

### Development

```bash
# Local development with docker-compose
docker-compose up -d

# Run application
dotnet run

# Application available at http://localhost:3978
```

### Docker Container

```dockerfile
# Multi-stage build
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS builder
WORKDIR /build
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine
WORKDIR /app
COPY --from=builder /app .
ENV ASPNETCORE_URLS=http://+:3978
EXPOSE 3978
ENTRYPOINT ["dotnet", "ConfluentBot.dll"]
```

```bash
docker build -t confluentbot:latest .
docker run \
  -e Kafka__BootstrapServers=kafka:9092 \
  -e GOOGLE_APPLICATION_CREDENTIALS=/secrets/gcp-key.json \
  -v /path/to/gcp-key.json:/secrets/gcp-key.json \
  -p 3978:3978 \
  confluentbot:latest
```

### Kubernetes Deployment

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: confluentbot-config
data:
  appsettings.json: |
    {
      "Kafka": {
        "BootstrapServers": "kafka-cluster:9092",
        "Topics": ["transactions", "events", "metrics"]
      }
    }

---
apiVersion: v1
kind: Secret
metadata:
  name: gcp-credentials
type: Opaque
stringData:
  service-account-key.json: |
    {
      "type": "service_account",
      "project_id": "...",
      ...
    }

---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: confluentbot
spec:
  replicas: 3
  selector:
    matchLabels:
      app: confluentbot
  template:
    metadata:
      labels:
        app: confluentbot
    spec:
      serviceAccountName: confluentbot
      containers:
      - name: confluentbot
        image: confluentbot:latest
        ports:
        - containerPort: 3978
        env:
        - name: GOOGLE_APPLICATION_CREDENTIALS
          value: /etc/secrets/gcp/service-account-key.json
        envFrom:
        - configMapRef:
            name: confluentbot-config
        volumeMounts:
        - name: gcp-credentials
          mountPath: /etc/secrets/gcp
          readOnly: true
        resources:
          requests:
            memory: "512Mi"
            cpu: "250m"
          limits:
            memory: "1Gi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 3978
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /ready
            port: 3978
          initialDelaySeconds: 10
          periodSeconds: 5
      volumes:
      - name: gcp-credentials
        secret:
          secretName: gcp-credentials

---
apiVersion: v1
kind: Service
metadata:
  name: confluentbot-service
spec:
  type: LoadBalancer
  selector:
    app: confluentbot
  ports:
  - protocol: TCP
    port: 80
    targetPort: 3978
```

## Monitoring and Observability

### Health Checks

Implement health endpoints for Kubernetes:

```csharp
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/ready", async (IStreamTelemetry telemetry) =>
{
    var health = telemetry.GetSystemHealth();
    return health.Status == "CRITICAL" 
        ? Results.StatusCode(503)
        : Results.Ok(health);
});
```

### Logging

```csharp
_logger.LogInformation(
    "Pipeline processed {ItemCount} items with {SuccessRate}% success",
    itemCount,
    successRate
);

_logger.LogError(
    "Failed to predict for item {ItemId}: {Error}",
    itemId,
    errorMessage
);
```

### Metrics Export

```csharp
// Export to Prometheus format
app.MapGet("/metrics", (IStreamTelemetry telemetry) =>
{
    var health = telemetry.GetSystemHealth();
    var metrics = $@"
# TYPE confluentbot_messages_total counter
confluentbot_messages_total {health.TotalMessagesProcessed}

# TYPE confluentbot_predictions_total counter
confluentbot_predictions_total {health.TotalPredictionsMade}

# TYPE confluentbot_errors_total counter
confluentbot_errors_total {health.TotalErrors}

# TYPE confluentbot_latency_ms gauge
confluentbot_latency_ms {health.AverageLatencyMs}

# TYPE confluentbot_confidence gauge
confluentbot_confidence {health.AverageConfidence}
";
    return Results.Text(metrics, "text/plain");
});
```

## Troubleshooting

### No Messages from Kafka

**Symptoms**: `GetLatestMessagesAsync` returns empty list

**Diagnosis**:
```csharp
var stats = await kafkaConsumer.GetTopicStatisticsAsync("topic-name");
if (stats.MessageCount == 0)
{
    // Check topic exists
    // Check consumer group subscribed
    // Check messages being published
}
```

**Solutions**:
1. Verify Kafka broker connectivity
2. Confirm topic exists and has data
3. Check consumer group offset
4. Review Kafka logs for errors

### Vertex AI Prediction Failures

**Symptoms**: `prediction.Success == false`

**Diagnosis**:
```csharp
if (!result.Success)
{
    _logger.LogError($"Prediction failed: {result.Error}");
    // Check error message
}
```

**Solutions**:
1. Verify GCP authentication credentials
2. Check Vertex AI endpoint ID is correct
3. Validate feature types match model input
4. Ensure endpoint is deployed and healthy
5. Check quota limits on GCP

### High Latency

**Symptoms**: Processing times > 100ms

**Diagnosis**:
```csharp
var metrics = pipeline.GetMetrics();
if (metrics.AverageProcessingTimeMs > 100)
{
    _logger.LogWarning(
        "High latency: p95={P95}ms, p99={P99}ms",
        metrics.Max,
        metrics.Max // worst case
    );
}
```

**Solutions**:
1. Profile individual stages (extraction, prediction, etc.)
2. Reduce feature extraction complexity
3. Optimize Vertex AI model (lighter model)
4. Increase number of consumer instances
5. Review network latency to GCP

## Performance Tuning

### Buffer Size Optimization

```json
{
  "Kafka": {
    "BufferSize": 100  // Adjust based on:
                       // - Available memory
                       // - Message frequency
                       // - Query patterns
  }
}
```

**Guidelines**:
- **High-frequency topics**: Use larger buffer (500+)
- **Memory-constrained**: Use smaller buffer (50)
- **Archive/analysis**: Use larger buffer for historical queries

### Feature Extraction

```csharp
private Dictionary<string, object> ExtractFeatures(StreamItem item)
{
    // Profile this method
    var sw = Stopwatch.StartNew();
    
    var features = JsonConvert.DeserializeObject<Dictionary<string, object>>(item.Payload);
    
    // Add only necessary features
    // Remove expensive computations
    
    sw.Stop();
    if (sw.ElapsedMilliseconds > 10)
    {
        _logger.LogWarning("Slow extraction: {Ms}ms", sw.ElapsedMilliseconds);
    }
    
    return features;
}
```

### Prediction Batching

```csharp
// Instead of individual predictions
var predictions = new List<PredictionResult>();
foreach (var item in items)
{
    predictions.Add(await vertexAI.PredictAsync(item));
}

// Use batch predictions
var results = await vertexAI.BatchPredictAsync(items);
```

### Consumer Group Scaling

For production:
```
1 topic with 4 partitions ? 4 consumer instances max
Each instance processes 1 partition ? No contention
Throughput = (4 partitions × message_rate_per_partition)
```

---

This implementation guide provides the foundation for operating ConfluentBot in production environments. Refer to specific component documentation for deeper technical details.
