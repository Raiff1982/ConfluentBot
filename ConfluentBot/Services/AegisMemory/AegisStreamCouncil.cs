using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ConfluentBot.Services.AegisMemory
{
    /// <summary>
    /// High-level orchestrator for the Aegis stream council.
    /// Manages parallel agent execution and council decision-making.
    /// </summary>
    public class AegisStreamCouncil
    {
        private readonly RegenerativeMemory _memory;
        private readonly DataQualityAgent _qualityAgent;
        private readonly TrendAgent _trendAgent;
        private readonly StreamHealthAgent _healthAgent;
        private readonly FraudDetectionAgent _fraudAgent;
        private readonly MetaCouncil _metaCouncil;
        private readonly ILogger<AegisStreamCouncil> _logger;

        public AegisStreamCouncil(
            RegenerativeMemory memory,
            DataQualityAgent qualityAgent,
            TrendAgent trendAgent,
            StreamHealthAgent healthAgent,
            FraudDetectionAgent fraudAgent,
            MetaCouncil metaCouncil,
            ILogger<AegisStreamCouncil> logger)
        {
            _memory = memory;
            _qualityAgent = qualityAgent;
            _trendAgent = trendAgent;
            _healthAgent = healthAgent;
            _fraudAgent = fraudAgent;
            _metaCouncil = metaCouncil;
            _logger = logger;
        }

        /// <summary>
        /// Run the full Aegis council cycle on stream data:
        /// 1. Parallel agent analysis
        /// 2. MetaCouncil synthesis
        /// 3. Regenerative cycle decision
        /// </summary>
        public async Task<CouncilDecision> AnalyzeStreamAsync(
            Dictionary<string, object> streamData,
            string topic = "unknown")
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // Add topic to input for agents
                streamData["topic"] = topic;

                // Phase 1: Parallel agent analysis
                _logger.LogInformation($"[AegisCouncil] Dispatching agents for topic={topic}");

                var agentTasks = new List<Task<AgentResult>>
                {
                    _qualityAgent.AnalyzeAsync(streamData),
                    _trendAgent.AnalyzeAsync(streamData),
                    _healthAgent.AnalyzeAsync(streamData)
                };

                var agentResults = await Task.WhenAll(agentTasks);

                // Phase 2: MetaCouncil synthesis
                _metaCouncil.SetAgentResults(agentResults.ToList());
                var councilResult = await _metaCouncil.MakeDecisionAsync();

                // Phase 3: Get decision action from regenerative cycle
                var decision = ExtractCouncilDecision(councilResults: agentResults, councilResult);
                decision.ProcessingTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds;

                _logger.LogInformation(
                    $"[AegisCouncil] Cycle complete: action={decision.Action} " +
                    $"virtue={decision.AggregateVirtue.Average:F2} " +
                    $"duration={decision.ProcessingTimeMs:F1}ms");

                return decision;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[AegisCouncil] Error during analysis: {ex.Message}");
                return new CouncilDecision
                {
                    Action = "ERROR",
                    Error = ex.Message,
                    ProcessingTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }
        }

        /// <summary>
        /// Specialized fraud analysis using the FraudDetectionAgent.
        /// </summary>
        public async Task<FraudAnalysisDecision> AnalyzeFraudAsync(Dictionary<string, object> transactionData)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInformation("[AegisCouncil] Fraud analysis initiated");

                // Run fraud agent + basic quality check
                var fraudTask = _fraudAgent.AnalyzeAsync(transactionData);
                var qualityTask = _qualityAgent.AnalyzeAsync(transactionData);

                var fraudResult = await fraudTask;
                var qualityResult = await qualityTask;

                // Combine virtue profiles
                var combinedVirtue = new VirtueProfile
                {
                    Compassion = (fraudResult.VirtueProfile.Compassion + qualityResult.VirtueProfile.Compassion) / 2.0,
                    Integrity = (fraudResult.VirtueProfile.Integrity + qualityResult.VirtueProfile.Integrity) / 2.0,
                    Courage = (fraudResult.VirtueProfile.Courage + qualityResult.VirtueProfile.Courage) / 2.0,
                    Wisdom = (fraudResult.VirtueProfile.Wisdom + qualityResult.VirtueProfile.Wisdom) / 2.0
                };

                var action = fraudResult.Data.ContainsKey("action") 
                    ? fraudResult.Data["action"].ToString() ?? "UNKNOWN"
                    : "ALLOW";

                var decision = new FraudAnalysisDecision
                {
                    TransactionId = transactionData.TryGetValue("id", out var id) ? id.ToString() ?? "" : "",
                    Action = action,
                    RiskLevel = fraudResult.Data.TryGetValue("risk_level", out var rl) ? rl.ToString() ?? "UNKNOWN" : "UNKNOWN",
                    VirtueProfile = combinedVirtue,
                    FraudIndicators = ExtractFraudIndicators(fraudResult),
                    Explanation = $"{fraudResult.Explanation} | Quality: {qualityResult.VirtueProfile.Integrity:F2}",
                    ProcessingTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds
                };

                _logger.LogWarning(
                    $"[AegisCouncil] Fraud decision: {action} | " +
                    $"Risk={decision.RiskLevel} | " +
                    $"Virtue={combinedVirtue.Average:F2}");

                return decision;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[AegisCouncil] Fraud analysis error: {ex.Message}");
                return new FraudAnalysisDecision
                {
                    Action = "ERROR",
                    Error = ex.Message,
                    ProcessingTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }
        }

        /// <summary>
        /// Get current system health metrics.
        /// </summary>
        public SystemHealth GetSystemHealth()
        {
            return _memory.ComputeHealth();
        }

        /// <summary>
        /// Manually trigger a snapshot (for debugging/testing).
        /// </summary>
        public SystemSnapshot? CreateSnapshot()
        {
            var health = _memory.ComputeHealth();
            return _memory.CreateSnapshot(health);
        }

        /// <summary>
        /// Manually trigger regeneration (for testing).
        /// </summary>
        public SystemSnapshot? Regenerate()
        {
            return _memory.Regenerate();
        }

        private CouncilDecision ExtractCouncilDecision(
            AgentResult[] councilResults,
            AgentResult councilResult)
        {
            var action = councilResult.Data.TryGetValue("action", out var a)
                ? a.ToString() ?? "none"
                : "none";

            var volatility = councilResult.Data.TryGetValue("volatility", out var v)
                && double.TryParse(v.ToString(), out var vd)
                ? vd
                : 0.0;

            return new CouncilDecision
            {
                Action = action,
                AggregateVirtue = councilResult.VirtueProfile,
                SystemVolatility = volatility,
                AgentCount = councilResults.Length,
                Explanation = councilResult.Explanation
            };
        }

        private Dictionary<string, double> ExtractFraudIndicators(AgentResult result)
        {
            var indicators = new Dictionary<string, double>();

            foreach (var key in new[] { "velocity_score", "amount_anomaly", "merchant_anomaly", "history_score" })
            {
                if (result.Data.TryGetValue(key, out var value) && double.TryParse(value.ToString(), out var dval))
                {
                    indicators[key] = dval;
                }
            }

            return indicators;
        }
    }

    /// <summary>
    /// Result of full Aegis council analysis.
    /// </summary>
    public class CouncilDecision
    {
        public string Action { get; set; } = "none";
        public VirtueProfile AggregateVirtue { get; set; } = new();
        public double SystemVolatility { get; set; }
        public int AgentCount { get; set; }
        public string Explanation { get; set; } = string.Empty;
        public string? Error { get; set; }
        public double ProcessingTimeMs { get; set; }
    }

    /// <summary>
    /// Specialized result for fraud analysis.
    /// </summary>
    public class FraudAnalysisDecision
    {
        public string TransactionId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // BLOCK, ALLOW, REVIEW
        public string RiskLevel { get; set; } = string.Empty; // LOW, MEDIUM, HIGH, CRITICAL
        public VirtueProfile VirtueProfile { get; set; } = new();
        public Dictionary<string, double> FraudIndicators { get; set; } = new();
        public string Explanation { get; set; } = string.Empty;
        public string? Error { get; set; }
        public double ProcessingTimeMs { get; set; }
    }
}
