using Transcription.Api.Dapr;

namespace Transcription.Controllers
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
                        
            _logger.LogInformation( $"{TranscriptionId}. Status API Called" );

            var state = await _client.GetState(TranscriptionId);
            if (state == null) {
                return NotFound();
            }

            apiCount.Add(1);
            _logger.LogInformation( $"{TranscriptionId}. Current status is {state.Status}" );
            return Ok(new { TranscriptionId, StatusMessage = state.Status, LastUpdated = state.LastUpdateTime });
        }
    }
}