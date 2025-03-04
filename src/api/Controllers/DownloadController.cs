namespace Transcription.Controllers
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

        [HttpGet("{id}")]
        public async Task<ActionResult> Get(string id, CancellationToken cancellationToken)
        {
            using var activity = _traduireActivitySource.StartActivity("DownloadController.GetActivity");
            _logger.LogInformation($"{id}. Attempting to download completed transcription");

            var state = await _client.GetState(id);
            if (state == null)
            {
                _logger.LogWarning($"{id}. Transcription not found.");
                return NotFound();
            }

            if (state.Status == TraduireTranscriptionStatus.Completed)
            {
                _logger.LogInformation($"{id}. Current status is {TraduireTranscriptionStatus.Completed}. Returning transcription");
                return Ok(new { id, StatusMessage = state.Status, Transcription = state.TranscriptionText });
            }

            _logger.LogInformation($"{id}. Current status is {state.Status}. Transcription is not completed yet.");
            return NoContent();
        }
    }
}