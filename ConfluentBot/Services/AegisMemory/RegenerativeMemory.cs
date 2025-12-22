using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace ConfluentBot.Services.AegisMemory
{
    /// <summary>
    /// Regenerative Memory inspired by Turritopsis dohrnii (immortal jellyfish).
    /// Entries decay over time; system can snapshot healthy states and regenerate when volatility spikes.
    /// </summary>
    public class MemoryEntry
    {
        public string Key { get; set; } = string.Empty;
        public object? Value { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public double EmotionWeight { get; set; } = 0.5; // 0-1, higher = protected from decay
        public double VirtueScore { get; set; } = 0.5;   // 0-1, quality/importance indicator

        public double GetAgeDays(DateTime? now = null)
        {
            var reference = now ?? DateTime.UtcNow;
            return (reference - Timestamp).TotalDays;
        }

        public bool IsDecayed(double baseDecayDays, DateTime? now = null)
        {
            var age = GetAgeDays(now);
            var lifetime = baseDecayDays * (0.5 + EmotionWeight);
            return age > lifetime;
        }
    }

    public class SystemSnapshot
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string StateHash { get; set; } = string.Empty;
        public int EntryCount { get; set; }
        public double AverageVirtue { get; set; }
        public double Volatility { get; set; }
        public Dictionary<string, MemoryEntry> State { get; set; } = new();
    }

    public class SystemHealth
    {
        public double Volatility { get; set; }        // Fraction of decayed entries
        public double AverageVirtue { get; set; }     // Mean virtue score of non-decayed entries
        public double Density { get; set; }           // (total - decayed) / max_entries
        public int TotalEntries { get; set; }
        public int DecayedEntries { get; set; }
    }

    public class RegenerativeMemory
    {
        private readonly ConcurrentDictionary<string, MemoryEntry> _store;
        private readonly ReaderWriterLockSlim _lock = new();
        private readonly ILogger<RegenerativeMemory> _logger;

        // Configuration
        public int MaxEntries { get; }
        public double BaseDecayDays { get; }
        public double VolatilityThreshold { get; }
        public double StabilityThreshold { get; }
        public int MinEntriesForSnapshot { get; }
        public int MaxSnapshots { get; }

        // Snapshots
        public List<SystemSnapshot> Snapshots { get; } = new();
        private readonly ReaderWriterLockSlim _snapshotLock = new();

        public RegenerativeMemory(
            ILogger<RegenerativeMemory> logger,
            int maxEntries = 10000,
            double baseDecayDays = 30.0,
            double volatilityThreshold = 0.6,
            double stabilityThreshold = 0.2,
            int minEntriesForSnapshot = 5,
            int maxSnapshots = 10)
        {
            _logger = logger;
            _store = new ConcurrentDictionary<string, MemoryEntry>();
            MaxEntries = maxEntries;
            BaseDecayDays = baseDecayDays;
            VolatilityThreshold = volatilityThreshold;
            StabilityThreshold = stabilityThreshold;
            MinEntriesForSnapshot = minEntriesForSnapshot;
            MaxSnapshots = maxSnapshots;
        }

        public string Write(string key, object value, double emotionWeight = 0.5, double virtueScore = 0.5)
        {
            emotionWeight = Math.Clamp(emotionWeight, 0.0, 1.0);
            virtueScore = Math.Clamp(virtueScore, 0.0, 1.0);
            var hashedKey = HashKey(key);

            var entry = new MemoryEntry
            {
                Key = key,
                Value = value,
                Timestamp = DateTime.UtcNow,
                EmotionWeight = emotionWeight,
                VirtueScore = virtueScore
            };

            _lock.EnterWriteLock();
            try
            {
                if (_store.Count >= MaxEntries)
                {
                    var oldest = _store.Values.OrderBy(e => e.Timestamp).First();
                    _store.TryRemove(HashKey(oldest.Key), out _);
                    _logger.LogInformation($"Evicted oldest entry: {oldest.Key}");
                }

                _store.AddOrUpdate(hashedKey, entry, (_, __) => entry);
                _logger.LogDebug($"Stored key={key} value_type={value?.GetType().Name}");
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            return hashedKey;
        }

        public T? Read<T>(string key) where T : class
        {
            var hashedKey = HashKey(key);

            _lock.EnterReadLock();
            try
            {
                if (_store.TryGetValue(hashedKey, out var entry))
                {
                    if (entry.IsDecayed(BaseDecayDays))
                    {
                        _lock.ExitReadLock();
                        _lock.EnterWriteLock();
                        try
                        {
                            _store.TryRemove(hashedKey, out _);
                            _logger.LogInformation($"Entry decayed; removed key={key}");
                        }
                        finally
                        {
                            _lock.ExitWriteLock();
                        }
                        return null;
                    }

                    return entry.Value as T;
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }

            return null;
        }

        public SystemHealth ComputeHealth()
        {
            var now = DateTime.UtcNow;
            _lock.EnterReadLock();
            try
            {
                if (_store.Count == 0)
                {
                    return new SystemHealth
                    {
                        Volatility = 0.0,
                        AverageVirtue = 0.0,
                        Density = 0.0,
                        TotalEntries = 0,
                        DecayedEntries = 0
                    };
                }

                int decayedCount = 0;
                var virtueValues = new List<double>();

                foreach (var entry in _store.Values)
                {
                    if (entry.IsDecayed(BaseDecayDays, now))
                    {
                        decayedCount++;
                    }
                    else
                    {
                        virtueValues.Add(entry.VirtueScore);
                    }
                }

                var total = _store.Count;
                var volatility = total > 0 ? decayedCount / (double)total : 0.0;
                var avgVirtue = virtueValues.Any() ? virtueValues.Average() : 0.0;
                var density = total / (double)MaxEntries;

                return new SystemHealth
                {
                    Volatility = volatility,
                    AverageVirtue = avgVirtue,
                    Density = density,
                    TotalEntries = total,
                    DecayedEntries = decayedCount
                };
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public Dictionary<string, object> Audit(int limit = 20)
        {
            var now = DateTime.UtcNow;
            var result = new Dictionary<string, object>();

            _lock.EnterReadLock();
            try
            {
                var i = 0;
                foreach (var (k, e) in _store.Take(limit))
                {
                    result[k] = new
                    {
                        key = e.Key,
                        timestamp = e.Timestamp.ToString("O"),
                        ageDays = Math.Round(e.GetAgeDays(now), 3),
                        emotionWeight = e.EmotionWeight,
                        virtueScore = e.VirtueScore,
                        decayed = e.IsDecayed(BaseDecayDays, now)
                    };
                    if (++i >= limit) break;
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }

            return result;
        }

        public SystemSnapshot? CreateSnapshot(SystemHealth? health = null)
        {
            health ??= ComputeHealth();

            _lock.EnterReadLock();
            try
            {
                if (_store.Count < MinEntriesForSnapshot)
                {
                    _logger.LogInformation($"Not enough entries for snapshot ({_store.Count} < {MinEntriesForSnapshot})");
                    return null;
                }

                var stateCopy = new Dictionary<string, MemoryEntry>(_store);
                _lock.ExitReadLock();

                var stateHash = ComputeStateHash(stateCopy);
                var snap = new SystemSnapshot
                {
                    CreatedAt = DateTime.UtcNow,
                    StateHash = stateHash,
                    EntryCount = stateCopy.Count,
                    AverageVirtue = health.AverageVirtue,
                    Volatility = health.Volatility,
                    State = stateCopy
                };

                _snapshotLock.EnterWriteLock();
                try
                {
                    Snapshots.Add(snap);
                    if (Snapshots.Count > MaxSnapshots)
                    {
                        var removed = Snapshots[0];
                        Snapshots.RemoveAt(0);
                        _logger.LogInformation($"Discarded oldest snapshot: {removed.Id}");
                    }
                }
                finally
                {
                    _snapshotLock.ExitWriteLock();
                }

                _logger.LogInformation(
                    $"Created snapshot: entries={snap.EntryCount} volatility={snap.Volatility:F3} virtue={snap.AverageVirtue:F3}");
                return snap;
            }
            finally
            {
                if (_lock.IsReadLockHeld) _lock.ExitReadLock();
            }
        }

        public SystemSnapshot? Regenerate()
        {
            _snapshotLock.EnterReadLock();
            try
            {
                if (!Snapshots.Any())
                {
                    _logger.LogWarning("Regeneration requested but no snapshots available");
                    return null;
                }

                var best = Snapshots
                    .OrderBy(s => s.Volatility)
                    .ThenByDescending(s => s.AverageVirtue)
                    .First();

                _snapshotLock.ExitReadLock();

                _lock.EnterWriteLock();
                try
                {
                    _store.Clear();
                    foreach (var (k, v) in best.State)
                    {
                        _store.TryAdd(k, v);
                    }

                    _logger.LogWarning(
                        $"LIFECYCLE_REVERT: Memory regenerated to snapshot {best.Id} " +
                        $"(entries={best.EntryCount} volatility={best.Volatility:F3})");
                    return best;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
            finally
            {
                if (_snapshotLock.IsReadLockHeld) _snapshotLock.ExitReadLock();
            }
        }

        public Dictionary<string, object> RegenerativeCycle(SystemHealth? health = null, double virtueGate = 0.5)
        {
            health ??= ComputeHealth();
            var action = "none";
            string? snapshotHash = null;

            if (health.Volatility >= VolatilityThreshold)
            {
                var snap = Regenerate();
                action = snap != null ? "regenerated" : "regeneration_failed";
                snapshotHash = snap?.StateHash;
            }
            else if (health.Volatility <= StabilityThreshold && health.AverageVirtue >= virtueGate)
            {
                var snap = CreateSnapshot(health);
                action = snap != null ? "snapshot_created" : "snapshot_skipped";
                snapshotHash = snap?.StateHash;
            }

            return new Dictionary<string, object>
            {
                { "action", action },
                { "volatility", Math.Round(health.Volatility, 4) },
                { "avg_virtue", Math.Round(health.AverageVirtue, 4) },
                { "density", Math.Round(health.Density, 4) },
                { "snapshot_hash", snapshotHash ?? "null" }
            };
        }

        private static string HashKey(string key)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
                return Convert.ToHexString(hashedBytes);
            }
        }

        private static string ComputeStateHash(Dictionary<string, MemoryEntry> state)
        {
            var serializable = new Dictionary<string, object>();
            foreach (var (k, v) in state)
            {
                serializable[k] = new
                {
                    timestamp = v.Timestamp.ToString("O"),
                    emotion_weight = v.EmotionWeight,
                    virtue_score = v.VirtueScore
                };
            }

            var json = System.Text.Json.JsonSerializer.Serialize(serializable);
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
                return Convert.ToHexString(hashedBytes);
            }
        }
    }
}
