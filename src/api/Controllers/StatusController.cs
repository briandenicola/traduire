using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.Encodings.Web;
using Dapr;
using Dapr.Client;
using Microsoft.Extensions.Logging;

using transcription.models;
using transcription.api.dapr;

namespace transcription.Controllers
{
    [Route("api/status")]
    [ApiController]
    public class StatusController : ControllerBase
    {
        private readonly ILogger _logger;
        private static DaprTranscriptionService _client;
        private static ActivitySource _traduireActivitySource;
        private static Meter _traduireApiMeter;
        private readonly Counter<int> apiCount;
    
        public StatusController(ILogger<StatusController> logger, DaprTranscriptionService client, ActivitySource traduireActivitySource, Meter traduireApiMeter)
        {
            _logger = logger;
            _client = client;
            _traduireActivitySource = traduireActivitySource;
            _traduireApiMeter = traduireApiMeter;

            apiCount = _traduireApiMeter.CreateCounter<int>("traduire.api.count", description: "Counts the times the API is called");
        }

        [HttpGet("{TranscriptionId}")]
        public async Task<ActionResult> Get(string TranscriptionId, CancellationToken cancellationToken)
        {
            using var activity = _traduireActivitySource.StartActivity("StatusController.GetActivity");
                        
            _logger.LogInformation("{TranscriptionId}. Status API Called", TranscriptionId);

            var state = await _client.GetState(TranscriptionId);
            if (state == null) {
                return NotFound();
            }

            apiCount.Add(1);
            _logger.LogInformation("{TranscriptionId}. Current status is {Status}", TranscriptionId, state.Status);
            return Ok(new { TranscriptionId, StatusMessage = state.Status, LastUpdated = state.LastUpdateTime });
        }
    }
}