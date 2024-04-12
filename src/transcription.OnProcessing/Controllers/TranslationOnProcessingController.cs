using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Dapr;
using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Client;
using Azure.Messaging.WebPubSub;

using transcription.models;
using transcription.actors;
using transcription.common;
using transcription.common.cognitiveservices;

namespace transcription.Controllers
{
    [ApiController]
    public class TranslationOnProcessing(ILogger<TranslationOnProcessing> logger) : ControllerBase
    {
        private readonly ILogger _logger = logger;

        [Topic(Components.PubSubName, Topics.TranscriptionProcessingTopicName)]
        [HttpPost("status")]
        public async Task<ActionResult> Transcribe(TradiureTranscriptionRequest request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{transcriptionId}. {BlobUri} was successfully received by Dapr PubSub", request.TranscriptionId, request.BlobUri);
            _logger.LogInformation("{transcriptionId}. Instantiating a Transcription Actor to handle saga",  request.TranscriptionId);
            var transcriptionActor = GetTranscriptionActor(request.TranscriptionId);
            await transcriptionActor.SubmitAsync(request.TranscriptionId.ToString(), request.BlobUri);
            return Ok();
        }

        private ITranscriptionActor GetTranscriptionActor(Guid transcriptId)
        {
            var actorId = new ActorId(transcriptId.ToString());
            return ActorProxy.Create<ITranscriptionActor>(actorId, nameof(TranscriptionActor));
        }
    }
}
