using System; 
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Dapr.Client;
using Microsoft.Extensions.Logging;

using transcription.models;
using transcription.api.dapr;

namespace transcription.Controllers
{ 
    [Route("api/transcribe")]
    [ApiController]
    public class TranscribeController : ControllerBase
    {
        private readonly ILogger _logger;
        private static DaprTranscriptionService _client; 
        
        public TranscribeController(ILogger<TranscribeController> logger, DaprTranscriptionService client )
        {
            _logger = logger;
            _client = client;
        }
        
        [HttpPost]
        public async Task<ActionResult> Post( TranscriptionReferenceRequest reference, CancellationToken cancellationToken)
        {
            var TranscriptionId = Guid.NewGuid().ToString();

            try{
                _logger.LogInformation($"{TranscriptionId}. Request to transcribe {reference.blobURL} was received");

                var state = await _client.UpdateState(TranscriptionId, reference.blobURL);
                _logger.LogInformation($"{TranscriptionId}. Record was successfully saved as to {Components.StateStoreName} State Store");

                await _client.PublishEvent( TranscriptionId, reference.blobURL, cancellationToken);
                _logger.LogInformation($"{TranscriptionId}. {reference.blobURL} was successfully published to {Components.PubSubName} pubsub store");
                
                return Ok( new { TranscriptionId = TranscriptionId, StatusMessage = state.Value.Status, LastUpdated = state.Value.LastUpdateTime }  ); 
            }
            catch( Exception ex ) 
            {
                _logger.LogWarning($"Failed to transcribe {reference.blobURL} - {ex.Message}");    
            }

            return BadRequest();
        }
    }
}