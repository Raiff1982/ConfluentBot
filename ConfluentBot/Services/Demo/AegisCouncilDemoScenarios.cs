using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ConfluentBot.Services.AegisMemory;
using Microsoft.Extensions.Logging;

namespace ConfluentBot.Services.Demo
{
    /// <summary>
    /// Demo scenarios for the Aegis Stream Council integrated with real-time fraud detection.
    /// Shows how the multi-agent framework exceeds expectations in the Confluent Challenge.
    /// </summary>
    public class AegisCouncilDemoScenarios
    {
        private readonly AegisStreamCouncil _council;
        private readonly ILogger<AegisCouncilDemoScenarios> _logger;

        public AegisCouncilDemoScenarios(
            AegisStreamCouncil council,
            ILogger<AegisCouncilDemoScenarios> logger)
        {
            _council = council;
            _logger = logger;
        }

        /// <summary>
        /// Scenario 1: Detect Fraud via Impossible Travel
        /// Same card used in 2 different cities within impossible timeframe.
        /// </summary>
        public async Task RunImpossibleTravelScenarioAsync()
        {
            _logger.LogInformation("=== SCENARIO 1: Impossible Travel Fraud ===");

            var transaction1 = new Dictionary<string, object>
            {
                { "id", Guid.NewGuid().ToString() },
                { "card_id", "CARD_12345" },
                { "amount", 150.00 },
                { "merchant", "Starbucks NYC" },
                { "location", "New York" },
                { "timestamp", DateTime.UtcNow },
                { "data_quality", 0.95 }
            };

            var transaction2 = new Dictionary<string, object>
            {
                { "id", Guid.NewGuid().ToString() },
                { "card_id", "CARD_12345" },
                { "amount", 200.00 },
                { "merchant", "Starbucks LAX" },
                { "location", "Los Angeles" },
                { "timestamp", DateTime.UtcNow.AddMinutes(5) }, // 5 minutes later - impossible
                { "data_quality", 0.90 }
            };

            _logger.LogInformation("\n[Transaction 1] NYC - Should be allowed");
            var decision1 = await _council.AnalyzeFraudAsync(transaction1);
            LogFraudDecision(decision1);

            _logger.LogInformation("\n[Transaction 2] LAX (5 min later) - Should be BLOCKED (impossible travel)");
            var decision2 = await _council.AnalyzeFraudAsync(transaction2);
            LogFraudDecision(decision2);

            _logger.LogInformation($"\nResult: {(decision2.Action == "BLOCK" ? "? FRAUD DETECTED" : "? MISSED")}");
        }

        /// <summary>
        /// Scenario 2: Detect Anomalous Transaction Amount
        /// Card historically used for small purchases suddenly shows large transaction.
        /// </summary>
        public async Task RunAnomalousAmountScenarioAsync()
        {
            _logger.LogInformation("\n=== SCENARIO 2: Unusual Amount Anomaly ===");

            var cardId = "CARD_ANOMALY_" + Guid.NewGuid();

            // Establish baseline with small transactions
            for (int i = 0; i < 5; i++)
            {
                var baseline = new Dictionary<string, object>
                {
                    { "id", Guid.NewGuid().ToString() },
                    { "card_id", cardId },
                    { "amount", 15.00 + i }, // $15-20 range
                    { "merchant", "Grocery Store" },
                    { "location", "Home City" },
                    { "timestamp", DateTime.UtcNow.AddHours(-i) },
                    { "data_quality", 0.98 }
                };

                await _council.AnalyzeFraudAsync(baseline);
            }

            // Now hit with huge transaction (3 std devs away)
            var anomalous = new Dictionary<string, object>
            {
                { "id", Guid.NewGuid().ToString() },
                { "card_id", cardId },
                { "amount", 5000.00 }, // Huge spike!
                { "merchant", "Luxury Watch Store" },
                { "location", "Home City" },
                { "timestamp", DateTime.UtcNow },
                { "data_quality", 0.92 }
            };

            _logger.LogInformation("\n[After establishing baseline] New $5000 transaction");
            var decision = await _council.AnalyzeFraudAsync(anomalous);
            LogFraudDecision(decision);

            _logger.LogInformation($"\nResult: {(decision.RiskLevel == "HIGH" || decision.RiskLevel == "CRITICAL" ? "? ANOMALY DETECTED" : "? MISSED")}");
        }

        /// <summary>
        /// Scenario 3: Batch Fraud Analysis with Council Decision
        /// Process multiple transactions and show council coordination.
        /// </summary>
        public async Task RunBatchFraudAnalysisScenarioAsync()
        {
            _logger.LogInformation("\n=== SCENARIO 3: Batch Fraud Analysis (5 transactions) ===");

            var transactions = new List<Dictionary<string, object>>
            {
                // Normal transaction
                new Dictionary<string, object>
                {
                    { "id", "TXN_001" },
                    { "card_id", "CARD_BATCH_A" },
                    { "amount", 75.50 },
                    { "merchant", "Gas Station" },
                    { "location", "Home" },
                    { "timestamp", DateTime.UtcNow },
                    { "data_quality", 0.97 }
                },
                // Suspicious: unusual merchant
                new Dictionary<string, object>
                {
                    { "id", "TXN_002" },
                    { "card_id", "CARD_BATCH_B" },
                    { "amount", 2500.00 },
                    { "merchant", "Wire Transfer Service" },
                    { "location", "Online" },
                    { "timestamp", DateTime.UtcNow },
                    { "data_quality", 0.75 } // Lower quality data
                },
                // Normal transaction
                new Dictionary<string, object>
                {
                    { "id", "TXN_003" },
                    { "card_id", "CARD_BATCH_C" },
                    { "amount", 35.00 },
                    { "merchant", "Coffee Shop" },
                    { "location", "Work" },
                    { "timestamp", DateTime.UtcNow },
                    { "data_quality", 0.96 }
                },
                // High risk: high amount + poor data quality
                new Dictionary<string, object>
                {
                    { "id", "TXN_004" },
                    { "card_id", "CARD_BATCH_D" },
                    { "amount", 4200.00 },
                    { "merchant", "Electronics Store" },
                    { "location", "Unknown" },
                    { "timestamp", DateTime.UtcNow },
                    { "data_quality", 0.50 } // Very poor data
                },
                // Normal transaction
                new Dictionary<string, object>
                {
                    { "id", "TXN_005" },
                    { "card_id", "CARD_BATCH_E" },
                    { "amount", 12.99 },
                    { "merchant", "Subscription Service" },
                    { "location", "Online" },
                    { "timestamp", DateTime.UtcNow },
                    { "data_quality", 0.88 }
                }
            };

            _logger.LogInformation($"Processing {transactions.Count} transactions in batch...\n");

            var blockedCount = 0;
            foreach (var txn in transactions)
            {
                var decision = await _council.AnalyzeFraudAsync(txn);
                _logger.LogInformation(
                    $"[{txn["id"]}] {decision.Action} | Risk={decision.RiskLevel} | " +
                    $"Virtue={decision.VirtueProfile.Average:F2}");

                if (decision.Action == "BLOCK") blockedCount++;
            }

            _logger.LogInformation($"\nBatch Summary: {blockedCount} blocked, {transactions.Count - blockedCount} allowed");
        }

        /// <summary>
        /// Scenario 4: System Health and Regeneration
        /// Show how Aegis memory tracks system volatility and regenerates.
        /// </summary>
        public async Task RunSystemHealthScenarioAsync()
        {
            _logger.LogInformation("\n=== SCENARIO 4: System Health & Regeneration ===");

            // Phase 1: Stable operation
            _logger.LogInformation("\n[Phase 1] Stable operation - creating snapshot");
            var health1 = _council.GetSystemHealth();
            _logger.LogInformation(
                $"Health: volatility={health1.Volatility:F3}, " +
                $"virtue={health1.AverageVirtue:F3}, entries={health1.TotalEntries}");

            var snap1 = _council.CreateSnapshot();
            if (snap1 != null)
            {
                _logger.LogInformation($"? Snapshot created: {snap1.Id}");
            }

            // Phase 2: Add some transactions
            _logger.LogInformation("\n[Phase 2] Processing transactions");
            for (int i = 0; i < 10; i++)
            {
                var txn = new Dictionary<string, object>
                {
                    { "id", $"TXN_HEALTH_{i}" },
                    { "card_id", $"CARD_{i}" },
                    { "amount", 50.0 + (i * 10) },
                    { "merchant", "Test Merchant" },
                    { "location", "Test Location" },
                    { "timestamp", DateTime.UtcNow },
                    { "data_quality", 0.85 }
                };

                await _council.AnalyzeFraudAsync(txn);
            }

            var health2 = _council.GetSystemHealth();
            _logger.LogInformation(
                $"Health after transactions: volatility={health2.Volatility:F3}, " +
                $"virtue={health2.AverageVirtue:F3}, entries={health2.TotalEntries}");

            // Phase 3: Check if system would trigger regeneration
            _logger.LogInformation("\n[Phase 3] Regeneration check");
            if (health2.Volatility > 0.6)
            {
                _logger.LogWarning("? High volatility detected - regeneration would be triggered");
                var regenerated = _council.Regenerate();
                if (regenerated != null)
                {
                    _logger.LogInformation($"? System regenerated to snapshot {regenerated.Id}");
                }
            }
            else
            {
                _logger.LogInformation("? System stable - no regeneration needed");
            }
        }

        /// <summary>
        /// Scenario 5: Stream Council Integration
        /// Show how all agents work together on generic stream data.
        /// </summary>
        public async Task RunStreamCouncilIntegrationScenarioAsync()
        {
            _logger.LogInformation("\n=== SCENARIO 5: Full Stream Council Integration ===");

            var streamData = new Dictionary<string, object>
            {
                { "sensor_id", "SENSOR_001" },
                { "temperature", 72.5 },
                { "humidity", 45.0 },
                { "pressure", 1013.25 },
                { "status", "normal" },
                { "values", new List<double> { 70.0, 71.5, 72.0, 72.5, 73.0 } }
            };

            _logger.LogInformation("\n[Input] Sensor data with multiple features");
            var decision = await _council.AnalyzeStreamAsync(streamData, topic: "iot-sensors");

            _logger.LogInformation($"\n[Council Decision]");
            _logger.LogInformation($"  Action: {decision.Action}");
            _logger.LogInformation($"  Volatility: {decision.SystemVolatility:F3}");
            _logger.LogInformation($"  Virtue Profile:");
            _logger.LogInformation($"    - Compassion: {decision.AggregateVirtue.Compassion:F2}");
            _logger.LogInformation($"    - Integrity: {decision.AggregateVirtue.Integrity:F2}");
            _logger.LogInformation($"    - Courage: {decision.AggregateVirtue.Courage:F2}");
            _logger.LogInformation($"    - Wisdom: {decision.AggregateVirtue.Wisdom:F2}");
            _logger.LogInformation($"  Processing Time: {decision.ProcessingTimeMs:F2}ms");
        }

        // Helper methods

        private void LogFraudDecision(FraudAnalysisDecision decision)
        {
            _logger.LogInformation($"  Action: {decision.Action}");
            _logger.LogInformation($"  Risk Level: {decision.RiskLevel}");
            _logger.LogInformation($"  Virtue Profile: {decision.VirtueProfile}");
            _logger.LogInformation($"  Fraud Indicators:");
            foreach (var (key, value) in decision.FraudIndicators)
            {
                _logger.LogInformation($"    - {key}: {value:F3}");
            }
            _logger.LogInformation($"  Explanation: {decision.Explanation}");
        }

        public async Task RunAllScenariosAsync()
        {
            _logger.LogInformation("\n" + new string('=', 80));
            _logger.LogInformation("AEGIS STREAM COUNCIL - COMPREHENSIVE DEMO");
            _logger.LogInformation("Demonstrating Multi-Agent Fraud Detection & Real-Time Analysis");
            _logger.LogInformation(new string('=', 80));

            try
            {
                await RunImpossibleTravelScenarioAsync();
                await RunAnomalousAmountScenarioAsync();
                await RunBatchFraudAnalysisScenarioAsync();
                await RunSystemHealthScenarioAsync();
                await RunStreamCouncilIntegrationScenarioAsync();

                _logger.LogInformation("\n" + new string('=', 80));
                _logger.LogInformation("? ALL SCENARIOS COMPLETED SUCCESSFULLY");
                _logger.LogInformation(new string('=', 80));
            }
            catch (Exception ex)
            {
                _logger.LogError($"\n? Demo failed: {ex.Message}");
                throw;
            }
        }
    }
}
