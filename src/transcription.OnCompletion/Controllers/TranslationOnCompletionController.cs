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
    public class TranslationOnCompletion(ILogger<TranslationOnCompletion> logger, DaprClient Client, AzureCognitiveServicesClient CogsClient, WebPubSubServiceClient ServiceClient) : ControllerBase
    {

        private StateEntry<TraduireTranscription> state;
        private readonly TraduireNotificationService _serviceClient = new(ServiceClient);
        private readonly DaprClient _client = Client;
        private readonly AzureCognitiveServicesClient _cogsClient = CogsClient;
        private readonly ILogger _logger = logger;

        [Topic(Components.PubSubName, Topics.TranscriptionCompletedTopicName)]
        [HttpPost("completed")]
        public async Task<ActionResult> Transcribe(TradiureTranscriptionRequest request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{TranscriptionId}. {BlobUri} was successfully received by Dapr PubSub", request.TranscriptionId, request.BlobUri);
            state = await _client.GetStateEntryAsync<TraduireTranscription>(Components.StateStoreName, request.TranscriptionId.ToString(), cancellationToken: cancellationToken);
            state.Value ??= new TraduireTranscription();

            (TranscriptionResults result, HttpStatusCode code) = await _cogsClient.DownloadTranscriptionResultAsync(new Uri(request.BlobUri));

            switch (code)
            {
                case HttpStatusCode.OK:
                    _logger.LogInformation("{TranscriptionId}. Transcription from '{BlobUri}' was saved to state store", request.TranscriptionId, request.BlobUri);
                    var firstChannel = result.CombinedRecognizedPhrases.FirstOrDefault();

                    await _serviceClient.PublishNotification(request.TranscriptionId.ToString(), state.Value.Status.ToString());
                    await UpdateStateRepository(TraduireTranscriptionStatus.Completed, firstChannel.Display);

                    _logger.LogInformation("{TranscriptionId}. All working completed on request", request.TranscriptionId);
                    return Ok(request.TranscriptionId);
            
                default:
                    _logger.LogInformation("{TranscriptionId}. Transcription Failed for an unexpected reason. Added to Failed Queue for review", request.TranscriptionId);
                    await _client.PublishEventAsync(
                                    Components.PubSubName, 
                                    Topics.TranscriptionFailedTopicName, 
                                    await UpdateStateRepository(TraduireTranscriptionStatus.Failed, code, request.BlobUri), 
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
    }
}
