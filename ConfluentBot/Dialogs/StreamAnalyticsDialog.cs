using ConfluentBot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ConfluentBot.Dialogs
{
    /// <summary>
    /// Dialog for analyzing real-time data streams using Kafka and Vertex AI predictions.
    /// Supports queries like "show me predictions for topic X" or "analyze stream health".
    /// </summary>
    public class StreamAnalyticsDialog : ComponentDialog
    {
        private const string SelectTopicStep = nameof(SelectTopicStep);
        private const string SelectAnalysisTypeStep = nameof(SelectAnalysisTypeStep);
        private const string ConfirmAnalysisStep = nameof(ConfirmAnalysisStep);
        private const string ResultsStep = nameof(ResultsStep);

        private readonly IKafkaConsumerService _kafkaConsumer;
        private readonly IStreamProcessingPipeline _pipeline;
        private readonly IStreamTelemetry _telemetry;

        public StreamAnalyticsDialog(
            IKafkaConsumerService kafkaConsumer,
            IStreamProcessingPipeline pipeline,
            IStreamTelemetry telemetry)
            : base(nameof(StreamAnalyticsDialog))
        {
            _kafkaConsumer = kafkaConsumer;
            _pipeline = pipeline;
            _telemetry = telemetry;

            var waterfallSteps = new WaterfallStep[]
            {
                SelectTopicAsync,
                SelectAnalysisTypeAsync,
                ConfirmAnalysisAsync,
                DisplayResultsAsync
            };

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> SelectTopicAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var topics = new[] { "transactions", "events", "metrics", "anomalies" };

            var choices = topics.Select(t => new Microsoft.Bot.Builder.Dialogs.Choices.Choice { Value = t }).ToList();

            return await stepContext.PromptAsync(
                nameof(ChoicePrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Which data stream would you like to analyze?"),
                    Choices = choices
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> SelectAnalysisTypeAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["Topic"] = ((Microsoft.Bot.Builder.Dialogs.Choices.FoundChoice)stepContext.Result).Value;

            var analysisTypes = new[]
            {
                new Microsoft.Bot.Builder.Dialogs.Choices.Choice { Value = "Latest Messages" },
                new Microsoft.Bot.Builder.Dialogs.Choices.Choice { Value = "Health Status" },
                new Microsoft.Bot.Builder.Dialogs.Choices.Choice { Value = "Predictions" },
                new Microsoft.Bot.Builder.Dialogs.Choices.Choice { Value = "Anomalies" }
            };

            return await stepContext.PromptAsync(
                nameof(ChoicePrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("What type of analysis would you like?"),
                    Choices = analysisTypes
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmAnalysisAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["AnalysisType"] = ((Microsoft.Bot.Builder.Dialogs.Choices.FoundChoice)stepContext.Result).Value;

            var topic = (string)stepContext.Values["Topic"];
            var analysisType = (string)stepContext.Values["AnalysisType"];

            var message = $"I'll analyze {analysisType.ToLower()} for the '{topic}' stream. Ready to proceed?";

            return await stepContext.PromptAsync(
                nameof(ConfirmPrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text(message)
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> DisplayResultsAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var confirmed = (bool)stepContext.Result;

            if (!confirmed)
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("Analysis cancelled."),
                    cancellationToken);
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }

            var topic = (string)stepContext.Values["Topic"];
            var analysisType = (string)stepContext.Values["AnalysisType"];

            try
            {
                Activity responseActivity;

                switch (analysisType)
                {
                    case "Latest Messages":
                        responseActivity = await GetLatestMessagesActivity(topic, cancellationToken);
                        break;

                    case "Health Status":
                        responseActivity = await GetHealthStatusActivity(topic, cancellationToken);
                        break;

                    case "Predictions":
                        responseActivity = await GetPredictionsActivity(topic, cancellationToken);
                        break;

                    case "Anomalies":
                        responseActivity = await GetAnomaliesActivity(topic, cancellationToken);
                        break;

                    default:
                        responseActivity = MessageFactory.Text("Unknown analysis type.");
                        break;
                }

                await stepContext.Context.SendActivityAsync(responseActivity, cancellationToken);
            }
            catch (Exception ex)
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text($"An error occurred during analysis: {ex.Message}"),
                    cancellationToken);
            }

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private async Task<Activity> GetLatestMessagesActivity(string topic, CancellationToken cancellationToken)
        {
            var messages = await _kafkaConsumer.GetLatestMessagesAsync<Dictionary<string, object>>(topic, 5);

            if (messages.Count == 0)
            {
                return MessageFactory.Text($"No recent messages found in '{topic}' topic.");
            }

            var text = $"?? **Latest 5 messages from '{topic}' topic:**\n\n";
            for (int i = 0; i < messages.Count; i++)
            {
                text += $"{i + 1}. {string.Join(", ", messages[i].Select(kv => $"{kv.Key}: {kv.Value}"))}\n";
            }

            return MessageFactory.Text(text);
        }

        private async Task<Activity> GetHealthStatusActivity(string topic, CancellationToken cancellationToken)
        {
            var stats = await _kafkaConsumer.GetTopicStatisticsAsync(topic);
            var metrics = _telemetry.GetMetrics(topic);

            var text = $"?? **Stream Health Status for '{topic}':**\n\n" +
                $"• Message Count: {stats.MessageCount}\n" +
                $"• Buffer Utilization: {stats.UtilizationPercentage:F2}%\n" +
                $"• Average Processing Time: {metrics.AverageProcessingTimeMs:F2}ms\n" +
                $"• Success Rate: {metrics.SuccessRate:F2}%\n" +
                $"• Prediction Accuracy: {metrics.PredictionAccuracy:F2}%";

            return MessageFactory.Text(text);
        }

        private async Task<Activity> GetPredictionsActivity(string topic, CancellationToken cancellationToken)
        {
            var messages = await _kafkaConsumer.GetLatestMessagesAsync<Dictionary<string, object>>(topic, 3);

            if (messages.Count == 0)
            {
                return MessageFactory.Text($"No recent messages found for predictions.");
            }

            var text = $"?? **AI Predictions for '{topic}' stream:**\n\n";

            foreach (var msg in messages)
            {
                var streamItem = new StreamItem
                {
                    Topic = topic,
                    Payload = Newtonsoft.Json.JsonConvert.SerializeObject(msg)
                };

                var prediction = await _pipeline.ProcessAsync(streamItem);

                text += $"• Event: {msg.Values.FirstOrDefault()}\n";
                text += $"  Prediction: {(prediction.Prediction?.Prediction?.FirstOrDefault().Value ?? "Unknown")}\n";
                text += $"  Confidence: {prediction.Prediction?.Confidence:F2}%\n\n";
            }

            return MessageFactory.Text(text);
        }

        private async Task<Activity> GetAnomaliesActivity(string topic, CancellationToken cancellationToken)
        {
            var messages = await _kafkaConsumer.GetLatestMessagesAsync<Dictionary<string, object>>(topic, 10);

            if (messages.Count == 0)
            {
                return MessageFactory.Text($"No recent messages found for anomaly detection.");
            }

            var anomalyCount = 0;
            var text = $"?? **Anomaly Detection Results for '{topic}':**\n\n";

            foreach (var msg in messages.Take(5))
            {
                // Simple anomaly detection based on value variance
                var values = msg.Values.OfType<int>().ToList();
                if (values.Count > 0)
                {
                    var average = values.Average();
                    var hasAnomaly = values.Any(v => Math.Abs(v - average) > average * 0.5);

                    if (hasAnomaly)
                    {
                        anomalyCount++;
                        text += $"?? Anomaly detected: {string.Join(", ", msg.Select(kv => $"{kv.Key}={kv.Value}"))}\n";
                    }
                }
            }

            if (anomalyCount == 0)
            {
                text += "? No anomalies detected in recent data.";
            }

            return MessageFactory.Text(text);
        }
    }
}
