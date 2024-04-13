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
    [Route("api/upload")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly string msiClientID;
        private readonly ILogger _logger;
        private static DaprTranscriptionService _client;
        private static ActivitySource _traduireActivitySource;

        public UploadController(ILogger<UploadController> logger, DaprTranscriptionService client, ActivitySource traduireActivitySource)
        {
            msiClientID = Environment.GetEnvironmentVariable("MSI_CLIENT_ID");

            _logger = logger;
            _client = client;
            _traduireActivitySource = traduireActivitySource;
        }

        [HttpPost, DisableRequestSizeLimit]
        public async Task<ActionResult> Post([FromForm] IFormFile file, CancellationToken cancellationToken)
        {
            var TranscriptionId = Guid.NewGuid().ToString();

            using var activity = _traduireActivitySource.StartActivity("UploadController.PostActivity");
        
            _logger.LogInformation( $"{TranscriptionId}. File upload request was received." );
            var response = await _client.UploadFile(file, cancellationToken);

            _logger.LogInformation( $"{TranscriptionId}. File was saved to {Components.BlobStoreName} blob storage" );

            var sasUrl = await _client.GetBlobSasToken(response.blobURL, msiClientID);
            var state = await _client.UpdateState(TranscriptionId, sasUrl);

            _logger.LogInformation( $"{TranscriptionId}. Record was successfully saved as to {Components.StateStoreName} State Store" );
            
            await _client.PublishEvent(TranscriptionId, sasUrl, cancellationToken);
            _logger.LogInformation( $"{TranscriptionId}. {sasUrl} was published to {Components.PubSubName} Pubsub store" );

            return Ok(new { TranscriptionId, StatusMessage = state.Value.Status, LastUpdated = state.Value.LastUpdateTime });
        }
    }
}
