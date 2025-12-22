# Aegis Stream Council - Quick Start Guide

## ?? Getting Started in 5 Minutes

### 1. **Build the Project**
```bash
cd ConfluentBot
dotnet build
```

### 2. **Run the Application**
```bash
dotnet run
```

The server will start on `http://localhost:3978`

### 3. **Access the Dashboard**
Open your browser to:
```
http://localhost:3978/index-aegis.html
```

You'll see:
- ?? System health (volatility, virtue, memory)
- ?? Agent status (all showing ACTIVE)
- ?? Transaction stream (empty until you test)
- ?? Fraud indicators (zeroed out initially)

### 4. **Test Fraud Detection**
Click the "? Test Fraud Detection" button in the dashboard.

**What happens**:
1. Random transaction generated
2. Sent to `/api/aegiscouncil/analyze/fraud`
3. DataQualityAgent checks data integrity
4. FraudDetectionAgent analyzes fraud signals
5. StreamHealthAgent monitors system
6. MetaCouncil aggregates virtue profiles
7. Decision returned: BLOCK or ALLOW
8. Dashboard updates with results

**Expected output**:
```json
{
  "transactionId": "TXN_abc123",
  "action": "ALLOW",
  "riskLevel": "LOW",
  "virtueProfile": {
    "compassion": 0.88,
    "integrity": 0.94,
    "courage": 0.92,
    "wisdom": 0.89
  },
  "fraudIndicators": {
    "velocity_score": 0.100,
    "amount_anomaly": 0.250,
    "merchant_anomaly": 0.300,
    "history_score": 0.400
  },
  "processingTimeMs": 23.5
}
```

---

## ?? Run Programmatic Demo

To run all 5 comprehensive scenarios:

```csharp
// In your controller or startup
var demoScenarios = new AegisCouncilDemoScenarios(council, logger);
await demoScenarios.RunAllScenariosAsync();
```

**Scenarios include**:
1. ?? **Impossible Travel**: NYC ? LA in 5 minutes (BLOCKED)
2. ?? **Anomalous Amount**: $15-20 normal, then $5000 spike (FLAGGED)
3. ?? **Batch Analysis**: 5 transactions, mixed results
4. ?? **System Health**: Snapshot creation & regeneration
5. ?? **Stream Integration**: Full council on sensor data

---

## ?? API Quick Reference

### **Single Fraud Analysis**
```bash
curl -X POST http://localhost:3978/api/aegiscouncil/analyze/fraud \
  -H "Content-Type: application/json" \
  -d '{
    "id": "TXN_001",
    "card_id": "CARD_12345",
    "amount": 1500.00,
    "merchant": "Electronics Store",
    "location": "Los Angeles",
    "timestamp": "2024-01-15T10:30:00Z",
    "data_quality": 0.95
  }'
```

### **Batch Fraud Analysis**
```bash
curl -X POST http://localhost:3978/api/aegiscouncil/analyze/fraud-batch \
  -H "Content-Type: application/json" \
  -d '[
    { "id": "TXN_001", "card_id": "CARD_A", "amount": 100, ... },
    { "id": "TXN_002", "card_id": "CARD_B", "amount": 5000, ... }
  ]'
```

### **System Health**
```bash
curl http://localhost:3978/api/aegiscouncil/health
```

Response:
```json
{
  "volatility": 0.125,
  "averageVirtue": 0.847,
  "density": 0.25,
  "totalEntries": 2500,
  "decayedEntries": 312,
  "status": "HEALTHY"
}
```

### **Create Snapshot**
```bash
curl -X POST http://localhost:3978/api/aegiscouncil/snapshot
```

### **Regenerate System**
```bash
curl -X POST http://localhost:3978/api/aegiscouncil/regenerate
```

---

## ?? Key Concepts

### **Virtue Profile**
Instead of a single "0.92 fraud probability", get 4 dimensions:

- **Compassion (0.88)**: Fair decision, avoided false positives
- **Integrity (0.94)**: Data quality was excellent
- **Courage (0.92)**: Confident enough to block
- **Wisdom (0.89)**: Sufficient evidence for decision

### **Fraud Indicators**
Real signals behind the decision:

- **Velocity Score** (0-1): Same card in multiple locations quickly
- **Amount Anomaly** (0-1): Z-score deviation from account average
- **Merchant Anomaly** (0-1): Unknown merchant risk
- **History Score** (0-1): Account age + fraud history

Aggregate = weighted sum. Block if > 0.85.

### **Regenerative Memory**
System automatically:
- **Decays** old entries over time
- **Tracks volatility** (% stale entries)
- **Creates snapshots** when stable
- **Reverts** to snapshots when stressed

Like the immortal jellyfish ??, the system regenerates.

---

## ?? Understanding the Dashboard

### **System Health Card**
- **Volatility**: % of decayed entries (aim for < 0.3)
- **Avg Virtue**: Average quality of stored decisions
- **Memory Entries**: Total items in regenerative memory
- **Status**: HEALTHY (vol < 0.3) | CAUTION (0.3-0.6) | DEGRADED (> 0.6)

### **Last Fraud Analysis Card**
- **Action**: BLOCK or ALLOW
- **Risk Level**: LOW, MEDIUM, HIGH, CRITICAL
- **Virtue Profile**: 4-dimensional breakdown
- **Processing Time**: Latency in ms

### **Fraud Indicators Card**
- **Velocity Score**: Impossible travel risk (0-1)
- **Amount Anomaly**: Unusual amount (0-1)
- **Merchant Anomaly**: Unknown merchant (0-1)
- **History Score**: Account risk (0-1)

### **Recent Transactions**
Live stream of analyzed transactions with:
- Transaction ID
- Card & Amount
- Merchant & Location
- Decision (BLOCK/ALLOW)
- Risk Level

---

## ?? Configuration

### **appsettings.json** (if needed)
```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "GroupId": "ConfluentBot-Consumer-Group",
    "Topics": ["transactions", "events", "metrics"],
    "BufferSize": 100
  },
  "VertexAI": {
    "ProjectId": "your-gcp-project",
    "Location": "us-central1",
    "EndpointId": "your-model-endpoint"
  }
}
```

**Note**: Aegis framework works without Kafka/Vertex AI configured. They're optional for advanced scenarios.

---

## ?? Learning Path

1. **Start**: Click "Test Fraud Detection" on dashboard (30 seconds)
2. **Understand**: Read `AEGIS_FRAMEWORK.md` (5 minutes)
3. **Explore**: Run demo scenarios in code (2 minutes)
4. **Deep Dive**: Review `StreamAgents.cs` + `FraudDetectionAgent.cs` (10 minutes)
5. **Extend**: Add your own agent (see "Extending Aegis" in AEGIS_FRAMEWORK.md)

---

## ?? Production Deployment

### **Docker**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine
WORKDIR /app
COPY bin/Release/net6.0/publish .
ENV ASPNETCORE_URLS=http://+:3978
EXPOSE 3978
ENTRYPOINT ["dotnet", "ConfluentBot.dll"]
```

### **Kubernetes**
```yaml
apiVersion: v1
kind: Service
metadata:
  name: confluentbot-aegis
spec:
  selector:
    app: confluentbot
  ports:
    - protocol: TCP
      port: 3978
      targetPort: 3978
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: confluentbot-deployment
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
      containers:
      - name: confluentbot
        image: confluentbot:latest
        ports:
        - containerPort: 3978
        env:
        - name: Kafka__BootstrapServers
          valueFrom:
            configMapKeyRef:
              name: kafka-config
              key: bootstrap-servers
```

---

## ?? Troubleshooting

### **Dashboard shows "Awaiting transactions..."**
- Click "Test Fraud Detection" button
- Or call `/api/aegiscouncil/analyze/fraud` with test data

### **System Health shows high volatility**
- Click "Create Snapshot" to record stable state
- Click "Regenerate System" to revert to best snapshot

### **Agents not showing ACTIVE**
- Restart application: `dotnet run`
- Check logs for registration errors

### **API returns 400 errors**
- Ensure transaction has required fields: `id`, `card_id`, `amount`, `merchant`, `location`
- Check Content-Type header is `application/json`

---

## ?? Pro Tips

1. **Multiple Snapshots**: System keeps last 10 snapshots. More snapshots = better recovery options.

2. **Virtue Thresholds**: Average virtue of all agents informs regenerative decisions.

3. **Memory Decay**: Change base decay days in `RegenerativeMemory` constructor (default: 30 days).

4. **Parallel Processing**: All agents run in parallel via `Task.WhenAll()` for minimal latency.

5. **Extending**: Add new agents by inheriting from `StreamAgent` and registering in Startup.cs.

---

## ?? Next Steps

- ? Run the dashboard
- ? Test fraud detection
- ? Read AEGIS_FRAMEWORK.md
- ? Review demo scenarios
- ? Deploy to production

**Questions?** Check the logs and AEGIS_FRAMEWORK.md for detailed architecture.

---

**?? You now have a production-grade real-time AI system powered by Confluent Kafka + Google Vertex AI + Aegis Stream Council.**
