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
    public class TranslationOnStarted(ILogger<TranslationOnStarted> logger, DaprClient Client, AzureCognitiveServicesClient CogsClient, WebPubSubServiceClient ServiceClient) : ControllerBase
    {
        private readonly TraduireNotificationService _serviceClient = new(ServiceClient);
        private readonly DaprClient _client = Client;
        private readonly AzureCognitiveServicesClient _cogsClient = CogsClient;
        private readonly ILogger _logger = logger;

        [Topic(Components.PubSubName, Topics.TranscriptionSubmittedTopicName)]
        [HttpPost("transcribe")]
        public async Task<ActionResult> Transcribe(TradiureTranscriptionRequest request, CancellationToken cancellationToken)
        {
            _logger.LogInformation( $"{request.TranscriptionId}. {request.BlobUri} was successfully received by Dapr PubSub" );
            (Transcription response, HttpStatusCode code) = await _cogsClient.SubmitTranscriptionRequestAsync(new Uri(request.BlobUri));

            await _serviceClient.PublishNotification(request.TranscriptionId.ToString(), response.Status);

            return code switch
            {
                HttpStatusCode.Created => await HandleSuccess(response.Self, request.TranscriptionId),
                _ => await HandleFailure(response.Self, request.TranscriptionId),
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
            _logger.LogInformation( $"{transcriptionId}. Transcription Failed for an unexpected reason. Added to Failed Queue for review" );
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
