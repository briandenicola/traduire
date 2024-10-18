using Dapr.Actors;
using Dapr.Actors.Client;

namespace transcription.Controllers
{
    [ApiController]
    public class TranslationOnProcessing(ILogger<TranslationOnProcessing> logger) : ControllerBase
    {
        private readonly ILogger _logger = logger;

        [Topic(Components.PubSubName, Topics.TranscriptionProcessingTopicName)]
        [HttpPost("status")]
        public async Task<ActionResult> Transcribe(TradiureTranscriptionRequest req, CancellationToken cancellationToken)
        {
            var id = req.TranscriptionId.ToString();
            _logger.LogInformation($"{id}. {req.BlobUri} was successfully received by Dapr PubSub");
            _logger.LogInformation($"{id}. Instantiating a Transcription Actor");
            var transcriptionActor = GetTranscriptionActor(id);
            await transcriptionActor.SubmitAsync(id, req.BlobUri);
            return Ok();
        }

        private ITranscriptionActor GetTranscriptionActor(string transcriptId)
        {
            var actorId = new ActorId(transcriptId);
            return ActorProxy.Create<ITranscriptionActor>(actorId, nameof(TranscriptionActor));
        }
    }
}
