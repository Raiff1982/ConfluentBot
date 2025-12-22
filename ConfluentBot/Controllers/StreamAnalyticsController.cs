using ConfluentBot.Models;
using ConfluentBot.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ConfluentBot.Controllers
{
    /// <summary>
    /// API endpoints for real-time stream analytics and AI predictions.
    /// Exposes Kafka stream data, Vertex AI predictions, and telemetry metrics.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class StreamAnalyticsController : ControllerBase
    {
        private readonly IKafkaConsumerService _kafkaConsumer;
        private readonly IStreamProcessingPipeline _pipeline;
        private readonly IStreamTelemetry _telemetry;

        public StreamAnalyticsController(
            IKafkaConsumerService kafkaConsumer,
            IStreamProcessingPipeline pipeline,
            IStreamTelemetry telemetry)
        {
            _kafkaConsumer = kafkaConsumer;
            _pipeline = pipeline;
            _telemetry = telemetry;
        }

        /// <summary>
        /// Gets the latest messages from a specific topic.
        /// </summary>
        /// <param name="topic">Topic name to query</param>
        /// <param name="count">Number of messages to retrieve (default: 10, max: 100)</param>
        [HttpGet("messages/{topic}")]
        [ProducesResponseType(typeof(StreamAnalysisResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetLatestMessages(string topic, [FromQuery] int count = 10)
        {
            if (string.IsNullOrEmpty(topic))
                return BadRequest("Topic name is required");

            if (count <= 0 || count > 100)
                count = 10;

            try
            {
                var messages = await _kafkaConsumer.GetLatestMessagesAsync<Dictionary<string, object>>(topic, count);

                return Ok(new StreamAnalysisResponse
                {
                    Success = true,
                    Result = messages,
                    Metadata = new Dictionary<string, object>
                    {
                        { "topic", topic },
                        { "message_count", messages.Count }
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new StreamAnalysisResponse
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Gets health status and statistics for a topic.
        /// </summary>
        /// <param name="topic">Topic name to check</param>
        [HttpGet("health/{topic}")]
        [ProducesResponseType(typeof(StreamAnalysisResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetTopicHealth(string topic)
        {
            if (string.IsNullOrEmpty(topic))
                return BadRequest("Topic name is required");

            try
            {
                var stats = await _kafkaConsumer.GetTopicStatisticsAsync(topic);
                var metrics = _telemetry.GetMetrics(topic);

                return Ok(new StreamAnalysisResponse
                {
                    Success = true,
                    Result = new
                    {
                        stats,
                        metrics
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new StreamAnalysisResponse
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Processes stream items through the AI prediction pipeline.
        /// </summary>
        /// <param name="request">Stream analysis request with topic and parameters</param>
        [HttpPost("predict")]
        [ProducesResponseType(typeof(StreamAnalysisResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> ProcessPrediction([FromBody] StreamAnalysisRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Topic))
                return BadRequest("Request with topic is required");

            try
            {
                var messages = await _kafkaConsumer.GetLatestMessagesAsync<Dictionary<string, object>>(
                    request.Topic, 
                    Math.Min(request.Parameters.ContainsKey("batch_size") ? (int)request.Parameters["batch_size"] : 5, 50));

                var predictions = new List<object>();

                foreach (var msg in messages)
                {
                    var streamItem = new StreamItem
                    {
                        Topic = request.Topic,
                        Payload = Newtonsoft.Json.JsonConvert.SerializeObject(msg)
                    };

                    var result = await _pipeline.ProcessAsync(streamItem);
                    predictions.Add(new
                    {
                        result.ItemId,
                        result.Status,
                        result.Prediction,
                        result.ProcessingDurationMs
                    });
                }

                return Ok(new StreamAnalysisResponse
                {
                    Success = true,
                    Result = predictions,
                    Metadata = new Dictionary<string, object>
                    {
                        { "topic", request.Topic },
                        { "predictions_count", predictions.Count }
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new StreamAnalysisResponse
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Gets current system health and aggregate metrics.
        /// </summary>
        [HttpGet("system-health")]
        [ProducesResponseType(typeof(StreamAnalysisResponse), 200)]
        public IActionResult GetSystemHealth()
        {
            try
            {
                var health = _telemetry.GetSystemHealth();

                return Ok(new StreamAnalysisResponse
                {
                    Success = true,
                    Result = health
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new StreamAnalysisResponse
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Gets historical metrics and percentiles for a topic.
        /// </summary>
        /// <param name="topic">Topic name</param>
        /// <param name="durationSeconds">Duration to analyze (default: 300 seconds)</param>
        [HttpGet("metrics/{topic}")]
        [ProducesResponseType(typeof(StreamAnalysisResponse), 200)]
        [ProducesResponseType(400)]
        public IActionResult GetTopicMetrics(string topic, [FromQuery] int durationSeconds = 300)
        {
            if (string.IsNullOrEmpty(topic))
                return BadRequest("Topic name is required");

            if (durationSeconds <= 0)
                durationSeconds = 300;

            try
            {
                var metrics = _telemetry.GetHistoricalMetrics(topic, durationSeconds);

                return Ok(new StreamAnalysisResponse
                {
                    Success = true,
                    Result = metrics
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new StreamAnalysisResponse
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Gets pipeline performance metrics.
        /// </summary>
        [HttpGet("pipeline-metrics")]
        [ProducesResponseType(typeof(StreamAnalysisResponse), 200)]
        public IActionResult GetPipelineMetrics()
        {
            try
            {
                var metrics = _pipeline.GetMetrics();

                return Ok(new StreamAnalysisResponse
                {
                    Success = true,
                    Result = metrics
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new StreamAnalysisResponse
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }
    }
}
