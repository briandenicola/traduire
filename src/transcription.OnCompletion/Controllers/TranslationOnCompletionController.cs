using System.Net;

namespace Transcription.Controllers
{
    [ApiController]
    public class TranslationOnCompletion(ILogger<TranslationOnCompletion> logger, DaprClient client, AzureCognitiveServicesClient cogsClient, WebPubSubServiceClient serviceClient) : ControllerBase
    {

        private StateEntry<TraduireTranscription> state;
        private readonly TraduireNotificationService _serviceClient = new(serviceClient);
        private readonly DaprClient _client = client;
        private readonly AzureCognitiveServicesClient _cogsClient = cogsClient;
        private readonly ILogger _logger = logger;

        [Topic(Components.PubSubName, Topics.TranscriptionCompletedTopicName)]
        [HttpPost("completed")]
        public async Task<ActionResult> Transcribe(TradiureTranscriptionRequest req, CancellationToken cancellationToken)
        {
            var id = req.TranscriptionId.ToString();
            _logger.LogInformation( $"{id}. {req.BlobUri} was successfully received by Dapr PubSub" );
            //var state = await GetState(id);

            (TranscriptionResults result, HttpStatusCode code) = await _cogsClient.DownloadTranscriptionResultAsync(new Uri(req.BlobUri));

            switch (code)
            {
                case HttpStatusCode.OK:
                    _logger.LogInformation( $"{id}. Transcription from '{req.BlobUri}' was saved to state store" );
                    var text = result.CombinedRecognizedPhrases.FirstOrDefault().Display;

                    await _serviceClient.PublishNotification(id, nameof(TraduireTranscriptionStatus.Completed));
                    await UpdateStateRepository(TraduireTranscriptionStatus.Completed, text);

                    _logger.LogInformation( $"{id}. All work has been completed for the request" );
                    return Ok(id);
            
                default:
                    _logger.LogInformation( $"{id}. Transcription Failed for an unexpected reason. Review {Topics.TranscriptionFailedTopicName} topic for details" );
                    await _client.PublishEventAsync(
                                    Components.PubSubName, 
                                    Topics.TranscriptionFailedTopicName, 
                                    await UpdateStateRepository(TraduireTranscriptionStatus.Failed, code, req.BlobUri), 
                                    cancellationToken);
                    break;
            }

            return BadRequest();
        }

        private async Task<TradiureTranscriptionRequest> UpdateStateRepository(TraduireTranscriptionStatus status, HttpStatusCode code, string uri)
        {
            state.Value.LastUpdateTime = DateTime.UtcNow;
            state.Value.Status = status;
            state.Value.StatusDetails = code.ToString();
            state.Value.TranscriptionStatusUri = uri;
            await state.SaveAsync();

            return new TradiureTranscriptionRequest()
            {
                TranscriptionId = state.Value.TranscriptionId,
                BlobUri = uri
            };
        }

        private async Task UpdateStateRepository(TraduireTranscriptionStatus status, string text)
        {
            state.Value.LastUpdateTime = DateTime.UtcNow;
            state.Value.Status = status;
            state.Value.TranscriptionText = text;
            await state.SaveAsync();
        }

        private async Task<StateEntry<TraduireTranscription>> GetState(string transcriptionId)
        {
            var item = await _client.GetStateEntryAsync<TraduireTranscription>(Components.StateStoreName, transcriptionId);
            item.Value ??= new TraduireTranscription();
            return item;
        }
    }
}
