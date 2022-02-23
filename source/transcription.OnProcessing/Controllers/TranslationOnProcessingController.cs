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
    public class TranslationOnProcessing : ControllerBase
    {   
        private readonly ILogger _logger;
                
        public TranslationOnProcessing(ILogger<TranslationOnProcessing> logger)
        {
            _logger = logger;
        }

        [Topic(Components.PubSubName, Topics.TranscriptionProcessingTopicName)]
        [HttpPost("status")]
        public async Task<ActionResult> Transcribe(TradiureTranscriptionRequest request,  CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"{request.TranscriptionId}. {request.BlobUri} was successfullly received by Dapr PubSub");
                _logger.LogInformation($"{request.TranscriptionId}. Instantiating a Transcription Actor to handle saga");
                var transcriptionActor = this.GetTranscriptionActor(request.TranscriptionId);
                await transcriptionActor.SubmitAsync(request.TranscriptionId.ToString(), request.BlobUri);

                return Ok(); 

            }
            catch ( Exception ex )  
            {
                _logger.LogWarning($"Nuts. Something really bad happened processing {request.BlobUri} - {ex.Message}"); 
            }

            return BadRequest(); 
        }

        private ITranscriptionActor GetTranscriptionActor(Guid transcriptId)
        {
            var actorId = new ActorId(transcriptId.ToString());
            return ActorProxy.Create<ITranscriptionActor>(actorId, nameof(TranscriptionActor));
        }

    }
}
