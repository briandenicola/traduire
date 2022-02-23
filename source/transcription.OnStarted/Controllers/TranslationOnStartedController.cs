using System; 
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dapr;
using Dapr.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Azure.Messaging.WebPubSub; 
using Azure.Core;

using transcription.models;
using transcription.common;
using transcription.common.cognitiveservices;

namespace transcription.Controllers
{ 
    [ApiController]
    public class TranslationOnStarted : ControllerBase
    {
        private readonly TraduireNotificationService _serviceClient;
        private readonly IConfiguration _configuration;
        private readonly DaprClient _client;
        private readonly AzureCognitiveServicesClient _cogsClient; 
        private readonly ILogger _logger;
                
        public TranslationOnStarted(ILogger<TranslationOnStarted> logger, IConfiguration configuration, DaprClient Client, AzureCognitiveServicesClient CogsClient, WebPubSubServiceClient ServiceClient)
        {
            _client = Client;
            _logger = logger;
            _configuration = configuration;
        	_cogsClient = CogsClient;
			_serviceClient = new TraduireNotificationService(ServiceClient);
        }

        [Topic(Components.PubSubName, Topics.TranscriptionSubmittedTopicName)]
        [HttpPost("transcribe")]
        public async Task<ActionResult> Transcribe(TradiureTranscriptionRequest request,  CancellationToken cancellationToken)
        {         
            try
            {
                _logger.LogInformation($"{request.TranscriptionId}. {request.BlobUri} was successfullly received by Dapr PubSub");
                (Transcription response, HttpStatusCode code)  = await _cogsClient.SubmitTranscriptionRequestAsync(new Uri(request.BlobUri));

                await _serviceClient.PublishNotification(request.TranscriptionId.ToString(), response.Status);

                return code switch
                {
                    HttpStatusCode.Created => await HandleSuccess(response.Self, request.TranscriptionId),
                    _ => await HandleFailure(response.Self, request.TranscriptionId),    
                }; 
            }
            catch( Exception ex )  
            {
                _logger.LogWarning($"Nuts. Something really bad happened processing {request.BlobUri} - {ex.Message}"); 
            }

            return BadRequest(); 
        }

        private async Task<ActionResult> HandleSuccess(string uri, Guid transcriptionId)
        {
            _logger.LogInformation($"{transcriptionId}. Event was successfullly publish to Azure Cognitive Services");
            await UpdateStateRepository(TraduireTranscriptionStatus.SentToCognitiveServices, HttpStatusCode.Created, uri, transcriptionId);

            var createdEvent = new TradiureTranscriptionRequest() {
                TranscriptionId = transcriptionId,
                BlobUri = uri
            };
            await _client.PublishEventAsync(Components.PubSubName, Topics.TranscriptionProcessingTopicName, createdEvent, CancellationToken.None );

            return Ok(transcriptionId); 
        }

        private async Task<ActionResult> HandleFailure(string uri, Guid transcriptionId) 
        {
            _logger.LogInformation($"{transcriptionId}. Transcription Failed for an unexpected reason. Added to Failed Queue for review");
            await UpdateStateRepository(TraduireTranscriptionStatus.Failed, HttpStatusCode.BadRequest, uri, transcriptionId);

            var failedEvent = new TradiureTranscriptionRequest() {
                TranscriptionId = transcriptionId,
                BlobUri = uri
            };
            await _client.PublishEventAsync(Components.PubSubName, Topics.TranscriptionFailedTopicName, failedEvent, CancellationToken.None);

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
