using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ConfluentBot.Services
{
    /// <summary>
    /// Real-time data processing pipeline that combines Kafka streaming with AI predictions.
    /// Implements reactive stream processing with feature extraction and enrichment.
    /// </summary>
    public interface IStreamProcessingPipeline
    {
        /// <summary>
        /// Processes a data item through the pipeline: extraction ? enrichment ? prediction.
        /// </summary>
        Task<EnrichedPrediction> ProcessAsync(StreamItem item, CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes multiple items in parallel.
        /// </summary>
        Task<List<EnrichedPrediction>> ProcessBatchAsync(List<StreamItem> items, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets pipeline health metrics.
        /// </summary>
        PipelineMetrics GetMetrics();

        /// <summary>
        /// Resets pipeline metrics.
        /// </summary>
        void ResetMetrics();
    }

    public class StreamProcessingPipeline : IStreamProcessingPipeline
    {
        private readonly IVertexAIPredictionService _predictionService;
        private readonly ILogger<StreamProcessingPipeline> _logger;
        private readonly PipelineMetrics _metrics;

        public StreamProcessingPipeline(
            IVertexAIPredictionService predictionService,
            ILogger<StreamProcessingPipeline> logger)
        {
            _predictionService = predictionService;
            _logger = logger;
            _metrics = new PipelineMetrics();
        }

        public async Task<EnrichedPrediction> ProcessAsync(StreamItem item, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // Stage 1: Extract features
                var features = ExtractFeatures(item);

                // Stage 2: Enrich with context
                var enrichedFeatures = EnrichFeatures(features, item);

                // Stage 3: Make prediction
                var prediction = await _predictionService.PredictAsync(enrichedFeatures);

                // Stage 4: Build enriched result
                var result = new EnrichedPrediction
                {
                    ItemId = item.Id,
                    SourceTopic = item.Topic,
                    ExtractedFeatures = features,
                    EnrichedFeatures = enrichedFeatures,
                    Prediction = prediction,
                    ProcessingDurationMs = (DateTime.UtcNow - startTime).TotalMilliseconds,
                    Status = prediction.Success ? "SUCCESS" : "PREDICTION_FAILED"
                };

                _metrics.RecordSuccess(result.ProcessingDurationMs);
                _logger.LogInformation($"Processed item {item.Id} successfully in {result.ProcessingDurationMs}ms");

                return result;
            }
            catch (Exception ex)
            {
                _metrics.RecordFailure();
                _logger.LogError($"Pipeline failed for item {item.Id}: {ex.Message}");

                return new EnrichedPrediction
                {
                    ItemId = item.Id,
                    SourceTopic = item.Topic,
                    Status = "PIPELINE_ERROR",
                    Error = ex.Message,
                    ProcessingDurationMs = (DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }
        }

        public async Task<List<EnrichedPrediction>> ProcessBatchAsync(List<StreamItem> items, CancellationToken cancellationToken = default)
        {
            var tasks = items
                .Select(item => ProcessAsync(item, cancellationToken))
                .ToList();

            var results = await Task.WhenAll(tasks);
            return results.ToList();
        }

        public PipelineMetrics GetMetrics()
        {
            return _metrics.Clone();
        }

        public void ResetMetrics()
        {
            _metrics.Reset();
        }

        /// <summary>
        /// Extracts relevant features from raw stream data.
        /// </summary>
        private Dictionary<string, object> ExtractFeatures(StreamItem item)
        {
            var features = new Dictionary<string, object>();

            // Parse JSON payload if available
            if (!string.IsNullOrEmpty(item.Payload))
            {
                try
                {
                    var data = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(item.Payload);
                    if (data != null)
                    {
                        features = data;
                    }
                }
                catch (Newtonsoft.Json.JsonException ex)
                {
                    _logger.LogWarning($"Failed to parse payload for item {item.Id}: {ex.Message}");
                    features["raw_payload"] = item.Payload;
                }
            }

            // Ensure critical numeric features are present
            if (!features.ContainsKey("timestamp"))
            {
                features["timestamp"] = new DateTimeOffset(item.Timestamp).ToUnixTimeMilliseconds();
            }

            return features;
        }

        /// <summary>
        /// Enriches features with contextual data.
        /// </summary>
        private Dictionary<string, object> EnrichFeatures(Dictionary<string, object> features, StreamItem item)
        {
            var enriched = new Dictionary<string, object>(features);

            // Add metadata
            enriched["topic"] = item.Topic;
            enriched["partition"] = item.Partition;
            enriched["offset"] = item.Offset;
            enriched["ingestion_timestamp"] = new DateTimeOffset(item.IngestionTime).ToUnixTimeMilliseconds();

            // Add derived features
            var ingestedAt = DateTime.UtcNow;
            enriched["processing_delay_ms"] = (ingestedAt - item.Timestamp).TotalMilliseconds;

            // Normalize numeric values
            NormalizeNumericFeatures(enriched);

            return enriched;
        }

        /// <summary>
        /// Normalizes numeric features for model input.
        /// </summary>
        private void NormalizeNumericFeatures(Dictionary<string, object> features)
        {
            var numericKeys = features
                .Where(f => f.Value is int or long or float or double)
                .Select(f => f.Key)
                .ToList();

            foreach (var key in numericKeys)
            {
                if (double.TryParse(features[key]?.ToString() ?? "0", out var value))
                {
                    // Apply min-max normalization for values in [0, 100] range
                    if (value > 100)
                    {
                        features[key] = Math.Min(value, 10000) / 10000.0;
                    }
                }
            }
        }
    }

    public class StreamItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Topic { get; set; } = string.Empty;
        public string? Payload { get; set; }
        public int Partition { get; set; }
        public long Offset { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public DateTime IngestionTime { get; set; } = DateTime.UtcNow;
    }

    public class EnrichedPrediction
    {
        public string ItemId { get; set; } = string.Empty;
        public string SourceTopic { get; set; } = string.Empty;
        public Dictionary<string, object>? ExtractedFeatures { get; set; }
        public Dictionary<string, object>? EnrichedFeatures { get; set; }
        public PredictionResult? Prediction { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Error { get; set; }
        public double ProcessingDurationMs { get; set; }
    }

    public class PipelineMetrics
    {
        private int _successCount;
        private int _failureCount;
        private readonly List<double> _processingTimes = new();
        private readonly object _lockObject = new();

        public int SuccessCount => _successCount;
        public int FailureCount => _failureCount;
        public int TotalCount => _successCount + _failureCount;
        public double AverageProcessingTimeMs => _processingTimes.Count > 0 ? _processingTimes.Average() : 0;
        public double MaxProcessingTimeMs => _processingTimes.Count > 0 ? _processingTimes.Max() : 0;
        public double MinProcessingTimeMs => _processingTimes.Count > 0 ? _processingTimes.Min() : 0;
        public double SuccessRate => TotalCount > 0 ? (_successCount / (double)TotalCount) * 100 : 0;

        public void RecordSuccess(double processingTimeMs)
        {
            lock (_lockObject)
            {
                _successCount++;
                _processingTimes.Add(processingTimeMs);
            }
        }

        public void RecordFailure()
        {
            lock (_lockObject)
            {
                _failureCount++;
            }
        }

        public void Reset()
        {
            lock (_lockObject)
            {
                _successCount = 0;
                _failureCount = 0;
                _processingTimes.Clear();
            }
        }

        public PipelineMetrics Clone()
        {
            lock (_lockObject)
            {
                var cloned = new PipelineMetrics
                {
                    _successCount = this._successCount,
                    _failureCount = this._failureCount
                };
                cloned._processingTimes.AddRange(this._processingTimes);
                return cloned;
            }
        }
    }
}
