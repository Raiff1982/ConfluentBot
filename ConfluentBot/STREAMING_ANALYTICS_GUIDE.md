# ConfluentBot: Real-Time AI on Data in Motion

## Overview

ConfluentBot is a next-generation AI application that leverages Confluent Kafka for real-time data streaming and Google Cloud Vertex AI for advanced predictions. It demonstrates how to process continuous data streams with machine learning models to generate intelligent predictions and detect anomalies in real time.

## Architecture

The system is built on a modular, layered architecture:

```
???????????????????????????????????????????????????????????????
?                   Bot Framework Layer                        ?
?              (User Interaction & Dialogs)                    ?
?         Main Dialog, Stream Analytics Dialog                 ?
???????????????????????????????????????????????????????????????
                         ?
???????????????????????????????????????????????????????????????
?               Service Integration Layer                      ?
?    Kafka Consumer | Stream Processing | Telemetry           ?
?         REST API Controllers                                ?
???????????????????????????????????????????????????????????????
                         ?
???????????????????????????????????????????????????????????????
?              Stream Processing Pipeline                      ?
?   Feature Extraction ? Enrichment ? AI Prediction           ?
???????????????????????????????????????????????????????????????
                         ?
???????????????????????????????????????????????????????????????
?   ???????????????????????     ????????????????????????????  ?
?   ?  Confluent Kafka    ?     ?  Google Cloud Vertex AI  ?  ?
?   ?  Real-Time Streams  ?     ?  AI/ML Predictions       ?  ?
?   ???????????????????????     ????????????????????????????  ?
??????????????????????????????????????????????????????????????
```

## Components

### 1. **KafkaConsumerService**
Manages real-time data consumption from Confluent Kafka topics.

**Features:**
- Subscribes to multiple topics simultaneously
- Maintains efficient message buffers
- Provides event-driven subscription model
- Collects topic statistics and health metrics

**Usage:**
```csharp
var messages = await kafkaConsumer.GetLatestMessagesAsync<Dictionary<string, object>>("transactions", 10);
kafkaConsumer.SubscribeToTopic("events", async (message) => {
    await ProcessMessageAsync(message);
});
```

### 2. **VertexAIPredictionService**
Interfaces with Google Cloud Vertex AI for real-time predictions.

**Features:**
- Online prediction for real-time queries
- Batch prediction for multiple instances
- Automatic feature type conversion
- Confidence score extraction

**Usage:**
```csharp
var features = new Dictionary<string, object> { { "amount", 1000 }, { "category", "purchase" } };
var prediction = await vertexAI.PredictAsync(features);
```

### 3. **StreamProcessingPipeline**
Orchestrates the complete data processing workflow.

**Stages:**
1. **Feature Extraction**: Parses JSON payload and extracts relevant fields
2. **Enrichment**: Adds contextual metadata (topic, partition, offset, processing delay)
3. **Prediction**: Calls Vertex AI model with enriched features
4. **Metrics**: Records performance metrics

**Usage:**
```csharp
var streamItem = new StreamItem { Topic = "transactions", Payload = jsonData };
var result = await pipeline.ProcessAsync(streamItem);
```

### 4. **StreamTelemetry**
Collects and analyzes performance metrics across the system.

**Metrics Collected:**
- Message throughput and latency (p50, p75, p90, p95, p99)
- Prediction accuracy and confidence distribution
- Error rates and success rates
- System health status
- Historical trends

**Usage:**
```csharp
telemetry.RecordMessage("transactions", 12.5, true);
var metrics = telemetry.GetMetrics("transactions");
var health = telemetry.GetSystemHealth();
```

## Configuration

### appsettings.json
```json
{
  "Kafka": {
    "BootstrapServers": "kafka-broker:9092",
    "GroupId": "ConfluentBot-Consumer-Group",
    "Topics": ["transactions", "events", "metrics"],
    "BufferSize": 100
  },
  "VertexAI": {
    "ProjectId": "your-gcp-project-id",
    "Location": "us-central1",
    "EndpointId": "your-endpoint-id"
  }
}
```

### Environment Setup

#### Kafka Configuration
1. Set up a Confluent Kafka cluster or use Confluent Cloud
2. Create topics: `transactions`, `events`, `metrics`, `anomalies`
3. Update `BootstrapServers` in configuration

#### Google Cloud Setup
1. Create a Google Cloud project
2. Enable Vertex AI API
3. Deploy a model endpoint
4. Set `ProjectId`, `Location`, and `EndpointId` in configuration
5. Configure authentication (Application Default Credentials or service account key)

## API Endpoints

### Stream Analytics Endpoints

#### Get Latest Messages
```
GET /api/streamanalytics/messages/{topic}?count=10
```
Returns the latest messages from a Kafka topic.

#### Check Topic Health
```
GET /api/streamanalytics/health/{topic}
```
Returns statistics and health metrics for a topic.

#### Get AI Predictions
```
POST /api/streamanalytics/predict
Content-Type: application/json

{
  "topic": "transactions",
  "analysisType": "prediction",
  "parameters": {
    "batch_size": 5
  }
}
```
Processes stream items and returns AI predictions.

#### System Health
```
GET /api/streamanalytics/system-health
```
Returns overall system health and aggregate metrics.

#### Topic Metrics
```
GET /api/streamanalytics/metrics/{topic}?durationSeconds=300
```
Returns historical metrics and percentile data.

#### Pipeline Metrics
```
GET /api/streamanalytics/pipeline-metrics
```
Returns stream processing pipeline performance metrics.

## Bot Dialogs

### StreamAnalyticsDialog
Interactive dialog for querying stream data and getting predictions.

**Features:**
- Topic selection (transactions, events, metrics, anomalies)
- Analysis type selection (Latest Messages, Health Status, Predictions, Anomalies)
- Real-time analysis execution
- Formatted result presentation

**Example Conversation:**
```
User: "Analyze stream data"
Bot: "Which data stream would you like to analyze?"
Bot: [Transactions] [Events] [Metrics] [Anomalies]
User: "Transactions"
Bot: "What type of analysis would you like?"
Bot: [Show latest messages] [Check health] [Get predictions] [Detect anomalies]
User: "Get predictions"
Bot: "?? AI Predictions for 'transactions' stream:
     • Event: payment_processed
       Prediction: fraudulent
       Confidence: 0.92
     ..."
```

## Real-World Use Cases

### 1. **Fraud Detection**
- Stream credit card transactions through Kafka
- Use Vertex AI model trained on historical fraud patterns
- Get real-time fraud scores and alerts
- Immediately block high-risk transactions

### 2. **Supply Chain Optimization**
- Ingest IoT sensor data from shipments
- Predict delivery delays and equipment failures
- Anomaly detection for unusual temperature/humidity
- Optimize routing in real time

### 3. **Predictive Maintenance**
- Stream equipment metrics (temperature, vibration, power)
- Predict component failures before they occur
- Detect maintenance needs automatically
- Schedule preventive maintenance

### 4. **Customer Behavior Analysis**
- Stream clickstream and purchase events
- Predict churn probability in real time
- Detect unusual purchase patterns
- Recommend interventions (discounts, support)

### 5. **Network Anomaly Detection**
- Ingest network traffic and security events
- Detect potential intrusions and DDoS attacks
- Identify anomalous data exfiltration
- Alert security teams in real time

## Performance Metrics

The system tracks comprehensive metrics:

- **Throughput**: Messages processed per second
- **Latency**: p50, p75, p90, p95, p99 percentiles
- **Accuracy**: Prediction success rate and confidence distribution
- **Error Rate**: Pipeline failure percentage
- **Health Status**: HEALTHY, DEGRADED, SLOW, CRITICAL

## Extensibility

### Adding New Streams
1. Add topic name to `appsettings.json` Kafka.Topics array
2. Messages are automatically consumed and buffered

### Custom Feature Extraction
Extend `StreamProcessingPipeline.ExtractFeatures()` to add domain-specific logic:
```csharp
private Dictionary<string, object> ExtractFeatures(StreamItem item)
{
    var features = base_extraction(item);
    // Add custom features
    features["custom_metric"] = ComputeCustomMetric(item);
    return features;
}
```

### Anomaly Detection Rules
Extend `StreamAnalyticsDialog.GetAnomaliesActivity()` with statistical methods:
- Z-score detection
- Isolation Forest
- Autoencoders
- DBSCAN clustering

## Security Considerations

1. **Kafka Security**:
   - Use SASL/SSL for Kafka connection
   - Implement ACLs for topic access
   - Rotate credentials regularly

2. **GCP Authentication**:
   - Use service accounts with minimal permissions
   - Store credentials in secure vaults
   - Use Workload Identity for Kubernetes deployments

3. **Data Privacy**:
   - Implement data masking for PII
   - Audit feature usage
   - Comply with GDPR/CCPA requirements

4. **API Security**:
   - Implement authentication and authorization
   - Rate limit sensitive endpoints
   - Log and monitor access

## Monitoring and Observability

The system exports structured logs and metrics:

```csharp
_logger.LogInformation(
    "Pipeline processed {Count} items in {Duration}ms with {SuccessRate}% success rate",
    metrics.TotalCount,
    metrics.AverageProcessingTimeMs,
    metrics.SuccessRate
);
```

## Deployment

### Docker Deployment
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY bin/Release/net6.0/publish .
ENTRYPOINT ["dotnet", "ConfluentBot.dll"]
```

### Kubernetes Deployment
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: confluentbot
spec:
  replicas: 3
  template:
    spec:
      containers:
      - name: confluentbot
        image: confluentbot:latest
        env:
        - name: Kafka__BootstrapServers
          valueFrom:
            configMapKeyRef:
              name: kafka-config
              key: bootstrap-servers
        - name: VertexAI__ProjectId
          valueFrom:
            secretKeyRef:
              name: gcp-credentials
              key: project-id
```

## Future Enhancements

1. **Advanced Analytics**:
   - Time-series forecasting
   - Cluster analysis
   - Causal inference

2. **MLOps Integration**:
   - Model versioning and deployment
   - A/B testing framework
   - Automated retraining pipelines

3. **Streaming SQL**:
   - Kafka Streams topology
   - Complex event processing (CEP)
   - State stores for aggregate queries

4. **Enhanced Visualization**:
   - Real-time dashboards
   - Prediction confidence graphs
   - Anomaly trend analysis

## References

- [Confluent Kafka Documentation](https://docs.confluent.io/)
- [Google Cloud Vertex AI](https://cloud.google.com/vertex-ai/docs)
- [Microsoft Bot Framework](https://docs.microsoft.com/en-us/azure/bot-service/)
- [.NET 6 Documentation](https://docs.microsoft.com/en-us/dotnet/core/whats-new/dotnet-6)

## Support

For issues, feature requests, or contributions, please refer to the project repository.
