# ?? How to Use NexisAegisCodetteFusion in Your Code

## Quick Start

### 1. Inject the Fusion Service

```csharp
// In Startup.cs or Program.cs
services.AddScoped<NexisIntegration.NexisAegisCodetteFusion>();
```

### 2. Use in Your Controller or Service

```csharp
[HttpPost("analyze")]
public async Task<IActionResult> AnalyzeTransaction([FromBody] Dictionary<string, object> transaction)
{
    var result = await _fusion.AnalyzeTransactionAsync(transaction);
    
    return Ok(new
    {
        decision = result.Decision.Action,
        fraudScore = result.Decision.FraudScore,
        confidence = result.Decision.Confidence,
        reasoning = result.ReasoningChain.Summary,
        nextisAnalysis = result.NexisFindings,
        codetteAnalysis = result.CodetteReasoning,
        virtueProfile = result.AegisVirtues
    });
}
```

### 3. Real Transaction Example

**Input**:
```json
{
  "id": "txn-12345",
  "amount": 1500.00,
  "merchant": "Amazon.com",
  "category": "Electronics",
  "timestamp": "2025-12-22T10:30:00Z"
}
```

**Output**:
```json
{
  "decision": "APPROVE",
  "fraudScore": 0.128,
  "confidence": 0.92,
  "reasoning": "Fusion analysis: APPROVE (Fraud: 12.8%, Confidence: 92%). Processed through 14 frameworks.",
  "nextisAnalysis": {
    "suspicion_score": 0.2,
    "entropy_index": 0.08,
    "ethical_alignment": "aligned",
    "harmonic_volatility": 0.075,
    "pre_corruption_risk": "low",
    "colleen_vector": "[0.42, 0.58]",
    "luke_ethics": "stabilized",
    "kellyanne_harmonics": "[0.23, 0.18, 0.31]"
  },
  "codetteAnalysis": {
    "Neural": "Neural: $1500.00 to Amazon.com shows moderate risk pattern.",
    "Newtonian": "Newtonian: Category 'Electronics' has moderate inherent risk.",
    "DaVinci": "Da Vinci: 'Amazon.com' in 'Electronics' represents intersection of commerce and ethics.",
    "Kindness": "Kindness: All parties deserve consideration of honest intent before judgment.",
    "Quantum": "Quantum: Probability of fraudulent intent approximately 35%.",
    "Philosophy": "Philosophy: What obligations exist to protect while enabling commerce?",
    "Mathematics": "Mathematics: Amount $1500.00 at 15% of distribution.",
    "Symbolic": "Symbolic: Merchant->Category trust assessment based on known patterns.",
    "Systems": "Systems: Transaction exists in ecosystem. Holistic evaluation required."
  },
  "virtueProfile": {
    "integrity": 0.92,
    "compassion": 0.88,
    "courage": 0.925,
    "wisdom": 0.90
  }
}
```

---

## Key Features You Can Use

### 1. Access the Fraud Score

```csharp
double fraudScore = result.Decision.FraudScore;
if (fraudScore > 0.7)
{
    // High fraud risk - block transaction
    await _paymentService.BlockTransaction(transaction["id"]);
}
```

### 2. Check the Reasoning Chain

```csharp
foreach (var step in result.ReasoningChain.Steps)
{
    Console.WriteLine($"{step.Framework}: {step.Finding}");
    Console.WriteLine($"  Weight: {step.Weight:P0}");
    Console.WriteLine($"  Rationale: {step.Rationale}");
}
```

### 3. Evaluate Virtue Profile

```csharp
var virtue = result.AegisVirtues;
var ethicalScore = (virtue.Integrity + virtue.Wisdom + virtue.Compassion) / 3.0;

if (ethicalScore > 0.85)
{
    // High ethical alignment - approve faster
    return ApproveImmediately();
}
```

### 4. Handle Ambiguous Cases

```csharp
if (result.Decision.Action == "REVIEW")
{
    // Escalate to human review
    await _reviewQueue.EnqueueAsync(new
    {
        TransactionId = result.TransactionId,
        FraudScore = result.Decision.FraudScore,
        Confidence = result.Decision.Confidence,
        ReasoningChain = result.ReasoningChain.Summary,
        Reasons = result.Decision.SupportingReasons
    });
}
```

### 5. Log Complete Analysis

```csharp
_logger.LogInformation($"Transaction {result.TransactionId}:");
_logger.LogInformation($"  Decision: {result.Decision.Action}");
_logger.LogInformation($"  Fraud Score: {result.Decision.FraudScore}");
_logger.LogInformation($"  Confidence: {result.Decision.Confidence}");
_logger.LogInformation($"  Message: {result.Decision.Message}");
foreach (var reason in result.Decision.SupportingReasons)
{
    _logger.LogInformation($"    - {reason}");
}
```

---

## Integration with Kafka

### Stream Transactions Through Fusion

```csharp
// Kafka consumer loop
while (true)
{
    var message = _kafkaConsumer.Consume(TimeSpan.FromSeconds(1));
    
    if (message == null) continue;
    
    // Parse transaction
    var transaction = JsonSerializer.Deserialize<Dictionary<string, object>>(
        message.Message.Value);
    
    // Analyze through Fusion
    var result = await _fusion.AnalyzeTransactionAsync(transaction);
    
    // Store result
    await _fraudDb.SaveAnalysisAsync(new
    {
        TransactionId = result.TransactionId,
        Decision = result.Decision.Action,
        FraudScore = result.Decision.FraudScore,
        Timestamp = DateTime.UtcNow
    });
    
    // Publish decision
    await _kafkaProducer.ProduceAsync("fraud-decisions", new Message<string, string>
    {
        Key = result.TransactionId,
        Value = JsonSerializer.Serialize(result.Decision)
    });
}
```

---

## Advanced Usage

### Custom Decision Logic

```csharp
public async Task<string> MakeDecisionWithContext(
    NexisAegisCodetteFusion.FusionAnalysisResult fusion,
    CustomerProfile customer)
{
    // Override based on customer history
    if (customer.TrustScore > 0.95)
    {
        return "APPROVE"; // Whitelist trusted customers
    }
    
    // Override based on amount thresholds
    var amount = /* extract from transaction */;
    if (amount > 50000 && fusion.Decision.Confidence < 0.8)
    {
        return "REVIEW"; // Manual review for large, uncertain transactions
    }
    
    // Use fusion decision
    return fusion.Decision.Action;
}
```

### Batch Processing

```csharp
var transactions = await GetPendingTransactionsAsync();
var results = new List<NexisAegisCodetteFusion.FusionAnalysisResult>();

foreach (var txn in transactions)
{
    var result = await _fusion.AnalyzeTransactionAsync(txn);
    results.Add(result);
}

// Analyze patterns
var blockCount = results.Count(r => r.Decision.Action == "BLOCK");
var avgFraudScore = results.Average(r => r.Decision.FraudScore);

Console.WriteLine($"Processed {results.Count} transactions");
Console.WriteLine($"Blocked: {blockCount}");
Console.WriteLine($"Average Fraud Score: {avgFraudScore:P1}");
```

### Store Analysis History

```csharp
public class FraudAnalysisRecord
{
    public string TransactionId { get; set; }
    public DateTime AnalysisTime { get; set; }
    public double FraudScore { get; set; }
    public double Confidence { get; set; }
    public string Decision { get; set; }
    public string ReasoningChain { get; set; }
    public Dictionary<string, object> NexisFindings { get; set; }
    public Dictionary<string, object> CodetteAnalysis { get; set; }
}

// Save to database
await _analysisDb.InsertAsync(new FraudAnalysisRecord
{
    TransactionId = result.TransactionId,
    AnalysisTime = DateTime.UtcNow,
    FraudScore = result.Decision.FraudScore,
    Confidence = result.Decision.Confidence,
    Decision = result.Decision.Action,
    ReasoningChain = result.ReasoningChain.Summary,
    NexisFindings = result.NexisFindings,
    CodetteAnalysis = result.CodetteReasoning
});
```

---

## Performance Considerations

### Typical Latency

```
Nexis Analysis:      ~20ms
Codette Synthesis:   ~10ms
Virtue Calculation:  ~5ms
Verdict Generation:  ~3ms
?????????????????????????????
Total:               ~40ms per transaction
```

### Scaling to High Throughput

```csharp
// Use parallel processing for batch analysis
var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

Parallel.ForEach(transactions, options, async txn =>
{
    var result = await _fusion.AnalyzeTransactionAsync(txn);
    await _resultsQueue.EnqueueAsync(result);
});
```

---

## Error Handling

```csharp
try
{
    var result = await _fusion.AnalyzeTransactionAsync(transaction);
    return Ok(result);
}
catch (ArgumentNullException ex)
{
    _logger.LogError($"Invalid transaction: {ex.Message}");
    return BadRequest("Transaction data missing required fields");
}
catch (InvalidOperationException ex)
{
    _logger.LogError($"Analysis failed: {ex.Message}");
    return StatusCode(500, "Analysis engine error");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error in fusion analysis");
    return StatusCode(500, "Internal server error");
}
```

---

## Example: Complete Fraud Detection Service

```csharp
[ApiController]
[Route("api/fraud")]
public class FraudDetectionController : ControllerBase
{
    private readonly NexisAegisCodetteFusion _fusion;
    private readonly IFraudDatabase _db;
    private readonly ILogger<FraudDetectionController> _logger;

    public FraudDetectionController(
        NexisAegisCodetteFusion fusion,
        IFraudDatabase db,
        ILogger<FraudDetectionController> logger)
    {
        _fusion = fusion;
        _db = db;
        _logger = logger;
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> AnalyzeTransaction(
        [FromBody] Dictionary<string, object> transaction)
    {
        _logger.LogInformation($"Analyzing transaction {transaction["id"]}");
        
        var result = await _fusion.AnalyzeTransactionAsync(transaction);
        
        // Store analysis
        await _db.SaveAnalysisAsync(result);
        
        // Determine action
        var response = new
        {
            transactionId = result.TransactionId,
            decision = result.Decision.Action,
            fraudScore = result.Decision.FraudScore,
            confidence = result.Decision.Confidence,
            message = result.Decision.Message,
            reasons = result.Decision.SupportingReasons,
            analysis = new
            {
                nexis = result.NexisFindings,
                codette = result.CodetteReasoning,
                virtue = result.AegisVirtues,
                reasoning = result.ReasoningChain.Summary
            }
        };
        
        return Ok(response);
    }

    [HttpGet("history/{transactionId}")]
    public async Task<IActionResult> GetAnalysisHistory(string transactionId)
    {
        var analyses = await _db.GetAnalysesAsync(transactionId);
        return Ok(analyses);
    }
}
```

---

## Summary

The NexisAegisCodetteFusion service is designed to be:
- ? **Easy to use** - Simple async API
- ? **Flexible** - Works with any transaction data
- ? **Transparent** - Complete reasoning chain included
- ? **Scalable** - Handles high throughput with Kafka
- ? **Explainable** - Every decision fully justified
- ? **Ethical** - Virtue-based confidence scoring

**Use it anywhere you need fraud detection with explainability and ethical considerations built-in.**

Good luck with your hackathon submission! ??
