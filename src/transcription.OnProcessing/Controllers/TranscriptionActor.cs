using System.Net;
using Dapr.Actors.Runtime;

namespace Transcription.Actors
{
    public class TranscriptionActor(ActorHost host, ILogger<TranscriptionActor> logger, IConfiguration configuration, DaprClient client, AzureCognitiveServicesClient cogsClient, WebPubSubServiceClient serviceClient) : Actor(host), ITranscriptionActor, IRemindable
    {
        private const int WAIT_TIME = 30;
        private const string ProcessingStatusReminder = "ProcessingStatusReminder";

        private StateEntry<TraduireTranscription> state;
        private readonly TraduireNotificationService _serviceClient = new TraduireNotificationService(serviceClient);
        private readonly IConfiguration _configuration = configuration;
        private readonly DaprClient _client = client;
        private readonly AzureCognitiveServicesClient _cogsClient = cogsClient;
        private readonly ILogger _logger = logger;
        private TradiureTranscriptionRequest transcriptionRequest;
        private string _id; 

        public async Task SubmitAsync(string transcriptionId, string uri)
        {
            _id = transcriptionId;

            transcriptionRequest = new TradiureTranscriptionRequest()
            {
                TranscriptionId = new Guid(_id),
                BlobUri = uri
            };

            await UpdateStateRepository(TraduireTranscriptionStatus.Pending, HttpStatusCode.Accepted);

            _logger.LogInformation( $"{_id}. Registering {ProcessingStatusReminder} Actor Reminder for {WAIT_TIME} seconds" );

            await RegisterReminderAsync(
                ProcessingStatusReminder,
                null,
                TimeSpan.FromSeconds(WAIT_TIME),
                TimeSpan.FromSeconds(WAIT_TIME));
        }

        private async Task<(Common.CognitiveServices.Transcription response, HttpStatusCode code)> CheckCognitiveServicesTranscriptionStatusAsync()
        {
            (Common.CognitiveServices.Transcription response, HttpStatusCode code) = await _cogsClient.CheckTranscriptionRequestAsync(new Uri(transcriptionRequest.BlobUri));
            await _serviceClient.PublishNotification(_id, response.Status);
            return (response, code);
        }

        private async Task<StateEntry<TraduireTranscription>> GetCurrentState(string id)
        {
            var state = await _client.GetStateEntryAsync<TraduireTranscription>(Components.StateStoreName, id);
            return state;
        }

        private async Task UpdateStateRepository(TraduireTranscriptionStatus status, HttpStatusCode code)
        {
            state = await GetCurrentState(_id);
            state.Value ??= new TraduireTranscription();
            state.Value.LastUpdateTime = DateTime.UtcNow;
            state.Value.Status = status;
            state.Value.StatusDetails = code.ToString();
            state.Value.TranscriptionStatusUri = transcriptionRequest.BlobUri;
            await state.SaveAsync();
        }

        private async Task PublishTranscriptionCompletion(string id, string uri)
        {
            _logger.LogInformation( $"{id}. Azure Cognitive Services has completed processing transcription" );
            
            await UpdateStateRepository(TraduireTranscriptionStatus.Completed, HttpStatusCode.OK);

            await _client.PublishEventAsync(
                            Components.PubSubName, 
                            Topics.TranscriptionCompletedTopicName, 
                            new TradiureTranscriptionRequest() {
                                TranscriptionId = new Guid(id),
                                BlobUri = uri
                            }, 
                            CancellationToken.None);

            await UnregisterReminderAsync(ProcessingStatusReminder);
        }

        private async Task PublishTranscriptionFailure()
        {
            _logger.LogInformation( $"{_id}. Transcription Failed for an unexpected reason. Review {Topics.TranscriptionFailedTopicName} topic for details" );
            await UpdateStateRepository(TraduireTranscriptionStatus.Failed, HttpStatusCode.BadRequest);

            await _client.PublishEventAsync(Components.PubSubName, Topics.TranscriptionFailedTopicName, transcriptionRequest, CancellationToken.None);
            await UnregisterReminderAsync(ProcessingStatusReminder);
        }

        private async Task PublishTranscriptionStillProcessing()
        {
            _logger.LogInformation( $"{_id}. Azure Cognitive Services is still progressing request" );
            await UpdateStateRepository(TraduireTranscriptionStatus.Pending, HttpStatusCode.OK);
        }

        private async Task CheckProcessingStatus()
        {
            (Common.CognitiveServices.Transcription response, HttpStatusCode code) = await CheckCognitiveServicesTranscriptionStatusAsync();

            switch (code)
            {
                case HttpStatusCode.OK when response.Status == "Succeeded":
                    await PublishTranscriptionCompletion(_id, response.Links.Files);
                    break;
                case HttpStatusCode.OK:
                    await PublishTranscriptionStillProcessing();
                    break;
                default:
                    await PublishTranscriptionFailure();
                    break;
            }
        }

        public Task ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period)
        {
            return reminderName switch
            {
                ProcessingStatusReminder => CheckProcessingStatus(),
                _ => Task.CompletedTask,
            };
        }
    }
}
