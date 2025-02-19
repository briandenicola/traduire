namespace Transcription.Controllers
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
            string id = Helper.NewGuid();

            using var activity = _traduireActivitySource.StartActivity("UploadController.PostActivity");

            _logger.LogInformation($"{id}. File upload request was received.");
            var response = await _client.UploadFile(file, cancellationToken);

            _logger.LogInformation($"{id}. File was saved to {Components.BlobStoreName} blob storage");

            var sasUrl = await _client.GetBlobSasToken(response.blobURL, msiClientID);
            var state = await _client.UpdateState(id, sasUrl);

            _logger.LogInformation($"{id}. Record was successfully saved as to {Components.StateStoreName} State Store");

            await _client.PublishEvent(id, sasUrl, cancellationToken);
            _logger.LogInformation($"{id}. {sasUrl} was published to {Components.PubSubName} Pubsub store");

            return Ok(new { id, StatusMessage = state.Value.Status, LastUpdated = state.Value.LastUpdateTime });
        }
    }
}
