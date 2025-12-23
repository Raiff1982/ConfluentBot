using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ConfluentBot.Services.AegisMemory;

namespace ConfluentBot.Services.NexisIntegration
{
    /// <summary>
    /// NexisSignalAgent bridges Nexis multi-perspective reasoning with Aegis virtue-based decisions.
    /// 
    /// Integration of Nexis Signal Engine concepts:
    /// - Colleen: Vector transformation (courage/certainty)
    /// - Luke: Ethical alignment + entropy analysis (wisdom/integrity)
    /// - Kellyanne: Harmonic resonance (compassion/harmony)
    /// 
    /// Yields virtue profile instead of raw Nexis verdict.
    /// </summary>
    public class NexisSignalAgent : StreamAgent
    {
        public class NexisIntentVector
        {
            public double SuspicionScore { get; set; }    // 0-1: Risk indicators detected
            public double EntropyIndex { get; set; }      // 0-1: Chaos/disorder level
            public string EthicalAlignment { get; set; }  // "aligned" or "unaligned"
            public double HarmonicVolatility { get; set; } // 0-1: Pattern instability
            public string PreCorruptionRisk { get; set; } // "high" or "low"
        }

        public class PerspectiveAnalysis
        {
            public string CoilleenVector { get; set; }    // Rotated vector (transformation)
            public string LukeEthics { get; set; }        // Ethical evaluation
            public string KellyAnnHarmonics { get; set; } // Harmonic profile
        }

        // Ethical and entropic term vocabularies
        private static readonly string[] EthicalTerms = 
        { 
            "hope", "truth", "resonance", "repair", "grace", "resolve", 
            "integrity", "compassion", "courage", "wisdom"
        };

        private static readonly string[] EntropicTerms = 
        { 
            "corruption", "instability", "malice", "chaos", "exploit", 
            "manipulate", "bypass", "infect", "override", "fraud"
        };

        private static readonly string[] RiskTerms = 
        { 
            "manipulate", "exploit", "bypass", "infect", "override", 
            "steal", "hack", "deceive", "mislead", "forge"
        };

        public NexisSignalAgent(RegenerativeMemory memory, ILogger logger)
            : base(nameof(NexisSignalAgent), memory, logger)
        {
        }

        public override async Task<AgentResult> AnalyzeAsync(Dictionary<string, object> inputData)
        {
            return await Task.Run(() =>
            {
                // Handle both transaction and stream message formats
                var payload = inputData;
                
                // If this is wrapped in a stream message format, unwrap it
                if (inputData.TryGetValue("payload", out var p) && p is Dictionary<string, object>)
                {
                    payload = (Dictionary<string, object>)p;
                }

                // Extract signal from transaction data
                string signal = ExtractSignal(payload);
                
                // Perform Nexis-inspired analysis
                var intentVector = AnalyzeIntentVector(signal, payload);
                var perspectives = AnalyzePerspectives(signal);
                
                // Convert to virtue profile
                var virtue = ConvertToVirtueProfile(intentVector, perspectives);

                var result = new AgentResult
                {
                    AgentName = Name,
                    Data = new Dictionary<string, object>
                    {
                        { "signal", signal },
                        { "suspicion_score", intentVector.SuspicionScore },
                        { "entropy_index", intentVector.EntropyIndex },
                        { "ethical_alignment", intentVector.EthicalAlignment },
                        { "harmonic_volatility", intentVector.HarmonicVolatility },
                        { "pre_corruption_risk", intentVector.PreCorruptionRisk },
                        { "colleen_vector", perspectives.CoilleenVector },
                        { "luke_ethics", perspectives.LukeEthics },
                        { "kellyanne_harmonics", perspectives.KellyAnnHarmonics }
                    },
                    VirtueProfile = virtue,
                    Explanation = $"Nexis Signal Analysis: {intentVector.PreCorruptionRisk} risk | " +
                                 $"Ethical: {intentVector.EthicalAlignment} | " +
                                 $"Entropy: {intentVector.EntropyIndex:F2} | " +
                                 $"Virtue: {virtue}"
                };

                var topic = inputData.TryGetValue("topic", out var t) ? t as string : "transaction";
                RecordAnalysis(topic ?? "unknown", result);
                LogAnalysis(result);
                return result;
            });
        }

        private string ExtractSignal(Dictionary<string, object> payload)
        {
            if (payload == null || payload.Count == 0)
                return "neutral";

            // Try to extract meaningful signal from transaction data
            var signals = new List<string>();

            if (payload.TryGetValue("merchant", out var merchant))
                signals.Add(merchant.ToString() ?? "");
            
            if (payload.TryGetValue("description", out var desc))
                signals.Add(desc.ToString() ?? "");
            
            if (payload.TryGetValue("category", out var cat))
                signals.Add(cat.ToString() ?? "");

            return string.Join(" ", signals.Where(s => !string.IsNullOrEmpty(s)));
        }

        private NexisIntentVector AnalyzeIntentVector(string signal)
        {
            return AnalyzeIntentVector(signal, null);
        }

        private NexisIntentVector AnalyzeIntentVector(string signal, Dictionary<string, object> payload)
        {
            signal = signal.ToLower();
            var words = signal.Split(new[] { ' ', ':', ';', ',' }, StringSplitOptions.RemoveEmptyEntries);

            // Get amount and merchant for contextual analysis
            var amount = GetDoubleValue(payload, "amount", 0.0);
            var merchant = GetStringValue(payload, "merchant", "unknown").ToLower();
            var category = GetStringValue(payload, "category", "unknown").ToLower();

            // Calculate suspicion score (risk terms) - base score from keywords
            var suspicion = 0.0;
            foreach (var word in words)
            {
                foreach (var riskTerm in RiskTerms)
                {
                    if (FuzzyMatch(word, riskTerm, 0.75))
                        suspicion += 0.25;
                }
            }
            
            // Add amount-based suspicion
            if (amount > 10000)
                suspicion += 0.3;
            else if (amount > 5000)
                suspicion += 0.15;
            
            // Check for suspicious merchant keywords
            if (FuzzyMatch(merchant, "crypto", 0.7) || FuzzyMatch(merchant, "exchange", 0.7) || 
                FuzzyMatch(merchant, "anonymous", 0.7))
                suspicion += 0.3;
            
            suspicion = Math.Min(suspicion, 1.0);

            // Calculate entropy (entropic terms presence)
            var entropy = 0.0;
            foreach (var word in words)
            {
                foreach (var entropicTerm in EntropicTerms)
                {
                    if (FuzzyMatch(word, entropicTerm, 0.75))
                        entropy += 0.2;
                }
            }
            entropy = Math.Min(entropy, 1.0);

            // Evaluate ethical alignment
            var ethicalAlignment = "unaligned";
            
            // Check for trusted merchants
            if (FuzzyMatch(merchant, "amazon", 0.8) || FuzzyMatch(merchant, "netflix", 0.8) ||
                FuzzyMatch(merchant, "apple", 0.8) || FuzzyMatch(merchant, "microsoft", 0.8) ||
                FuzzyMatch(merchant, "google", 0.8) || FuzzyMatch(merchant, "paypal", 0.8))
            {
                ethicalAlignment = "aligned";
            }
            else
            {
                // Check for ethical terms in signal
                foreach (var word in words)
                {
                    foreach (var ethicalTerm in EthicalTerms)
                    {
                        if (FuzzyMatch(word, ethicalTerm, 0.75))
                        {
                            ethicalAlignment = "aligned";
                            break;
                        }
                    }
                    if (ethicalAlignment == "aligned") break;
                }
            }

            // Calculate harmonic volatility (entropy + risk combination, adjusted for stability)
            var volatility = (entropy + suspicion) / 2.0;

            // Determine pre-corruption risk based on all factors
            var risk = (suspicion > 0.5 || entropy > 0.6 || volatility > 0.55 || amount > 8000) ? "high" : "low";

            return new NexisIntentVector
            {
                SuspicionScore = Math.Round(suspicion, 3),
                EntropyIndex = Math.Round(entropy, 3),
                EthicalAlignment = ethicalAlignment,
                HarmonicVolatility = Math.Round(volatility, 3),
                PreCorruptionRisk = risk
            };
        }

        private double GetDoubleValue(Dictionary<string, object> dict, string key, double defaultValue)
        {
            if (dict.TryGetValue(key, out var value))
            {
                if (double.TryParse(value.ToString(), out var result))
                    return result;
            }
            return defaultValue;
        }

        private string GetStringValue(Dictionary<string, object> dict, string key, string defaultValue)
        {
            return dict.TryGetValue(key, out var value) ? value.ToString() : defaultValue;
        }

        private PerspectiveAnalysis AnalyzePerspectives(string signal)
        {
            var words = signal.ToLower().Split(new[] { ' ', ':', ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
            
            // Colleen: Vector transformation (abstract/transformed perspective)
            var colleeVector = GenerateVectorSignature(signal);

            // Luke: Ethical evaluation + entropy
            var lukeEthics = EvaluateEthics(words);

            // Kellyanne: Harmonic/resonance profile
            var kellyAnnHarmonics = GenerateHarmonicProfile(signal);

            return new PerspectiveAnalysis
            {
                CoilleenVector = colleeVector,
                LukeEthics = lukeEthics,
                KellyAnnHarmonics = kellyAnnHarmonics
            };
        }

        private string GenerateVectorSignature(string signal)
        {
            // Simple vector representation (could use actual math)
            var hash = signal.GetHashCode();
            var x = (hash % 100) / 100.0;
            var y = ((hash >> 16) % 100) / 100.0;
            return $"[{x:F2}, {y:F2}]";
        }

        private string EvaluateEthics(string[] words)
        {
            var ethicalCount = words.Count(w => EthicalTerms.Any(t => FuzzyMatch(w, t, 0.75)));
            var entropicCount = words.Count(w => EntropicTerms.Any(t => FuzzyMatch(w, t, 0.75)));

            if (ethicalCount > 0 && entropicCount == 0)
                return "stabilized";
            else if (entropicCount > ethicalCount)
                return "diffused";
            else
                return "neutral";
        }

        private string GenerateHarmonicProfile(string signal)
        {
            // Generate a simple harmonic profile based on character frequencies
            var alphaOnly = new string(signal.Where(c => char.IsLetter(c)).ToArray());
            if (alphaOnly.Length == 0)
                return "[0.0, 0.0, 0.0]";

            var freqs = new double[3];
            for (int i = 0; i < Math.Min(3, alphaOnly.Length); i++)
            {
                freqs[i] = (alphaOnly[i] % 13) / 13.0;
            }

            return $"[{freqs[0]:F2}, {freqs[1]:F2}, {freqs[2]:F2}]";
        }

        private VirtueProfile ConvertToVirtueProfile(NexisIntentVector intent, PerspectiveAnalysis perspectives)
        {
            // Convert Nexis analysis to virtue dimensions
            
            // INTEGRITY: Based on ethical alignment and entropy (lower entropy = higher integrity)
            var integrity = intent.EthicalAlignment == "aligned" 
                ? 0.85 + (1.0 - intent.EntropyIndex) * 0.15
                : Math.Max(0.3, 0.5 - intent.EntropyIndex);

            // COMPASSION: Based on ethical alignment and lack of suspicious indicators
            var compassion = intent.EthicalAlignment == "aligned"
                ? 0.8 + (1.0 - intent.SuspicionScore) * 0.2
                : Math.Max(0.2, 0.6 - intent.SuspicionScore * 2);

            // COURAGE: Confidence in the analysis (based on signal strength)
            var courage = 1.0 - intent.HarmonicVolatility;

            // WISDOM: Combination of ethical alignment and low risk
            var wisdom = (integrity + (1.0 - Math.Min(intent.EntropyIndex, 1.0))) / 2.0;

            return new VirtueProfile
            {
                Integrity = Math.Round(Math.Max(0, Math.Min(1, integrity)), 3),
                Compassion = Math.Round(Math.Max(0, Math.Min(1, compassion)), 3),
                Courage = Math.Round(Math.Max(0, Math.Min(1, courage)), 3),
                Wisdom = Math.Round(Math.Max(0, Math.Min(1, wisdom)), 3)
            };
        }

        /// <summary>
        /// Simple fuzzy string matching (Levenshtein-like)
        /// </summary>
        private bool FuzzyMatch(string s1, string s2, double threshold)
        {
            if (s1 == s2) return true;
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return false;

            int distance = LevenshteinDistance(s1, s2);
            int maxLen = Math.Max(s1.Length, s2.Length);
            double similarity = 1.0 - (distance / (double)maxLen);

            return similarity >= threshold;
        }

        private int LevenshteinDistance(string s1, string s2)
        {
            int len1 = s1.Length;
            int len2 = s2.Length;
            var d = new int[len1 + 1, len2 + 1];

            for (int i = 0; i <= len1; i++) d[i, 0] = i;
            for (int j = 0; j <= len2; j++) d[0, j] = j;

            for (int i = 1; i <= len1; i++)
            {
                for (int j = 1; j <= len2; j++)
                {
                    int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[len1, len2];
        }
    }

    /// <summary>
    /// ContextualIntentAgent uses Nexis signal analysis for fraud detection.
    /// Evaluates transaction intent based on ethical vocabulary and harmonic patterns.
    /// </summary>
    public class ContextualIntentAgent : StreamAgent
    {
        private readonly NexisSignalAgent _nexisAgent;

        public ContextualIntentAgent(RegenerativeMemory memory, ILogger logger)
            : base(nameof(ContextualIntentAgent), memory, logger)
        {
            _nexisAgent = new NexisSignalAgent(memory, logger);
        }

        public override async Task<AgentResult> AnalyzeAsync(Dictionary<string, object> inputData)
        {
            // First get Nexis analysis
            var nexisResult = await _nexisAgent.AnalyzeAsync(inputData);

            // Extract Nexis findings
            var preCorruptionRisk = nexisResult.Data.TryGetValue("pre_corruption_risk", out var risk) 
                ? risk.ToString() 
                : "low";
            
            var ethicalAlignment = nexisResult.Data.TryGetValue("ethical_alignment", out var ethics) 
                ? ethics.ToString() 
                : "unaligned";

            var suspicionScore = nexisResult.Data.TryGetValue("suspicion_score", out var susp) 
                && double.TryParse(susp.ToString(), out var suspVal)
                ? suspVal
                : 0.0;

            // Determine intent class
            string intentClass = preCorruptionRisk == "high" ? "suspicious" :
                                 ethicalAlignment == "aligned" ? "benign" :
                                 suspicionScore > 0.5 ? "questionable" : "normal";

            var virtue = new VirtueProfile
            {
                Compassion = ethicalAlignment == "aligned" ? 0.9 : 0.5,
                Integrity = preCorruptionRisk == "high" ? 0.4 : 0.85,
                Courage = 1.0 - suspicionScore,
                Wisdom = ethicalAlignment == "aligned" ? 0.9 : 0.6
            };

            var result = new AgentResult
            {
                AgentName = Name,
                Data = new Dictionary<string, object>
                {
                    { "intent_class", intentClass },
                    { "ethical_alignment", ethicalAlignment },
                    { "pre_corruption_risk", preCorruptionRisk },
                    { "nexis_suspicion", suspicionScore }
                },
                VirtueProfile = virtue,
                Explanation = $"Intent Analysis: {intentClass} transaction | " +
                             $"Ethics: {ethicalAlignment} | " +
                             $"Risk: {preCorruptionRisk}"
            };

            RecordAnalysis(inputData.TryGetValue("topic", out var t) ? t.ToString() : "unknown", result);
            LogAnalysis(result);

            return result;
        }
    }
}
