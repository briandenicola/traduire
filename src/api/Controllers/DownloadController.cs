using Microsoft.AspNetCore.Mvc;
using transcription.models;
using transcription.api.dapr;

namespace transcription.Controllers
{
    [Route("api/download")]
    [ApiController]
    public class DownloadController : ControllerBase
    {
        private readonly ILogger _logger;
        private static DaprTranscriptionService _client;
        private static ActivitySource _traduireActivitySource;

        public DownloadController(ILogger<DownloadController> logger, DaprTranscriptionService client, ActivitySource traduireActivitySource)
        {
            _logger = logger;
            _client = client;
            _traduireActivitySource = traduireActivitySource;
        }

        [HttpGet("{TranscriptionId}")]
        public async Task<ActionResult> Get(string TranscriptionId, CancellationToken cancellationToken)
        {
            using var activity = _traduireActivitySource.StartActivity("DownloadController.GetActivity");
            _logger.LogInformation("{TranscriptionId}. Attempting to download completed transcription", TranscriptionId);

            var state = await _client.GetState(TranscriptionId);
            if (state == null)  {
                return NotFound();
            }

            if (state.Status == TraduireTranscriptionStatus.Completed) {
                _logger.LogInformation("{TranscriptionId}. Current status is {TraduireTranscriptionStatus}. Returning transcription", TranscriptionId, TraduireTranscriptionStatus.Completed );
                return Ok(new { TranscriptionId, StatusMessage = state.Status, Transcription = state.TranscriptionText });
            }

            _logger.LogInformation("{TranscriptionId}. Current status is {TraduireTranscriptionStatus}. Transcription is not yet complete", TranscriptionId, state.Status); 
            return NoContent();
        }
    }
}