using System.Net;

namespace Transcription.Controllers
{
    [ApiController]
    public class TranslationOnStarted(ILogger<TranslationOnStarted> logger, DaprClient client, AzureCognitiveServicesClient cogsClient, WebPubSubServiceClient serviceClient) : ControllerBase
    {
        private readonly TraduireNotificationService _serviceClient = new(serviceClient);
        private readonly DaprClient _client = client;
        private readonly AzureCognitiveServicesClient _cogsClient = cogsClient;
        private readonly ILogger _logger = logger;

        [Topic(Components.PubSubName, Topics.TranscriptionSubmittedTopicName)]
        [HttpPost("transcribe")]
        public async Task<ActionResult> Transcribe(TradiureTranscriptionRequest req, CancellationToken cancellationToken)
        {
            var id = req.TranscriptionId.ToString();
            _logger.LogInformation( $"{id}. {req.BlobUri} was successfully received by Dapr PubSub" );

            (Common.CognitiveServices.Transcription response, HttpStatusCode code) = await _cogsClient.SubmitTranscriptionRequestAsync(new Uri(req.BlobUri));
            await _serviceClient.PublishNotification(id, response.Status);

            return code switch
            {
                HttpStatusCode.Created => await HandleSuccess(response.Self, req.TranscriptionId),
                _ => await HandleFailure(response.Self, req.TranscriptionId),
            };
        }

        private async Task<ActionResult> HandleSuccess(string uri, Guid transcriptionId)
        {
            _logger.LogInformation( $"{transcriptionId}. Event was successfully publish to Azure Cognitive Services" );
            await UpdateStateRepository(TraduireTranscriptionStatus.SentToCognitiveServices, HttpStatusCode.Created, uri, transcriptionId);

            await _client.PublishEventAsync(
                            Components.PubSubName, 
                            Topics.TranscriptionProcessingTopicName, 
                            new TradiureTranscriptionRequest() {
                                TranscriptionId = transcriptionId,
                                BlobUri = uri
                            },
                            CancellationToken.None);

            return Ok(transcriptionId);
        }

        private async Task<ActionResult> HandleFailure(string uri, Guid transcriptionId)
        {
            _logger.LogInformation( $"{transcriptionId}. Transcription Failed for an unexpected reason. Review {Topics.TranscriptionFailedTopicName} topic for details" );
            await UpdateStateRepository(TraduireTranscriptionStatus.Failed, HttpStatusCode.BadRequest, uri, transcriptionId);

            await _client.PublishEventAsync(
                            Components.PubSubName, 
                            Topics.TranscriptionFailedTopicName, 
                            new TradiureTranscriptionRequest() {
                                TranscriptionId = transcriptionId,
                                BlobUri = uri
                            }, 
                            CancellationToken.None);

            return BadRequest();
        }

        private async Task UpdateStateRepository(TraduireTranscriptionStatus status, HttpStatusCode code, string uri, Guid transcriptionId)
        {
            var state = await _client.GetStateEntryAsync<TraduireTranscription>(Components.StateStoreName, transcriptionId.ToString());
            state.Value ??= new TraduireTranscription();

            state.Value.LastUpdateTime = DateTime.UtcNow;
            state.Value.Status = status;
            state.Value.StatusDetails = code.ToString();
            state.Value.TranscriptionStatusUri = uri;
            await state.SaveAsync();
        }
    }
}
