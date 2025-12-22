using System;
using System.Collections.Generic;

namespace ConfluentBot.Models
{
    /// <summary>
    /// Represents a real-time data stream event.
    /// </summary>
    public class StreamEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Topic { get; set; } = string.Empty;
        public Dictionary<string, object> Data { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? CorrelationId { get; set; }
        public int Priority { get; set; }
    }

    /// <summary>
    /// Represents a stream metric or measurement.
    /// </summary>
    public class StreamMetric
    {
        public string MetricName { get; set; } = string.Empty;
        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, string> Tags { get; set; } = new();
    }

    /// <summary>
    /// Represents a prediction insight generated from stream processing.
    /// </summary>
    public class PredictionInsight
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string EventId { get; set; } = string.Empty;
        public string PredictionType { get; set; } = string.Empty;
        public Dictionary<string, object> Values { get; set; } = new();
        public double Confidence { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public List<string> RecommendedActions { get; set; } = new();
    }

    /// <summary>
    /// Represents a configuration for stream processing.
    /// </summary>
    public class StreamProcessingConfig
    {
        public string Name { get; set; } = string.Empty;
        public string InputTopic { get; set; } = string.Empty;
        public string OutputTopic { get; set; } = string.Empty;
        public bool EnableFeatureExtraction { get; set; } = true;
        public bool EnableAIPrediction { get; set; } = true;
        public int BatchSize { get; set; } = 10;
        public int MaxProcessingTimeoutMs { get; set; } = 30000;
    }

    /// <summary>
    /// Represents anomaly detection result.
    /// </summary>
    public class AnomalyDetectionResult
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string EventId { get; set; } = string.Empty;
        public bool IsAnomaly { get; set; }
        public double AnomalyScore { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> AnomalousFeatures { get; set; } = new();
    }

    /// <summary>
    /// Represents the result of trend analysis.
    /// </summary>
    public class TrendAnalysis
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Topic { get; set; } = string.Empty;
        public string TrendDirection { get; set; } = string.Empty; // "UP", "DOWN", "STABLE"
        public double TrendStrength { get; set; }
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
        public List<StreamMetric> RecentMetrics { get; set; } = new();
        public Dictionary<string, object> Analysis { get; set; } = new();
    }

    /// <summary>
    /// Represents an alert triggered by stream analysis.
    /// </summary>
    public class StreamAlert
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string AlertType { get; set; } = string.Empty; // "ANOMALY", "THRESHOLD", "TREND"
        public string Severity { get; set; } = "INFO"; // "INFO", "WARNING", "CRITICAL"
        public string Message { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Context { get; set; } = new();
        public List<string> RecommendedActions { get; set; } = new();
    }

    /// <summary>
    /// Represents a stream health check result.
    /// </summary>
    public class StreamHealthStatus
    {
        public string Topic { get; set; } = string.Empty;
        public bool IsHealthy { get; set; } = true;
        public string Status { get; set; } = "HEALTHY"; // "HEALTHY", "DEGRADED", "OFFLINE"
        public long MessageCount { get; set; }
        public double AverageLatencyMs { get; set; }
        public double ErrorRate { get; set; }
        public DateTime LastCheckedAt { get; set; } = DateTime.UtcNow;
        public List<string> Issues { get; set; } = new();
    }

    /// <summary>
    /// Request model for initiating stream analysis.
    /// </summary>
    public class StreamAnalysisRequest
    {
        public string Topic { get; set; } = string.Empty;
        public string AnalysisType { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    /// <summary>
    /// Response model for stream analysis.
    /// </summary>
    public class StreamAnalysisResponse
    {
        public string RequestId { get; set; } = Guid.NewGuid().ToString();
        public bool Success { get; set; }
        public string? Error { get; set; }
        public object? Result { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    }
}
