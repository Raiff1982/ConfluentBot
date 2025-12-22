using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace ConfluentBot.Services
{
    /// <summary>
    /// Telemetry service for monitoring stream processing performance and health.
    /// Tracks metrics like throughput, latency, error rates, and resource usage.
    /// </summary>
    public interface IStreamTelemetry
    {
        /// <summary>
        /// Records a processed message.
        /// </summary>
        void RecordMessage(string topic, double processingTimeMs, bool success);

        /// <summary>
        /// Records a prediction result.
        /// </summary>
        void RecordPrediction(string topic, double confidence, bool success);

        /// <summary>
        /// Gets current metrics for a topic.
        /// </summary>
        StreamMetrics GetMetrics(string topic);

        /// <summary>
        /// Gets overall system health.
        /// </summary>
        SystemHealth GetSystemHealth();

        /// <summary>
        /// Gets historical metrics data.
        /// </summary>
        HistoricalMetrics GetHistoricalMetrics(string topic, int durationSeconds = 300);
    }

    public class StreamTelemetry : IStreamTelemetry
    {
        private readonly ILogger<StreamTelemetry> _logger;
        private readonly Dictionary<string, TopicMetrics> _topicMetrics = new();
        private readonly object _lockObject = new();
        private readonly Stopwatch _uptime = Stopwatch.StartNew();
        private long _totalMessagesProcessed;
        private long _totalPredictionsMade;
        private long _totalErrors;

        public StreamTelemetry(ILogger<StreamTelemetry> logger)
        {
            _logger = logger;
        }

        public void RecordMessage(string topic, double processingTimeMs, bool success)
        {
            lock (_lockObject)
            {
                if (!_topicMetrics.TryGetValue(topic, out var metrics))
                {
                    metrics = new TopicMetrics { Topic = topic };
                    _topicMetrics[topic] = metrics;
                }

                metrics.RecordMessage(processingTimeMs, success);
                _totalMessagesProcessed++;

                if (!success)
                {
                    _totalErrors++;
                }
            }

            _logger.LogDebug($"Recorded message for {topic}: {processingTimeMs}ms, Success={success}");
        }

        public void RecordPrediction(string topic, double confidence, bool success)
        {
            lock (_lockObject)
            {
                if (!_topicMetrics.TryGetValue(topic, out var metrics))
                {
                    metrics = new TopicMetrics { Topic = topic };
                    _topicMetrics[topic] = metrics;
                }

                metrics.RecordPrediction(confidence, success);
                _totalPredictionsMade++;

                if (!success)
                {
                    _totalErrors++;
                }
            }

            _logger.LogDebug($"Recorded prediction for {topic}: Confidence={confidence}, Success={success}");
        }

        public StreamMetrics GetMetrics(string topic)
        {
            lock (_lockObject)
            {
                if (_topicMetrics.TryGetValue(topic, out var metrics))
                {
                    return metrics.ToStreamMetrics();
                }
            }

            return new StreamMetrics { Topic = topic };
        }

        public SystemHealth GetSystemHealth()
        {
            lock (_lockObject)
            {
                var health = new SystemHealth
                {
                    UptimeSeconds = (int)_uptime.Elapsed.TotalSeconds,
                    TotalMessagesProcessed = _totalMessagesProcessed,
                    TotalPredictionsMade = _totalPredictionsMade,
                    TotalErrors = _totalErrors,
                    CheckedAt = DateTime.UtcNow
                };

                // Calculate overall metrics
                foreach (var topic in _topicMetrics.Keys)
                {
                    var metrics = _topicMetrics[topic];
                    health.TopicsCount++;
                    health.AverageLatencyMs += metrics.AverageProcessingTimeMs;
                    health.AverageConfidence += metrics.AveragePredictionConfidence;
                }

                if (health.TopicsCount > 0)
                {
                    health.AverageLatencyMs /= health.TopicsCount;
                    health.AverageConfidence /= health.TopicsCount;
                }

                health.OverallErrorRate = health.TotalMessagesProcessed > 0
                    ? (health.TotalErrors / (double)health.TotalMessagesProcessed) * 100
                    : 0;

                health.Status = DetermineHealthStatus(health);

                return health;
            }
        }

        public HistoricalMetrics GetHistoricalMetrics(string topic, int durationSeconds = 300)
        {
            lock (_lockObject)
            {
                if (_topicMetrics.TryGetValue(topic, out var metrics))
                {
                    return new HistoricalMetrics
                    {
                        Topic = topic,
                        DurationSeconds = durationSeconds,
                        CollectedAt = DateTime.UtcNow,
                        LatencyPercentiles = CalculatePercentiles(metrics.ProcessingTimes),
                        ConfidenceDistribution = CalculateConfidenceDistribution(metrics.PredictionConfidences),
                        MessageCountByMinute = metrics.GetMessageCountByMinute(durationSeconds)
                    };
                }
            }

            return new HistoricalMetrics { Topic = topic };
        }

        private string DetermineHealthStatus(SystemHealth health)
        {
            if (health.OverallErrorRate > 10)
                return "CRITICAL";
            if (health.OverallErrorRate > 5)
                return "DEGRADED";
            if (health.AverageLatencyMs > 5000)
                return "SLOW";
            return "HEALTHY";
        }

        private Dictionary<string, double> CalculatePercentiles(List<double> values)
        {
            if (values.Count == 0)
                return new Dictionary<string, double>();

            var sorted = values.OrderBy(v => v).ToList();
            return new Dictionary<string, double>
            {
                { "p50", GetPercentile(sorted, 50) },
                { "p75", GetPercentile(sorted, 75) },
                { "p90", GetPercentile(sorted, 90) },
                { "p95", GetPercentile(sorted, 95) },
                { "p99", GetPercentile(sorted, 99) }
            };
        }

        private Dictionary<string, int> CalculateConfidenceDistribution(List<double> confidences)
        {
            var distribution = new Dictionary<string, int>
            {
                { "0-20%", 0 },
                { "20-40%", 0 },
                { "40-60%", 0 },
                { "60-80%", 0 },
                { "80-100%", 0 }
            };

            foreach (var conf in confidences)
            {
                if (conf < 0.2) distribution["0-20%"]++;
                else if (conf < 0.4) distribution["20-40%"]++;
                else if (conf < 0.6) distribution["40-60%"]++;
                else if (conf < 0.8) distribution["60-80%"]++;
                else distribution["80-100%"]++;
            }

            return distribution;
        }

        private double GetPercentile(List<double> sortedValues, int percentile)
        {
            if (sortedValues.Count == 0) return 0;
            int index = (int)Math.Ceiling(sortedValues.Count * percentile / 100.0) - 1;
            return sortedValues[Math.Max(0, index)];
        }
    }

    public class TopicMetrics
    {
        public string Topic { get; set; } = string.Empty;
        private long _messageCount;
        private long _successCount;
        private long _predictionCount;
        private long _predictionSuccessCount;
        public List<double> ProcessingTimes { get; } = new();
        public List<double> PredictionConfidences { get; } = new();

        public double AverageProcessingTimeMs => ProcessingTimes.Count > 0 ? ProcessingTimes.Average() : 0;
        public double AveragePredictionConfidence => PredictionConfidences.Count > 0 ? PredictionConfidences.Average() : 0;

        public void RecordMessage(double processingTimeMs, bool success)
        {
            Interlocked.Increment(ref _messageCount);
            if (success)
                Interlocked.Increment(ref _successCount);

            lock (ProcessingTimes)
            {
                ProcessingTimes.Add(processingTimeMs);
                // Keep only last 1000 values
                if (ProcessingTimes.Count > 1000)
                    ProcessingTimes.RemoveRange(0, ProcessingTimes.Count - 1000);
            }
        }

        public void RecordPrediction(double confidence, bool success)
        {
            Interlocked.Increment(ref _predictionCount);
            if (success)
                Interlocked.Increment(ref _predictionSuccessCount);

            lock (PredictionConfidences)
            {
                PredictionConfidences.Add(confidence);
                // Keep only last 1000 values
                if (PredictionConfidences.Count > 1000)
                    PredictionConfidences.RemoveRange(0, PredictionConfidences.Count - 1000);
            }
        }

        public StreamMetrics ToStreamMetrics()
        {
            return new StreamMetrics
            {
                Topic = Topic,
                MessageCount = _messageCount,
                SuccessCount = _successCount,
                PredictionCount = _predictionCount,
                PredictionSuccessCount = _predictionSuccessCount,
                AverageProcessingTimeMs = AverageProcessingTimeMs,
                AveragePredictionConfidence = AveragePredictionConfidence,
                SuccessRate = _messageCount > 0 ? (_successCount / (double)_messageCount) * 100 : 0,
                PredictionAccuracy = _predictionCount > 0 ? (_predictionSuccessCount / (double)_predictionCount) * 100 : 0
            };
        }

        public Dictionary<string, int> GetMessageCountByMinute(int durationSeconds)
        {
            var result = new Dictionary<string, int>();
            var now = DateTime.UtcNow;

            for (int i = 0; i < (durationSeconds / 60); i++)
            {
                var minute = now.AddSeconds(-(i * 60));
                result[minute.ToString("yyyy-MM-dd HH:mm")] = 0;
            }

            return result;
        }
    }

    public class StreamMetrics
    {
        public string Topic { get; set; } = string.Empty;
        public long MessageCount { get; set; }
        public long SuccessCount { get; set; }
        public long PredictionCount { get; set; }
        public long PredictionSuccessCount { get; set; }
        public double AverageProcessingTimeMs { get; set; }
        public double AveragePredictionConfidence { get; set; }
        public double SuccessRate { get; set; }
        public double PredictionAccuracy { get; set; }
    }

    public class SystemHealth
    {
        public int UptimeSeconds { get; set; }
        public long TotalMessagesProcessed { get; set; }
        public long TotalPredictionsMade { get; set; }
        public long TotalErrors { get; set; }
        public int TopicsCount { get; set; }
        public double AverageLatencyMs { get; set; }
        public double AverageConfidence { get; set; }
        public double OverallErrorRate { get; set; }
        public string Status { get; set; } = "HEALTHY";
        public DateTime CheckedAt { get; set; }
    }

    public class HistoricalMetrics
    {
        public string Topic { get; set; } = string.Empty;
        public int DurationSeconds { get; set; }
        public DateTime CollectedAt { get; set; }
        public Dictionary<string, double> LatencyPercentiles { get; set; } = new();
        public Dictionary<string, int> ConfidenceDistribution { get; set; } = new();
        public Dictionary<string, int> MessageCountByMinute { get; set; } = new();
    }
}
