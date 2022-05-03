using System; 
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Azure.Messaging.WebPubSub; 
using Azure.Core;

using transcription.models;
using transcription.common;
using transcription.common.cognitiveservices;

namespace transcription.Controllers
{ 
    [ApiController]
    public class TranslationOnCompletion : ControllerBase
    {

        private StateEntry<TraduireTranscription> state;
        private readonly TraduireNotificationService _serviceClient;
        private readonly IConfiguration _configuration;
        private readonly DaprClient _client;
        private readonly AzureCognitiveServicesClient _cogsClient; 
        private readonly ILogger _logger;
                
        public TranslationOnCompletion(ILogger<TranslationOnCompletion> logger, IConfiguration configuration, DaprClient Client, AzureCognitiveServicesClient CogsClient, WebPubSubServiceClient ServiceClient)
        {
            _client = Client;
            _logger = logger;
            _configuration = configuration;
            _cogsClient = CogsClient;
			_serviceClient = new TraduireNotificationService(ServiceClient);
        }

        [Topic(Components.PubSubName, Topics.TranscriptionCompletedTopicName)]
        [HttpPost("completed")]
        public async Task<ActionResult> Transcribe(TradiureTranscriptionRequest request,  CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"{request.TranscriptionId}. {request.BlobUri} was successfully received by Dapr PubSub");
                state = await _client.GetStateEntryAsync<TraduireTranscription>(Components.StateStoreName, request.TranscriptionId.ToString());
                state.Value ??= new TraduireTranscription();

                (TranscriptionResults result, HttpStatusCode code)  = await _cogsClient.DownloadTranscriptionResultAsync(new Uri(request.BlobUri)); 

                switch(code)
                {
                    case HttpStatusCode.OK:                  
                        _logger.LogInformation($"{request.TranscriptionId}. Transcription from '{request.BlobUri}' was saved to state store ");
                        var firstChannel = result.CombinedRecognizedPhrases.FirstOrDefault();

                        await _serviceClient.PublishNotification(request.TranscriptionId.ToString(), state.Value.Status.ToString());
                        await UpdateStateRepository(TraduireTranscriptionStatus.Completed, firstChannel.Display);

                        _logger.LogInformation($"{request.TranscriptionId}. All working completed on request");
                        return Ok(request.TranscriptionId); 
                    
                    default:
                        _logger.LogInformation($"{request.TranscriptionId}. Transcription Failed for an unexpected reason. Added to Failed Queue for review");
                        var failedEvent = await UpdateStateRepository(TraduireTranscriptionStatus.Failed, code, request.BlobUri);
                        await _client.PublishEventAsync(Components.PubSubName, Topics.TranscriptionFailedTopicName, failedEvent, cancellationToken);
                        break;
                }
            }
            catch( Exception ex )  
            {
                _logger.LogWarning($"Nuts. Something really bad happened processing {request.BlobUri} - {ex.Message}"); 
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

            return new TradiureTranscriptionRequest() {
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
    }
}
