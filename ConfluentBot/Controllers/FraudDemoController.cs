using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ConfluentBot.Services.AegisMemory;
using ConfluentBot.Services.NexisIntegration;

namespace ConfluentBot.Controllers
{
    /// <summary>
    /// Fraud Detection Demo API - Interactive demonstration of NexisAegisCodetteFusion
    /// Provides endpoints for real-time fraud detection analysis with full explainability
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class FraudDemoController : ControllerBase
    {
        private readonly RegenerativeMemory _memory;
        private readonly ILogger<FraudDemoController> _logger;
        private readonly ILoggerFactory _loggerFactory;

        public FraudDemoController(
            RegenerativeMemory memory,
            ILogger<FraudDemoController> logger,
            ILoggerFactory loggerFactory)
        {
            _memory = memory;
            _logger = logger;
            _loggerFactory = loggerFactory;
        }

        /// <summary>
        /// Analyze a transaction and return explainable fraud detection verdict
        /// </summary>
        [HttpPost("analyze")]
        public async Task<ActionResult<FraudAnalysisResponse>> AnalyzeTransaction(
            [FromBody] TransactionInput transaction)
        {
            try
            {
                if (transaction == null || string.IsNullOrEmpty(transaction.Id))
                {
                    return BadRequest(new { error = "Transaction ID is required" });
                }

                var transactionDict = new Dictionary<string, object>
                {
                    { "id", transaction.Id },
                    { "amount", transaction.Amount },
                    { "merchant", transaction.Merchant ?? "unknown" },
                    { "category", transaction.Category ?? "unknown" }
                };

                _logger.LogInformation($"Analyzing transaction: {transaction.Id}");

                // Create NexisAegisCodetteFusion instance with logger factory
                var fusionLogger = _loggerFactory.CreateLogger<NexisAegisCodetteFusion>();
                var fusion = new NexisAegisCodetteFusion(_memory, fusionLogger, _loggerFactory);
                var result = await fusion.AnalyzeTransactionAsync(transactionDict);

                var response = new FraudAnalysisResponse
                {
                    TransactionId = result.TransactionId,
                    Decision = new DecisionSummary
                    {
                        Action = result.Decision.Action,
                        FraudScore = result.Decision.FraudScore,
                        Confidence = result.Decision.Confidence,
                        Message = result.Decision.Message,
                        Reasons = result.Decision.SupportingReasons
                    },
                    Analysis = new AnalysisDetails
                    {
                        NexisFindings = result.NexisFindings,
                        CodetteReasoning = result.CodetteReasoning,
                        AegisVirtues = result.AegisVirtues,
                        ReasoningChain = new ReasoningChainResponse
                        {
                            Steps = result.ReasoningChain.Steps.Select((s, idx) => new ReasoningStepResponse
                            {
                                Sequence = idx + 1,
                                Framework = s.Framework,
                                Finding = s.Finding,
                                Weight = $"{s.Weight:P0}",
                                Rationale = s.Rationale
                            }).ToList(),
                            Summary = result.ReasoningChain.Summary,
                            Confidence = result.ReasoningChain.Confidence
                        }
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing transaction");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get demo scenarios with expected outputs
        /// </summary>
        [HttpGet("scenarios")]
        public ActionResult<ScenariosResponse> GetScenarios()
        {
            var scenarios = new ScenariosResponse
            {
                Scenarios = new List<ScenarioDefinition>
                {
                    new ScenarioDefinition
                    {
                        Id = 1,
                        Title = "Benign E-Commerce Purchase",
                        Description = "Normal online shopping transaction - low risk",
                        Transaction = new TransactionInput
                        {
                            Id = "demo-benign-001",
                            Amount = 49.99,
                            Merchant = "Amazon.com",
                            Category = "Books"
                        },
                        ExpectedVerdict = "APPROVE",
                        ExpectedConfidence = 0.95,
                        Explanation = "Established merchant, normal amount, low fraud risk. All frameworks agree."
                    },
                    new ScenarioDefinition
                    {
                        Id = 2,
                        Title = "Suspicious International Transfer",
                        Description = "Large amount to unknown international merchant with risk keywords",
                        Transaction = new TransactionInput
                        {
                            Id = "demo-suspicious-002",
                            Amount = 15000.00,
                            Merchant = "Cryptocurrency Exchange",
                            Category = "Risky Asset"
                        },
                        ExpectedVerdict = "BLOCK",
                        ExpectedConfidence = 0.88,
                        Explanation = "High amount, unfamiliar merchant, risk terms detected. Multiple frameworks flag concerns."
                    },
                    new ScenarioDefinition
                    {
                        Id = 3,
                        Title = "Ambiguous Transaction",
                        Description = "Moderate amount to unknown merchant - mixed signals",
                        Transaction = new TransactionInput
                        {
                            Id = "demo-ambiguous-003",
                            Amount = 2500.00,
                            Merchant = "Unknown Electronics Ltd",
                            Category = "Electronics"
                        },
                        ExpectedVerdict = "REVIEW",
                        ExpectedConfidence = 0.65,
                        Explanation = "Moderate fraud risk, frameworks split. Recommends human verification."
                    },
                    new ScenarioDefinition
                    {
                        Id = 4,
                        Title = "Trusted Vendor Subscription",
                        Description = "Recurring transaction with established streaming service",
                        Transaction = new TransactionInput
                        {
                            Id = "demo-trusted-004",
                            Amount = 14.99,
                            Merchant = "Netflix",
                            Category = "Subscription"
                        },
                        ExpectedVerdict = "APPROVE",
                        ExpectedConfidence = 0.97,
                        Explanation = "Established vendor, low amount, recurring pattern. High virtue alignment."
                    },
                    new ScenarioDefinition
                    {
                        Id = 5,
                        Title = "Mid-Range Unknown Merchant",
                        Description = "Moderate amount to unfamiliar but legitimate-sounding business",
                        Transaction = new TransactionInput
                        {
                            Id = "demo-moderate-005",
                            Amount = 275.50,
                            Merchant = "Tech Supplies Inc",
                            Category = "Electronics"
                        },
                        ExpectedVerdict = "REVIEW",
                        ExpectedConfidence = 0.72,
                        Explanation = "Unknown vendor requires verification. Moderate fraud risk profile."
                    }
                }
            };

            return Ok(scenarios);
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        [HttpGet("health")]
        public ActionResult<HealthResponse> HealthCheck()
        {
            return Ok(new HealthResponse
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0",
                Framework = "NexisAegisCodetteFusion",
                Frameworks = 14,
                LatencyMs = 40
            });
        }
    }

    /// <summary>
    /// Request Models
    /// </summary>
    public class TransactionInput
    {
        public string Id { get; set; }
        public double Amount { get; set; }
        public string Merchant { get; set; }
        public string Category { get; set; }
    }

    /// <summary>
    /// Response Models
    /// </summary>
    public class FraudAnalysisResponse
    {
        public string TransactionId { get; set; }
        public DecisionSummary Decision { get; set; }
        public AnalysisDetails Analysis { get; set; }
    }

    public class DecisionSummary
    {
        public string Action { get; set; }
        public double FraudScore { get; set; }
        public double Confidence { get; set; }
        public string Message { get; set; }
        public List<string> Reasons { get; set; }
    }

    public class AnalysisDetails
    {
        public Dictionary<string, object> NexisFindings { get; set; }
        public Dictionary<string, object> CodetteReasoning { get; set; }
        public object AegisVirtues { get; set; }
        public ReasoningChainResponse ReasoningChain { get; set; }
    }

    public class ReasoningChainResponse
    {
        public List<ReasoningStepResponse> Steps { get; set; }
        public string Summary { get; set; }
        public double Confidence { get; set; }
    }

    public class ReasoningStepResponse
    {
        public int Sequence { get; set; }
        public string Framework { get; set; }
        public string Finding { get; set; }
        public string Weight { get; set; }
        public string Rationale { get; set; }
    }

    public class ScenariosResponse
    {
        public List<ScenarioDefinition> Scenarios { get; set; }
    }

    public class ScenarioDefinition
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public TransactionInput Transaction { get; set; }
        public string ExpectedVerdict { get; set; }
        public double ExpectedConfidence { get; set; }
        public string Explanation { get; set; }
    }

    public class HealthResponse
    {
        public string Status { get; set; }
        public DateTime Timestamp { get; set; }
        public string Version { get; set; }
        public string Framework { get; set; }
        public int Frameworks { get; set; }
        public int LatencyMs { get; set; }
    }
}
