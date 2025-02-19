
namespace Transcription.Controllers
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

            string id = Helper.NewGuid();

            _logger.LogInformation($"{id}. Request to transcribe {reference.blobURL} was received");
            var state = await _client.UpdateState(id, new Uri(reference.blobURL));

            _logger.LogInformation($"{id}. Record was successfully saved as to {Components.StateStoreName} State Store");
            await _client.PublishEvent(id, new Uri(reference.blobURL), cancellationToken);

            _logger.LogInformation($"{id}. {reference.blobURL} was successfully published to {Components.PubSubName} PubSub Store");
            return Ok(new { id, StatusMessage = state.Value.Status, LastUpdated = state.Value.LastUpdateTime });
        }
    }
}