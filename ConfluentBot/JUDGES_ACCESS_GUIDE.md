# ?? GITHUB REPOSITORY & TESTING INFORMATION

## GitHub Repository (FOR JUDGES)

**Repository URL**: https://github.com/Raiff1982/ConfluentBot

**Status**: ? Public | ? Ready to clone | ? Production code

---

## ?? For Hackathon Judges

### **Option 1: Test Locally** (Recommended for deep testing)

```bash
# 1. Clone repository
git clone https://github.com/Raiff1982/ConfluentBot.git

# 2. Navigate to project
cd ConfluentBot/ConfluentBot

# 3. Build (requires .NET 8.0 SDK)
dotnet build

# 4. Run
dotnet run

# 5. Open in browser
http://localhost:3978/demo

# 6. Click demo scenarios
# - Benign transaction ? APPROVE
# - Suspicious transaction ? BLOCK
# - Ambiguous transaction ? REVIEW
```

**Requirements**: .NET 8.0 SDK installed
**Time to run**: 2-3 minutes after clone

---

### **Option 2: Test Live** (When hosted)

Once deployed (see DEPLOYMENT_GUIDE.md):

```
https://confluentbot.azurewebsites.net/demo
(or chosen deployment platform)
```

---

## ?? What Judges Will See

### **Interactive Dashboard**
- 5 pre-built fraud detection scenarios
- Real-time analysis display
- Complete reasoning chain (14+ frameworks)
- Virtue profile scoring
- Fraud decision: APPROVE | REVIEW | BLOCK

### **API Endpoints Available**
```
POST /api/fraudDemo/analyze
GET  /api/fraudDemo/scenarios
GET  /api/fraudDemo/health
```

---

## ?? Demo Walkthrough (5 Minutes)

1. **Home Page** (30 seconds)
   - "Welcome to NexisAegisCodetteFusion"
   - 5 scenario buttons visible

2. **Benign Case** (1 minute)
   - Click: "$49.99 Amazon.com Books"
   - See: ? APPROVE (95% confidence)
   - Explain: Low amount, trusted merchant, ethical alignment

3. **Suspicious Case** (1 minute)
   - Click: "$15,000 Cryptocurrency Exchange"
   - See: ?? BLOCK (88% confidence)
   - Explain: High amount, suspicious merchant, multiple risk flags

4. **Ambiguous Case** (1.5 minutes)
   - Click: "$2,500 Unknown Electronics Ltd"
   - See: ?? REVIEW (65% confidence)
   - Explain: Mixed signals, system escalates to human

5. **Innovation** (1 minute)
   - Explain: "15+ frameworks converging on each decision"
   - Show: Reasoning chain with all framework contributions
   - Highlight: Google Vertex AI + custom ethical frameworks

---

## ?? Repository Structure

```
ConfluentBot/
??? Controllers/
?   ??? FraudDemoController.cs          (REST API endpoints)
??? Services/
?   ??? NexisIntegration/
?   ?   ??? NexisSignalAgent.cs         (3 Nexis perspectives)
?   ?   ??? NexisAegisCodetteFusion.cs  (15+ framework orchestration)
?   ?   ??? VertexAIFraudAnalyzer.cs    (Google Vertex AI)
?   ??? AegisMemory/
?   ?   ??? RegenerativeMemory.cs       (Virtue-based memory)
?   ?   ??? AegisStreamCouncil.cs       (Multi-agent decisions)
?   ??? KafkaConsumerService.cs         (Real-time streaming)
?   ??? VertexAIPredictionService.cs    (Cloud predictions)
??? Models/
?   ??? StreamingModels.cs              (Data structures)
??? wwwroot/
?   ??? demo.html                       (Interactive dashboard)
??? appsettings.json                    (Configuration)
??? Startup.cs                          (DI & middleware)
??? ConfluentBot.csproj                 (Dependencies)
```

---

## ?? Key Files for Judges

### **To Understand the Architecture**
- Read: `COMPLETE_TECH_STACK.md` (What tools/frameworks)
- Read: `Services/NexisIntegration/NexisAegisCodetteFusion.cs` (How it works)
- Read: `wwwroot/demo.html` (Interactive demo)

### **To See the Code**
- `Services/NexisIntegration/NexisSignalAgent.cs` - 270+ LOC
- `Services/NexisIntegration/NexisAegisCodetteFusion.cs` - 200+ LOC
- `Controllers/FraudDemoController.cs` - 250+ LOC
- `Services/NexisIntegration/VertexAIFraudAnalyzer.cs` - 170+ LOC

### **To Test the API**
```bash
# Once running locally, test with:

# Analyze a transaction
curl -X POST http://localhost:3978/api/fraudDemo/analyze \
  -H "Content-Type: application/json" \
  -d '{
    "id": "txn-001",
    "amount": 49.99,
    "merchant": "Amazon.com",
    "category": "Books"
  }'

# Get demo scenarios
curl http://localhost:3978/api/fraudDemo/scenarios

# Health check
curl http://localhost:3978/api/fraudDemo/health
```

---

## ?? What Makes This Project Stand Out

? **15+ Converging Frameworks**
- 3 Nexis perspectives (ethical reasoning)
- 9 Codette frameworks (multi-logic)
- 1 Google Vertex AI (ML-based)
- Multi-agent Aegis (consensus)

? **Real-Time Streaming**
- Confluent Kafka integration
- Production-ready pipeline
- High-throughput, low-latency

? **100% Explainable**
- Every framework visible
- Complete reasoning chain
- Virtue profile transparency

? **Production Grade**
- Error handling
- Structured logging
- Graceful degradation
- Async/await throughout

---

## ? Build & Test Status

| Check | Status |
|-------|--------|
| **Code Compiles** | ? SUCCESS |
| **No Errors** | ? 0 errors |
| **Build Passes** | ? YES |
| **Runs Locally** | ? YES |
| **Demo Works** | ? YES |
| **API Responsive** | ? YES |
| **Data Real** | ? YES |

---

## ?? For Judges: Quick Access

1. **See the Code**: https://github.com/Raiff1982/ConfluentBot
2. **Clone Locally**: `git clone https://github.com/Raiff1982/ConfluentBot`
3. **Run Demo**: `dotnet run` then `http://localhost:3978/demo`
4. **Test API**: POST to `/api/fraudDemo/analyze`
5. **Read Docs**: Check repo for COMPLETE_TECH_STACK.md, deployment guides

---

## ?? Deployment Options

If you want to provide judges with a **live URL** (not local):

| Platform | Setup Time | Cost | URL Pattern |
|----------|-----------|------|-------------|
| **Heroku** | 5 min | $7/mo | https://app-name.herokuapp.com/demo |
| **Azure** | 15 min | $10-50/mo | https://app-name.azurewebsites.net/demo |
| **Google Cloud Run** | 10 min | Free tier | https://app-name-xxxxx.run.app/demo |

**See**: DEPLOYMENT_GUIDE.md for setup instructions

---

## ?? For Judges Who Need Help

If judges encounter issues running locally:

1. **Ensure .NET 8.0 installed**: `dotnet --version`
2. **Clone fresh**: `git clone https://github.com/Raiff1982/ConfluentBot`
3. **Clean build**: `dotnet clean` then `dotnet build`
4. **Run**: `dotnet run`
5. **Default port**: http://localhost:3978

If issues persist:
- Check `appsettings.json` configuration
- Verify port 3978 not in use
- Check firewall settings

---

## ?? Summary for Judges

**GitHub Repository**: https://github.com/Raiff1982/ConfluentBot

**To Test**:
```bash
git clone https://github.com/Raiff1982/ConfluentBot
cd ConfluentBot/ConfluentBot
dotnet build
dotnet run
# Visit: http://localhost:3978/demo
```

**Project**: NexisAegisCodetteFusion Fraud Detection
**Innovation**: 15+ converging frameworks
**Status**: Production-ready, tested, documented

---

**Ready for hackathon judges!** ??
