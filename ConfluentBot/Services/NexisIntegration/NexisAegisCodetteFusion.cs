using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ConfluentBot.Services.AegisMemory;

namespace ConfluentBot.Services.NexisIntegration
{
    /// <summary>
    /// NexisAegisCodetteFusion: The Ultimate Intelligence Framework
    /// </summary>
    public class NexisAegisCodetteFusion
    {
        private readonly RegenerativeMemory _memory;
        private readonly ILogger<NexisAegisCodetteFusion> _logger;
        private readonly NexisSignalAgent _nexisAgent;
        private readonly CodetteSynthesizer _codette;

        public class FusionAnalysisResult
        {
            public string TransactionId { get; set; }
            public Dictionary<string, object> NexisFindings { get; set; }
            public Dictionary<string, object> CodetteReasoning { get; set; }
            public VirtueProfile AegisVirtues { get; set; }
            public FinalVerdict Decision { get; set; }
            public ExplainableChain ReasoningChain { get; set; }
        }

        public class ExplainableChain
        {
            public List<ReasoningStep> Steps { get; set; } = new();
            public string Summary { get; set; }
            public double Confidence { get; set; }
        }

        public class ReasoningStep
        {
            public string Framework { get; set; }
            public string Finding { get; set; }
            public double Weight { get; set; }
            public string Rationale { get; set; }
        }

        public class FinalVerdict
        {
            public string Action { get; set; }
            public double FraudScore { get; set; }
            public double Confidence { get; set; }
            public string Message { get; set; }
            public List<string> SupportingReasons { get; set; }
        }

        public NexisAegisCodetteFusion(RegenerativeMemory memory, ILogger<NexisAegisCodetteFusion> logger, ILoggerFactory loggerFactory = null)
        {
            _memory = memory;
            _logger = logger;
            _nexisAgent = new NexisSignalAgent(memory, logger);
            
            // Create Vertex AI with proper logger type using factory
            VertexAIFraudAnalyzer vertexAI = null;
            if (loggerFactory != null)
            {
                var vertexLogger = loggerFactory.CreateLogger<VertexAIFraudAnalyzer>();
                vertexAI = new VertexAIFraudAnalyzer(vertexLogger);
            }
            
            _codette = new CodetteSynthesizer(vertexAI);
        }

        public async Task<FusionAnalysisResult> AnalyzeTransactionAsync(Dictionary<string, object> transaction)
        {
            var transactionId = transaction.TryGetValue("id", out var id) ? id.ToString() : Guid.NewGuid().ToString();
            
            _logger.LogInformation($"[FUSION] Starting analysis for transaction: {transactionId}");

            var result = new FusionAnalysisResult
            {
                TransactionId = transactionId,
                NexisFindings = new(),
                CodetteReasoning = new(),
                ReasoningChain = new ExplainableChain { Steps = new() }
            };

            // Wrap transaction for Nexis analysis (expects payload format)
            var nexisInput = new Dictionary<string, object>
            {
                { "topic", "transaction" },
                { "payload", transaction }
            };

            // Run Nexis analysis
            var nexisResult = await _nexisAgent.AnalyzeAsync(nexisInput);
            result.NexisFindings = nexisResult.Data;
            result.AegisVirtues = nexisResult.VirtueProfile;

            // Extract Nexis insights
            var suspicionScore = GetDoubleValue(nexisResult.Data, "suspicion_score", 0.0);
            var entropyIndex = GetDoubleValue(nexisResult.Data, "entropy_index", 0.0);
            var ethicalAlignment = GetStringValue(nexisResult.Data, "ethical_alignment", "unknown");
            var preCorruptionRisk = GetStringValue(nexisResult.Data, "pre_corruption_risk", "unknown");

            // Add Nexis steps to reasoning chain
            result.ReasoningChain.Steps.Add(new ReasoningStep
            {
                Framework = "NEXIS_COLLEEN",
                Finding = GetStringValue(nexisResult.Data, "colleen_vector", "N/A"),
                Weight = 0.15,
                Rationale = "Analyzes signal pattern transformation in abstract space"
            });

            result.ReasoningChain.Steps.Add(new ReasoningStep
            {
                Framework = "NEXIS_LUKE",
                Finding = $"Ethical: {ethicalAlignment}, Entropy: {entropyIndex:F2}",
                Weight = 0.20,
                Rationale = "Evaluates ethical intent and system stability"
            });

            result.ReasoningChain.Steps.Add(new ReasoningStep
            {
                Framework = "NEXIS_KELLYANNE",
                Finding = GetStringValue(nexisResult.Data, "kellyanne_harmonics", "N/A"),
                Weight = 0.15,
                Rationale = "Detects pattern resonance and anomalies"
            });

            // Run Codette synthesis (now with async support for Vertex AI)
            var codetteAnalysis = await _codette.SynthesizeReasoningAsync(transaction);
            result.CodetteReasoning = codetteAnalysis;

            // Add Codette frameworks
            foreach (var framework in new[] { "Neural", "Newtonian", "DaVinci", "Quantum", "Philosophy", "Mathematics", "Symbolic", "Systems", "Kindness", "VertexAI" })
            {
                if (codetteAnalysis.TryGetValue(framework, out var finding))
                {
                    var weight = framework == "VertexAI" ? 0.10 : 0.08; // Vertex AI gets 10% weight
                    result.ReasoningChain.Steps.Add(new ReasoningStep
                    {
                        Framework = $"CODETTE_{framework.ToUpper()}",
                        Finding = finding.ToString(),
                        Weight = weight,
                        Rationale = $"Applies {framework} logic perspective"
                    });
                }
            }

            // Compute virtue-weighted verdict
            var virtue = result.AegisVirtues;
            var virtueScore = (virtue.Integrity + virtue.Wisdom + virtue.Courage + virtue.Compassion) / 4.0;

            result.ReasoningChain.Steps.Add(new ReasoningStep
            {
                Framework = "AEGIS_VIRTUE",
                Finding = $"Integrity: {virtue.Integrity:F2}, Compass: {virtue.Compassion:F2}, Courage: {virtue.Courage:F2}, Wisdom: {virtue.Wisdom:F2}",
                Weight = 0.27,
                Rationale = "Virtue-based confidence scoring for trust"
            });

            // Calculate fraud score
            var fraudScore = CalculateFraudScore(suspicionScore, entropyIndex, virtueScore, ethicalAlignment == "aligned");
            var confidenceFactors = new List<double>
            {
                1.0 - suspicionScore,
                1.0 - entropyIndex,
                virtueScore,
                ethicalAlignment == "aligned" ? 0.9 : 0.5
            };
            var confidence = confidenceFactors.Average();

            // Determine action
            string action;
            string message;
            var supportingReasons = new List<string>();

            if (fraudScore > 0.7)
            {
                action = "BLOCK";
                message = "High fraud risk detected across multiple perspectives";
                supportingReasons.Add($"Suspicion: {suspicionScore:F2}");
                supportingReasons.Add($"Entropy: {entropyIndex:F2}");
            }
            else if (fraudScore > 0.4 || preCorruptionRisk == "high")
            {
                action = "REVIEW";
                message = "Recommend manual verification before approval";
                supportingReasons.Add($"Risk: {preCorruptionRisk}");
                supportingReasons.Add("Ambiguous signals require human judgment");
            }
            else if (virtueScore > 0.8 && ethicalAlignment == "aligned")
            {
                action = "APPROVE";
                message = "Transaction aligns with ethical and virtue profiles";
                supportingReasons.Add("High virtue alignment");
                supportingReasons.Add($"Ethics: {ethicalAlignment}");
            }
            else
            {
                action = "REVIEW";
                message = "Marginal case - recommend verification";
                supportingReasons.Add("Falls between clear thresholds");
            }

            result.Decision = new FinalVerdict
            {
                Action = action,
                FraudScore = Math.Round(fraudScore, 3),
                Confidence = Math.Round(confidence, 3),
                Message = message,
                SupportingReasons = supportingReasons
            };

            result.ReasoningChain.Confidence = confidence;
            result.ReasoningChain.Summary = BuildSummary(result);

            _logger.LogInformation($"[FUSION] Complete: {action} (Fraud: {fraudScore:F2}, Conf: {confidence:F2})");

            return result;
        }

        private double CalculateFraudScore(double suspicion, double entropy, double virtue, bool ethical)
        {
            var baseScore = (suspicion + entropy) / 2.0;
            var virtueAdjustment = (1.0 - virtue) * 0.3;
            var ethicalAdjustment = ethical ? -0.15 : 0.15;
            
            return Math.Max(0, Math.Min(1.0, baseScore + virtueAdjustment + ethicalAdjustment));
        }

        private string BuildSummary(FusionAnalysisResult result)
        {
            return $"Fusion analysis: {result.Decision.Action} " +
                   $"(Fraud: {result.Decision.FraudScore:P1}, Conf: {result.Decision.Confidence:P1}). " +
                   $"Processed through {result.ReasoningChain.Steps.Count} frameworks.";
        }

        private double GetDoubleValue(Dictionary<string, object> dict, string key, double defaultValue)
        {
            if (dict.TryGetValue(key, out var value))
            {
                if (double.TryParse(value.ToString(), out var result))
                    return result;
            }
            return defaultValue;
        }

        private string GetStringValue(Dictionary<string, object> dict, string key, string defaultValue)
        {
            return dict.TryGetValue(key, out var value) ? value.ToString() : defaultValue;
        }
    }

    public class CodetteSynthesizer
    {
        private readonly VertexAIFraudAnalyzer _vertexAI;

        public CodetteSynthesizer(VertexAIFraudAnalyzer vertexAI = null)
        {
            _vertexAI = vertexAI;
        }

        public async Task<Dictionary<string, object>> SynthesizeReasoningAsync(Dictionary<string, object> transaction)
        {
            var results = new Dictionary<string, object>();
            var amount = GetDoubleValue(transaction, "amount", 0.0);
            var merchant = GetStringValue(transaction, "merchant", "unknown");
            var category = GetStringValue(transaction, "category", "unknown");

            // Determine transaction risk profile
            var isTrustedMerchant = IsTrustedMerchant(merchant);
            var isSuspiciousMerchant = IsSuspiciousMerchant(merchant);
            var isHighAmount = amount > 5000;
            var isSmallAmount = amount < 100;

            results["Neural"] = NeuralNetworkPerspective(amount, merchant, isTrustedMerchant, isSuspiciousMerchant);
            results["Newtonian"] = NewtonianLogic(amount, category, isHighAmount);
            results["DaVinci"] = DaVinciSynthesis(merchant, category, isTrustedMerchant);
            results["Kindness"] = ResilientKindness(isTrustedMerchant);
            results["Quantum"] = QuantumLogic(amount, merchant, isSuspiciousMerchant);
            results["Philosophy"] = PhilosophicalInquiry(amount, isTrustedMerchant);
            results["Mathematics"] = MathematicalRigor(amount, isHighAmount);
            results["Symbolic"] = SymbolicReasoning(merchant, category, isTrustedMerchant);
            results["Systems"] = SystemsThinking(amount, isTrustedMerchant, isSuspiciousMerchant);
            
            // Add Vertex AI as 10th framework
            if (_vertexAI != null)
            {
                var vertexResult = await _vertexAI.AnalyzeFraudRiskAsync(transaction);
                results["VertexAI"] = VertexAIFramework(vertexResult, amount, merchant);
            }

            return results;
        }

        private string VertexAIFramework(VertexAIFraudAnalyzer.VertexAIResult vertexResult, double amount, string merchant)
        {
            var source = vertexResult.IsAvailable ? "Vertex AI ML Model" : "Vertex AI Fallback";
            return $"{source}: Fraud likelihood {vertexResult.FraudLikelihood:P0}, Risk={vertexResult.RiskLevel}. {vertexResult.Rationale}";
        }

        private bool IsTrustedMerchant(string merchant)
        {
            var trusted = new[] { "amazon", "netflix", "apple", "microsoft", "google", "paypal", "walmart", "target" };
            var lowerMerchant = merchant.ToLower();
            return trusted.Any(t => lowerMerchant.Contains(t));
        }

        private bool IsSuspiciousMerchant(string merchant)
        {
            var suspicious = new[] { "crypto", "exchange", "anonymous", "offshore", "darknet", "untraceable" };
            var lowerMerchant = merchant.ToLower();
            return suspicious.Any(s => lowerMerchant.Contains(s));
        }

        private string NeuralNetworkPerspective(double amount, string merchant, bool trusted, bool suspicious)
        {
            if (trusted)
                return $"Neural: ${amount:F2} to trusted {merchant} - LOW RISK pattern recognized.";
            if (suspicious)
                return $"Neural: ${amount:F2} to {merchant} - HIGH RISK anomaly detected.";
            if (amount > 5000)
                return $"Neural: ${amount:F2} transaction - ELEVATED risk due to amount.";
            if (amount < 100)
                return $"Neural: ${amount:F2} micro-transaction - LOW RISK pattern.";
            return $"Neural: ${amount:F2} to {merchant} - MODERATE risk baseline.";
        }

        private string NewtonianLogic(double amount, string category, bool highAmount)
        {
            var categoryRisk = category.ToLower() switch
            {
                "electronics" => "moderate",
                "luxury" => "high",
                "essentials" => "low",
                "subscription" => "very low",
                "books" => "very low",
                "crypto" => "extreme",
                _ => "neutral"
            };

            if (highAmount)
                return $"Newtonian: Category '{category}' + ${amount:F2} amount = {categoryRisk} ? ELEVATED risk.";
            return $"Newtonian: Category '{category}' has {categoryRisk} risk profile.";
        }

        private string DaVinciSynthesis(string merchant, string category, bool trusted)
        {
            if (trusted)
                return $"Da Vinci: '{merchant}' in '{category}' - ETHICAL ALIGNMENT confirmed (known merchant).";
            return $"Da Vinci: '{merchant}' in '{category}' - UNKNOWN merchant requires scrutiny.";
        }

        private string ResilientKindness(bool trusted)
        {
            if (trusted)
                return "Kindness: Established merchant history suggests good-faith intent.";
            return "Kindness: Benefit of doubt given, but caution warranted for unknown vendors.";
        }

        private string QuantumLogic(double amount, string merchant, bool suspicious)
        {
            if (suspicious)
                return $"Quantum: High probability (>80%) of fraudulent intent detected.";
            if (amount > 5000)
                return $"Quantum: Probability of fraudulent intent approximately 55%.";
            if (amount < 100)
                return $"Quantum: Probability of fraudulent intent <10% (low-value transaction).";
            return $"Quantum: Probability of fraudulent intent approximately 35%.";
        }

        private string PhilosophicalInquiry(double amount, bool trusted)
        {
            if (trusted)
                return $"Philosophy: Trusted vendors ? Commerce enabled. Transaction permits prosperity.";
            if (amount > 5000)
                return $"Philosophy: Large sums demand heightened scrutiny. Protection vs. enablement tension.";
            return $"Philosophy: Balance commerce freedom with consumer protection principles.";
        }

        private string MathematicalRigor(double amount, bool highAmount)
        {
            var percentile = Math.Min(amount / 10000.0, 1.0);
            if (highAmount)
                return $"Mathematics: ${amount:F2} at {percentile:P0} of distribution = OUTLIER (watch for fraud).";
            return $"Mathematics: ${amount:F2} at {percentile:P0} of normal distribution.";
        }

        private string SymbolicReasoning(string merchant, string category, bool trusted)
        {
            if (trusted)
                return $"Symbolic: {merchant} ? [trusted] ? ? validate and approve.";
            if (category.ToLower().Contains("crypto"))
                return $"Symbolic: crypto ? [high-risk] ? ? scrutinize heavily.";
            return $"Symbolic: {merchant} ? [known] ? ? requires verification.";
        }

        private string SystemsThinking(double amount, bool trusted, bool suspicious)
        {
            if (trusted && amount < 100)
                return $"Systems: Low-value + trusted vendor = stable ecosystem transaction.";
            if (suspicious && amount > 5000)
                return $"Systems: High-value + suspicious vendor = system destabilization risk.";
            if (trusted)
                return $"Systems: Trusted merchant maintains ecosystem integrity.";
            return $"Systems: Unknown vendor introduces uncertainty into transaction ecosystem.";
        }

        private double GetDoubleValue(Dictionary<string, object> dict, string key, double defaultValue)
        {
            if (dict.TryGetValue(key, out var value))
            {
                if (double.TryParse(value.ToString(), out var result))
                    return result;
            }
            return defaultValue;
        }

        private string GetStringValue(Dictionary<string, object> dict, string key, string defaultValue)
        {
            return dict.TryGetValue(key, out var value) ? value.ToString() : defaultValue;
        }
    }
}
