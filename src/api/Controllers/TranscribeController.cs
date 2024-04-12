using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Dapr.Client;
using Microsoft.Extensions.Logging;

using transcription.models;
using transcription.api.dapr;

namespace transcription.Controllers
{
    [Route("api/transcribe")]
    [ApiController]
    public class TranscribeController : ControllerBase
    {
        private readonly ILogger _logger;
        private static DaprTranscriptionService _client;
        private static ActivitySource _traduireActivitySource;

        public TranscribeController(ILogger<TranscribeController> logger, DaprTranscriptionService client, ActivitySource traduireActivitySource)
        {
            _logger = logger;
            _client = client;
            _traduireActivitySource = traduireActivitySource;
        }

        [HttpPost]
        public async Task<ActionResult> Post(TranscriptionReferenceRequest reference, CancellationToken cancellationToken)
        {
            using var activity = _traduireActivitySource.StartActivity("TranscribeController.PostActivity");

            var TranscriptionId = Guid.NewGuid().ToString();
            
            _logger.LogInformation("{TranscriptionId}. Request to transcribe {blobURL} was received", TranscriptionId, reference.blobURL);
            var state = await _client.UpdateState(TranscriptionId, new Uri(reference.blobURL) );

            _logger.LogInformation("{TranscriptionId}. Record was successfully saved as to {StateStoreName} State Store", TranscriptionId, Components.StateStoreName);
            await _client.PublishEvent(TranscriptionId, new Uri(reference.blobURL), cancellationToken);

            _logger.LogInformation("{TranscriptionId}. {blobURL} was successfully published to {PubSubName} pubsub store", TranscriptionId, reference.blobURL, Components.PubSubName);
            return Ok(new { TranscriptionId, StatusMessage = state.Value.Status, LastUpdated = state.Value.LastUpdateTime });
        }
    }
}