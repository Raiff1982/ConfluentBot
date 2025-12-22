using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ConfluentBot.Services
{
    /// <summary>
    /// Service for consuming real-time data from Confluent Kafka topics.
    /// Provides event-driven architecture for streaming data processing.
    /// </summary>
    public interface IKafkaConsumerService : IHostedService
    {
        /// <summary>
        /// Gets the latest messages from a topic, applying optional filtering.
        /// </summary>
        Task<List<T>> GetLatestMessagesAsync<T>(string topic, int maxMessages = 10) where T : class;

        /// <summary>
        /// Subscribes to real-time updates for a topic.
        /// </summary>
        void SubscribeToTopic(string topic, Func<string, Task> onMessageReceived);

        /// <summary>
        /// Unsubscribes from a topic.
        /// </summary>
        void UnsubscribeFromTopic(string topic);

        /// <summary>
        /// Gets aggregate statistics for a topic.
        /// </summary>
        Task<TopicStatistics> GetTopicStatisticsAsync(string topic);
    }

    public class KafkaConsumerService : IKafkaConsumerService
    {
        private readonly IConsumer<string, string> _consumer;
        private readonly ILogger<KafkaConsumerService> _logger;
        private readonly string[] _topics;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _consumerTask;
        private readonly ConcurrentDictionary<string, Queue<string>> _messageBuffer;
        private readonly ConcurrentDictionary<string, List<Func<string, Task>>> _subscribers;
        private readonly int _bufferSize;

        public KafkaConsumerService(ILogger<KafkaConsumerService> logger, KafkaConsumerConfig config)
        {
            _logger = logger;
            _bufferSize = config.BufferSize;
            _messageBuffer = new ConcurrentDictionary<string, Queue<string>>();
            _subscribers = new ConcurrentDictionary<string, List<Func<string, Task>>>();
            _topics = config.Topics ?? Array.Empty<string>();

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = config.BootstrapServers,
                GroupId = config.GroupId ?? "ConfluentBot-Consumer-Group",
                AutoOffsetReset = AutoOffsetReset.Latest,
                EnableAutoCommit = true,
                EnableAutoOffsetStore = false,
                MaxPollIntervalMs = 300000,
                SessionTimeoutMs = 30000,
            };

            _consumer = new ConsumerBuilder<string, string>(consumerConfig)
                .SetErrorHandler((_, error) => _logger.LogError($"Kafka Error: {error}"))
                .Build();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Kafka Consumer Service");
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            if (_topics.Length > 0)
            {
                _consumer.Subscribe(_topics);
                _logger.LogInformation($"Subscribed to topics: {string.Join(", ", _topics)}");

                // Initialize message buffers for each topic
                foreach (var topic in _topics)
                {
                    _messageBuffer.TryAdd(topic, new Queue<string>(_bufferSize));
                    _subscribers.TryAdd(topic, new List<Func<string, Task>>());
                }

                _consumerTask = ConsumeMessagesAsync(_cancellationTokenSource.Token);
            }
            else
            {
                _logger.LogWarning("No topics configured for Kafka consumer");
            }

            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Kafka Consumer Service");
            _cancellationTokenSource?.Cancel();

            if (_consumerTask != null)
            {
                try
                {
                    await _consumerTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected during shutdown
                }
            }

            _consumer?.Close();
            _consumer?.Dispose();
            _cancellationTokenSource?.Dispose();
        }

        public async Task<List<T>> GetLatestMessagesAsync<T>(string topic, int maxMessages = 10) where T : class
        {
            var messages = new List<T>();

            if (_messageBuffer.TryGetValue(topic, out var buffer))
            {
                lock (buffer)
                {
                    var count = 0;
                    foreach (var message in buffer.Reverse().Take(maxMessages))
                    {
                        try
                        {
                            var obj = JsonConvert.DeserializeObject<T>(message);
                            if (obj != null)
                            {
                                messages.Insert(0, obj);
                                count++;
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning($"Failed to deserialize message from {topic}: {ex.Message}");
                        }
                    }
                }
            }

            return await Task.FromResult(messages);
        }

        public void SubscribeToTopic(string topic, Func<string, Task> onMessageReceived)
        {
            if (_subscribers.TryGetValue(topic, out var handlers))
            {
                lock (handlers)
                {
                    handlers.Add(onMessageReceived);
                }
            }
        }

        public void UnsubscribeFromTopic(string topic)
        {
            if (_subscribers.TryRemove(topic, out _))
            {
                _logger.LogInformation($"Unsubscribed from topic: {topic}");
            }
        }

        public async Task<TopicStatistics> GetTopicStatisticsAsync(string topic)
        {
            var stats = new TopicStatistics { Topic = topic };

            if (_messageBuffer.TryGetValue(topic, out var buffer))
            {
                lock (buffer)
                {
                    stats.MessageCount = buffer.Count;
                    stats.BufferCapacity = _bufferSize;
                    stats.UtilizationPercentage = (stats.MessageCount / (double)_bufferSize) * 100;
                }
            }

            return await Task.FromResult(stats);
        }

        private async Task ConsumeMessagesAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var message = _consumer.Consume(TimeSpan.FromMilliseconds(100));

                        if (message != null && message.Message != null)
                        {
                            var topic = message.Topic;
                            var value = message.Message.Value;

                            // Add to buffer
                            if (_messageBuffer.TryGetValue(topic, out var buffer))
                            {
                                lock (buffer)
                                {
                                    if (buffer.Count >= _bufferSize)
                                    {
                                        buffer.Dequeue();
                                    }
                                    buffer.Enqueue(value);
                                }
                            }

                            // Notify subscribers
                            if (_subscribers.TryGetValue(topic, out var handlers))
                            {
                                lock (handlers)
                                {
                                    foreach (var handler in handlers)
                                    {
                                        _ = handler(value).ConfigureAwait(false);
                                    }
                                }
                            }

                            _logger.LogDebug($"Consumed message from {topic}");
                        }
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError($"Consume error: {ex.Error.Reason}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Kafka consumer cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error in Kafka consumer: {ex}");
            }
        }
    }

    public class KafkaConsumerConfig
    {
        public string BootstrapServers { get; set; } = "localhost:9092";
        public string? GroupId { get; set; }
        public string[]? Topics { get; set; }
        public int BufferSize { get; set; } = 100;
    }

    public class TopicStatistics
    {
        public string Topic { get; set; } = string.Empty;
        public int MessageCount { get; set; }
        public int BufferCapacity { get; set; }
        public double UtilizationPercentage { get; set; }
    }
}
