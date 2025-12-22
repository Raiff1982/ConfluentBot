using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ConfluentBot.Services.AegisMemory
{
    /// <summary>
    /// Virtue profile: multi-dimensional confidence breakdown.
    /// Compassion: fairness and safety of prediction
    /// Integrity: data quality and model reliability
    /// Courage: confidence to act on prediction
    /// Wisdom: sufficiency of evidence for decision
    /// </summary>
    public class VirtueProfile
    {
        public double Compassion { get; set; }  // 0-1
        public double Integrity { get; set; }   // 0-1
        public double Courage { get; set; }     // 0-1
        public double Wisdom { get; set; }      // 0-1

        public double Average => (Compassion + Integrity + Courage + Wisdom) / 4.0;

        public override string ToString() =>
            $"Virtue(compassion={Compassion:F2} integrity={Integrity:F2} courage={Courage:F2} wisdom={Wisdom:F2})";
    }

    public class AgentResult
    {
        public string AgentName { get; set; } = string.Empty;
        public Dictionary<string, object> Data { get; set; } = new();
        public string Explanation { get; set; } = string.Empty;
        public VirtueProfile VirtueProfile { get; set; } = new();
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Base class for all stream analysis agents.
    /// Agents operate independently and produce virtue-based results.
    /// </summary>
    public abstract class StreamAgent
    {
        protected readonly string Name;
        protected readonly RegenerativeMemory Memory;
        protected readonly ILogger Logger;

        protected StreamAgent(string name, RegenerativeMemory memory, ILogger logger)
        {
            Name = name;
            Memory = memory;
            Logger = logger;
        }

        public abstract Task<AgentResult> AnalyzeAsync(Dictionary<string, object> inputData);

        protected void LogAnalysis(AgentResult result)
        {
            Logger.LogInformation(
                $"[{Name}] {result.Explanation} | Virtue: {result.VirtueProfile}");
        }

        /// <summary>
        /// Write analysis result to regenerative memory with virtue weighting.
        /// </summary>
        protected void RecordAnalysis(string topic, AgentResult result)
        {
            var avgVirtue = result.VirtueProfile.Average;
            Memory.Write(
                key: $"{Name}_{topic}_{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss-fff}",
                value: result.Data,
                emotionWeight: avgVirtue,
                virtueScore: avgVirtue);
        }
    }

    /// <summary>
    /// Data Quality Agent: assesses integrity of stream data.
    /// Checks for null values, schema violations, missing fields.
    /// </summary>
    public class DataQualityAgent : StreamAgent
    {
        public DataQualityAgent(RegenerativeMemory memory, ILogger logger)
            : base(nameof(DataQualityAgent), memory, logger)
        {
        }

        public override async Task<AgentResult> AnalyzeAsync(Dictionary<string, object> inputData)
        {
            return await Task.Run(() =>
            {
                var topic = inputData.TryGetValue("topic", out var t) ? t as string : "unknown";
                var payload = inputData.TryGetValue("payload", out var p) ? p as Dictionary<string, object> : new();

                double nullCount = 0;
                double totalFields = 0;

                if (payload != null)
                {
                    foreach (var (_, value) in payload)
                    {
                        totalFields++;
                        if (value == null || (value is string s && string.IsNullOrWhiteSpace(s)))
                        {
                            nullCount++;
                        }
                    }
                }

                var completeness = totalFields > 0 ? 1.0 - (nullCount / totalFields) : 1.0;
                var hasRequiredFields = totalFields >= 3; // Arbitrary threshold

                var virtue = new VirtueProfile
                {
                    Integrity = completeness,
                    Compassion = hasRequiredFields ? 0.9 : 0.5,
                    Courage = completeness * 0.8 + 0.2,
                    Wisdom = (completeness + (hasRequiredFields ? 1.0 : 0.0)) / 2.0
                };

                var result = new AgentResult
                {
                    AgentName = Name,
                    Data = new Dictionary<string, object>
                    {
                        { "completeness", Math.Round(completeness, 3) },
                        { "null_ratio", Math.Round(nullCount / Math.Max(1, totalFields), 3) },
                        { "field_count", totalFields },
                        { "quality_score", Math.Round(completeness, 3) }
                    },
                    VirtueProfile = virtue,
                    Explanation = $"Data quality: {virtue.Integrity:F2} integrity, {totalFields} fields, " +
                                 $"{nullCount} null values"
                };

                RecordAnalysis(topic ?? "unknown", result);
                LogAnalysis(result);
                return result;
            });
        }
    }

    /// <summary>
    /// Trend Agent: analyzes recent patterns to forecast stability.
    /// Looks for upward/downward trends and volatility.
    /// </summary>
    public class TrendAgent : StreamAgent
    {
        public TrendAgent(RegenerativeMemory memory, ILogger logger)
            : base(nameof(TrendAgent), memory, logger)
        {
        }

        public override async Task<AgentResult> AnalyzeAsync(Dictionary<string, object> inputData)
        {
            return await Task.Run(() =>
            {
                var topic = inputData.TryGetValue("topic", out var t) ? t as string : "unknown";
                var values = inputData.TryGetValue("values", out var v) && v is List<double> list
                    ? list
                    : new List<double>();

                string forecast = "stable";
                double trendStrength = 0.0;

                if (values.Count >= 3)
                {
                    // Simple linear trend: (last - first) / count
                    var trend = (values.Last() - values.First()) / values.Count;
                    trendStrength = Math.Abs(trend);

                    if (trend > 0.1) forecast = "increasing";
                    else if (trend < -0.1) forecast = "decreasing";
                }

                var virtue = new VirtueProfile
                {
                    Wisdom = Math.Min(1.0, (values.Count / 10.0) * 0.8 + 0.2),
                    Courage = Math.Min(1.0, 1.0 - trendStrength),
                    Integrity = forecast == "stable" ? 0.9 : 0.7,
                    Compassion = forecast == "stable" ? 0.9 : 0.6
                };

                var result = new AgentResult
                {
                    AgentName = Name,
                    Data = new Dictionary<string, object>
                    {
                        { "forecast", forecast },
                        { "trend_strength", Math.Round(trendStrength, 3) },
                        { "data_points", values.Count }
                    },
                    VirtueProfile = virtue,
                    Explanation = $"Trend forecast: {forecast} (strength={trendStrength:F2}, points={values.Count})"
                };

                RecordAnalysis(topic ?? "unknown", result);
                LogAnalysis(result);
                return result;
            });
        }
    }

    /// <summary>
    /// Stream Health Agent: monitors system volatility and memory health.
    /// </summary>
    public class StreamHealthAgent : StreamAgent
    {
        public StreamHealthAgent(RegenerativeMemory memory, ILogger logger)
            : base(nameof(StreamHealthAgent), memory, logger)
        {
        }

        public override async Task<AgentResult> AnalyzeAsync(Dictionary<string, object> inputData)
        {
            return await Task.Run(() =>
            {
                var health = Memory.ComputeHealth();

                var virtue = new VirtueProfile
                {
                    Integrity = 1.0 - health.Volatility,
                    Compassion = health.AverageVirtue,
                    Courage = Math.Max(0.0, 1.0 - health.Volatility),
                    Wisdom = (health.AverageVirtue + (1.0 - health.Volatility)) / 2.0
                };

                var result = new AgentResult
                {
                    AgentName = Name,
                    Data = new Dictionary<string, object>
                    {
                        { "volatility", Math.Round(health.Volatility, 4) },
                        { "avg_virtue", Math.Round(health.AverageVirtue, 4) },
                        { "density", Math.Round(health.Density, 4) },
                        { "total_entries", health.TotalEntries }
                    },
                    VirtueProfile = virtue,
                    Explanation = $"Stream health: volatility={health.Volatility:F3}, " +
                                 $"virtue={health.AverageVirtue:F3}, density={health.Density:F3}"
                };

                RecordAnalysis("system", result);
                LogAnalysis(result);
                return result;
            });
        }
    }

    /// <summary>
    /// Meta Council: combines agent insights and triggers regenerative cycle.
    /// This is the decision-making layer.
    /// </summary>
    public class MetaCouncil
    {
        private readonly RegenerativeMemory _memory;
        private readonly ILogger<MetaCouncil> _logger;
        private List<AgentResult> _agentResults = new();

        public MetaCouncil(RegenerativeMemory memory, ILogger<MetaCouncil> logger)
        {
            _memory = memory;
            _logger = logger;
        }

        public void SetAgentResults(List<AgentResult> results)
        {
            _agentResults = results ?? new();
        }

        public async Task<AgentResult> MakeDecisionAsync()
        {
            return await Task.Run(() =>
            {
                if (!_agentResults.Any())
                {
                    return new AgentResult
                    {
                        AgentName = "MetaCouncil",
                        Explanation = "No agent results to process",
                        Data = new Dictionary<string, object>()
                    };
                }

                // Aggregate virtue profiles
                var avgCompassion = _agentResults.Average(r => r.VirtueProfile.Compassion);
                var avgIntegrity = _agentResults.Average(r => r.VirtueProfile.Integrity);
                var avgCourage = _agentResults.Average(r => r.VirtueProfile.Courage);
                var avgWisdom = _agentResults.Average(r => r.VirtueProfile.Wisdom);

                var aggregateVirtue = new VirtueProfile
                {
                    Compassion = avgCompassion,
                    Integrity = avgIntegrity,
                    Courage = avgCourage,
                    Wisdom = avgWisdom
                };

                // Trigger regenerative cycle
                var health = _memory.ComputeHealth();
                var decision = _memory.RegenerativeCycle(health, aggregateVirtue.Average);

                var result = new AgentResult
                {
                    AgentName = "MetaCouncil",
                    VirtueProfile = aggregateVirtue,
                    Data = decision,
                    Explanation = $"Council decision: {decision["action"]} | " +
                                 $"Virtue: {aggregateVirtue} | " +
                                 $"System action triggered"
                };

                _logger.LogWarning(result.Explanation);
                return result;
            });
        }
    }
}
