using ConfluentBot.Services.AegisMemory;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConfluentBot.Controllers
{
    /// <summary>
    /// API endpoints for Aegis stream council analysis.
    /// Demonstrates multi-agent fraud detection, health monitoring, and council decisions.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AegisCouncilController : ControllerBase
    {
        private readonly AegisStreamCouncil _council;

        public AegisCouncilController(AegisStreamCouncil council)
        {
            _council = council;
        }

        /// <summary>
        /// Analyze a transaction for fraud using the Aegis council.
        /// </summary>
        /// <param name="transaction">Transaction data with id, card_id, amount, merchant, location</param>
        [HttpPost("analyze/fraud")]
        [ProducesResponseType(typeof(FraudAnalysisDecision), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> AnalyzeFraud([FromBody] Dictionary<string, object> transaction)
        {
            if (transaction == null || !transaction.ContainsKey("card_id"))
            {
                return BadRequest(new { error = "Transaction must contain 'card_id'" });
            }

            var decision = await _council.AnalyzeFraudAsync(transaction);
            return Ok(decision);
        }

        /// <summary>
        /// Analyze stream data using all agents in the council.
        /// </summary>
        [HttpPost("analyze/stream")]
        [ProducesResponseType(typeof(CouncilDecision), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> AnalyzeStream(
            [FromBody] Dictionary<string, object> streamData,
            [FromQuery] string topic = "unknown")
        {
            if (streamData == null || streamData.Count == 0)
            {
                return BadRequest(new { error = "Stream data required" });
            }

            var decision = await _council.AnalyzeStreamAsync(streamData, topic);
            return Ok(decision);
        }

        /// <summary>
        /// Get current system health metrics.
        /// </summary>
        [HttpGet("health")]
        [ProducesResponseType(typeof(SystemHealth), 200)]
        public IActionResult GetHealth()
        {
            var health = _council.GetSystemHealth();
            return Ok(new
            {
                health.Volatility,
                health.AverageVirtue,
                health.Density,
                health.TotalEntries,
                health.DecayedEntries,
                status = health.Volatility > 0.6 ? "DEGRADED" : health.Volatility > 0.3 ? "CAUTION" : "HEALTHY"
            });
        }

        /// <summary>
        /// Manually trigger a system snapshot for recovery purposes.
        /// </summary>
        [HttpPost("snapshot")]
        [ProducesResponseType(typeof(object), 200)]
        public IActionResult CreateSnapshot()
        {
            var snap = _council.CreateSnapshot();
            return Ok(new
            {
                success = snap != null,
                snapshot = snap != null ? new
                {
                    snap.Id,
                    snap.CreatedAt,
                    snap.EntryCount,
                    snap.AverageVirtue,
                    snap.Volatility
                } : null
            });
        }

        /// <summary>
        /// Manually trigger system regeneration (revert to best snapshot).
        /// </summary>
        [HttpPost("regenerate")]
        [ProducesResponseType(typeof(object), 200)]
        public IActionResult Regenerate()
        {
            var snap = _council.Regenerate();
            return Ok(new
            {
                success = snap != null,
                snapshot = snap != null ? new
                {
                    snap.Id,
                    snap.CreatedAt,
                    snap.EntryCount,
                    snap.AverageVirtue,
                    snap.Volatility
                } : null
            });
        }

        /// <summary>
        /// Batch fraud analysis for multiple transactions.
        /// </summary>
        [HttpPost("analyze/fraud-batch")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> AnalyzeFraudBatch(
            [FromBody] List<Dictionary<string, object>> transactions)
        {
            if (transactions == null || transactions.Count == 0)
            {
                return BadRequest(new { error = "Transactions list required" });
            }

            var decisions = new List<FraudAnalysisDecision>();
            foreach (var txn in transactions)
            {
                var decision = await _council.AnalyzeFraudAsync(txn);
                decisions.Add(decision);
            }

            var blocked = decisions.Where(d => d.Action == "BLOCK").Count();
            var allowed = decisions.Where(d => d.Action == "ALLOW").Count();
            var avgVirtue = decisions.Average(d => d.VirtueProfile.Average);

            return Ok(new
            {
                total = decisions.Count,
                blocked,
                allowed,
                avg_virtue = avgVirtue,
                decisions
            });
        }
    }
}
