using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ConfluentBot.Services.AegisMemory
{
    /// <summary>
    /// Fraud Detection Agent: Multi-faceted fraud scoring combining:
    /// - Transaction velocity (same card in different locations)
    /// - Anomaly detection (unusual amount/merchant)
    /// - Historical patterns (repeat fraud indicators)
    /// - Data quality validation
    /// 
    /// Returns confidence as multi-dimensional virtue profile instead of single score.
    /// </summary>
    public class FraudDetectionAgent : StreamAgent
    {
        public class FraudIndicators
        {
            public double VelocityScore { get; set; }      // Same card multiple locations quickly
            public double AmountAnomalyScore { get; set; } // Unusual transaction amount
            public double MerchantAnomalyScore { get; set; } // Unusual merchant category
            public double HistoryScore { get; set; }        // Account history risk
            public bool IsBlocked { get; set; }
        }

        // Configuration
        private const double HIGH_VELOCITY_THRESHOLD = 0.7;
        private const double HIGH_ANOMALY_THRESHOLD = 0.75;
        private const double BLOCK_THRESHOLD = 0.85; // Virtue-based blocking decision

        private readonly Dictionary<string, TransactionHistory> _history = new();
        private readonly object _historyLock = new();

        public FraudDetectionAgent(RegenerativeMemory memory, ILogger logger)
            : base(nameof(FraudDetectionAgent), memory, logger)
        {
        }

        public override async Task<AgentResult> AnalyzeAsync(Dictionary<string, object> inputData)
        {
            return await Task.Run(() =>
            {
                var txn = ParseTransaction(inputData);
                if (txn == null)
                {
                    return CreateErrorResult("Invalid transaction format");
                }

                var indicators = ComputeFraudIndicators(txn);
                var virtue = ComputeVirtueProfile(indicators, txn);

                var action = indicators.IsBlocked ? "BLOCK" : "ALLOW";
                var riskLevel = indicators.IsBlocked ? "CRITICAL" : 
                                indicators.AmountAnomalyScore > 0.6 ? "HIGH" : 
                                indicators.VelocityScore > 0.5 ? "MEDIUM" : "LOW";

                var result = new AgentResult
                {
                    AgentName = Name,
                    Data = new Dictionary<string, object>
                    {
                        { "transaction_id", txn.Id },
                        { "card_id", txn.CardId },
                        { "amount", Math.Round(txn.Amount, 2) },
                        { "merchant", txn.Merchant },
                        { "location", txn.Location },
                        { "velocity_score", Math.Round(indicators.VelocityScore, 3) },
                        { "amount_anomaly", Math.Round(indicators.AmountAnomalyScore, 3) },
                        { "merchant_anomaly", Math.Round(indicators.MerchantAnomalyScore, 3) },
                        { "history_score", Math.Round(indicators.HistoryScore, 3) },
                        { "risk_level", riskLevel },
                        { "action", action },
                        { "reason", GetDecisionReason(indicators) }
                    },
                    VirtueProfile = virtue,
                    Explanation = $"Fraud Analysis: {action} | Risk={riskLevel} | " +
                                 $"Virtue={virtue} | Velocity={indicators.VelocityScore:F2} " +
                                 $"Amount={indicators.AmountAnomalyScore:F2}"
                };

                RecordAnalysis(txn.CardId, result);
                LogAnalysis(result);
                UpdateHistory(txn, indicators);

                return result;
            });
        }

        private FraudIndicators ComputeFraudIndicators(Transaction txn)
        {
            var indicators = new FraudIndicators();

            // 1. VELOCITY SCORING: Same card multiple locations in short time
            indicators.VelocityScore = ComputeVelocityScore(txn);

            // 2. AMOUNT ANOMALY: Is this amount unusual for this card?
            indicators.AmountAnomalyScore = ComputeAmountAnomalyScore(txn);

            // 3. MERCHANT ANOMALY: Is this merchant unusual for this card?
            indicators.MerchantAnomalyScore = ComputeMerchantAnomalyScore(txn);

            // 4. HISTORY SCORE: Account history risk
            indicators.HistoryScore = ComputeHistoryScore(txn);

            // 5. BLOCKING DECISION: Aggregate scores into confidence
            var aggregateRisk = (indicators.VelocityScore * 0.35 +
                                 indicators.AmountAnomalyScore * 0.25 +
                                 indicators.MerchantAnomalyScore * 0.20 +
                                 indicators.HistoryScore * 0.20);

            indicators.IsBlocked = aggregateRisk > BLOCK_THRESHOLD;

            return indicators;
        }

        private double ComputeVelocityScore(Transaction txn)
        {
            lock (_historyLock)
            {
                if (!_history.TryGetValue(txn.CardId, out var history))
                {
                    history = new TransactionHistory();
                    _history[txn.CardId] = history;
                }

                // Check if same card used in 2 different locations within 30 minutes
                var recent = history.Transactions
                    .Where(t => (txn.Timestamp - t.Timestamp).TotalMinutes < 30)
                    .ToList();

                if (recent.Any(t => t.Location != txn.Location))
                {
                    return 0.9; // Impossible travel
                }

                // Check transaction frequency
                var lastHourCount = history.Transactions
                    .Count(t => (txn.Timestamp - t.Timestamp).TotalHours < 1);

                if (lastHourCount > 5)
                {
                    return 0.7; // High frequency
                }

                return Math.Min(lastHourCount / 10.0, 0.5); // Normal velocity
            }
        }

        private double ComputeAmountAnomalyScore(Transaction txn)
        {
            lock (_historyLock)
            {
                if (!_history.TryGetValue(txn.CardId, out var history) || !history.Transactions.Any())
                {
                    return 0.2; // No history = slight caution
                }

                var avgAmount = history.Transactions.Average(t => t.Amount);
                var stdDev = Math.Sqrt(history.Transactions
                    .Average(t => Math.Pow(t.Amount - avgAmount, 2)));

                if (stdDev == 0)
                {
                    stdDev = avgAmount * 0.1; // Avoid division by zero
                }

                var zScore = Math.Abs((txn.Amount - avgAmount) / stdDev);

                // Z-score > 3 is highly anomalous (3 std devs from mean)
                return Math.Min(zScore / 3.0, 1.0);
            }
        }

        private double ComputeMerchantAnomalyScore(Transaction txn)
        {
            lock (_historyLock)
            {
                if (!_history.TryGetValue(txn.CardId, out var history) || !history.Transactions.Any())
                {
                    return 0.1;
                }

                var merchantCount = history.Transactions
                    .Count(t => t.Merchant == txn.Merchant);

                // If merchant is new to this card, slightly more suspicious
                if (merchantCount == 0)
                {
                    return 0.3;
                }

                // Repeat merchants are lower risk
                return Math.Max(0.0, 0.5 - (merchantCount / 10.0));
            }
        }

        private double ComputeHistoryScore(Transaction txn)
        {
            lock (_historyLock)
            {
                if (!_history.TryGetValue(txn.CardId, out var history))
                {
                    return 0.2; // New account = slight risk
                }

                var accountAge = DateTime.UtcNow - history.CreatedAt;
                var fraudRate = history.FraudCount / Math.Max(1, history.Transactions.Count);

                var ageScore = Math.Min(accountAge.TotalDays / 365.0, 1.0); // Older = lower risk
                var fraudScore = fraudRate > 0.05 ? 0.8 : fraudRate > 0.01 ? 0.4 : 0.1;

                return (ageScore * 0.6 + (1.0 - fraudScore) * 0.4);
            }
        }

        private VirtueProfile ComputeVirtueProfile(FraudIndicators indicators, Transaction txn)
        {
            // Virtue is based on confidence in the decision, not the risk itself

            var integrity = 0.0;
            var compassion = 0.0;
            var courage = 0.0;
            var wisdom = 0.0;

            // INTEGRITY: How clean/valid is the data?
            integrity = (txn.DataQuality >= 0.8) ? 0.95 : (txn.DataQuality >= 0.6) ? 0.7 : 0.4;

            // COMPASSION: Are we being fair? (avoiding false positives)
            var riskScore = (indicators.VelocityScore * 0.35 + 
                            indicators.AmountAnomalyScore * 0.25 +
                            indicators.MerchantAnomalyScore * 0.20 +
                            indicators.HistoryScore * 0.20);
            compassion = Math.Max(0.4, 1.0 - (riskScore * 0.8)); // High risk = lower compassion (block needed)

            // COURAGE: Are we confident enough to act?
            courage = Math.Min(1.0, indicators.VelocityScore + indicators.AmountAnomalyScore) * 0.9 + 0.1;

            // WISDOM: Sufficiency of evidence
            wisdom = (integrity + courage + (1.0 - indicators.HistoryScore)) / 3.0;

            return new VirtueProfile
            {
                Integrity = Math.Round(integrity, 3),
                Compassion = Math.Round(compassion, 3),
                Courage = Math.Round(courage, 3),
                Wisdom = Math.Round(wisdom, 3)
            };
        }

        private string GetDecisionReason(FraudIndicators indicators)
        {
            if (indicators.VelocityScore > HIGH_VELOCITY_THRESHOLD)
            {
                return "Impossible travel detected";
            }

            if (indicators.AmountAnomalyScore > HIGH_ANOMALY_THRESHOLD)
            {
                return "Unusual transaction amount";
            }

            if (indicators.MerchantAnomalyScore > 0.7)
            {
                return "Unknown merchant for this card";
            }

            if (indicators.HistoryScore > 0.7)
            {
                return "Account history risk";
            }

            return "Transaction approved";
        }

        private void UpdateHistory(Transaction txn, FraudIndicators indicators)
        {
            lock (_historyLock)
            {
                if (!_history.TryGetValue(txn.CardId, out var history))
                {
                    history = new TransactionHistory();
                    _history[txn.CardId] = history;
                }

                history.Transactions.Add(txn);
                if (indicators.IsBlocked)
                {
                    history.FraudCount++;
                }

                // Keep only last 100 transactions per card
                if (history.Transactions.Count > 100)
                {
                    history.Transactions.RemoveAt(0);
                }
            }
        }

        private AgentResult CreateErrorResult(string message)
        {
            return new AgentResult
            {
                AgentName = Name,
                Explanation = message,
                Data = new Dictionary<string, object> { { "error", message } }
            };
        }

        private Transaction? ParseTransaction(Dictionary<string, object> inputData)
        {
            try
            {
                return new Transaction
                {
                    Id = inputData.TryGetValue("id", out var id) ? id.ToString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString(),
                    CardId = inputData.TryGetValue("card_id", out var cid) ? cid.ToString() ?? "" : "",
                    Amount = inputData.TryGetValue("amount", out var amt) && double.TryParse(amt.ToString(), out var a) ? a : 0,
                    Merchant = inputData.TryGetValue("merchant", out var m) ? m.ToString() ?? "" : "",
                    Location = inputData.TryGetValue("location", out var l) ? l.ToString() ?? "" : "",
                    Timestamp = inputData.TryGetValue("timestamp", out var ts) && DateTime.TryParse(ts.ToString(), out var t) ? t : DateTime.UtcNow,
                    DataQuality = inputData.TryGetValue("data_quality", out var dq) && double.TryParse(dq.ToString(), out var q) ? q : 0.8
                };
            }
            catch
            {
                return null;
            }
        }

        private class Transaction
        {
            public string Id { get; set; } = string.Empty;
            public string CardId { get; set; } = string.Empty;
            public double Amount { get; set; }
            public string Merchant { get; set; } = string.Empty;
            public string Location { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; } = DateTime.UtcNow;
            public double DataQuality { get; set; } = 0.8;
        }

        private class TransactionHistory
        {
            public List<Transaction> Transactions { get; set; } = new();
            public int FraudCount { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        }
    }
}
