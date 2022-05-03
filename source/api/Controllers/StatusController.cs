using System; 
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.Encodings.Web;
using Dapr;
using Dapr.Client;
using Microsoft.Extensions.Logging;

using transcription.models;
using transcription.api.dapr;

namespace transcription.Controllers
{ 
    [Route("api/status")]
    [ApiController]
    public class StatusController : ControllerBase
    {
        private readonly ILogger _logger;
        private static DaprTranscriptionService _client; 

        public StatusController(ILogger<StatusController> logger, DaprTranscriptionService client )
        {
            _logger = logger;
            _client = client;
        }

        [HttpGet("{TranscriptionId}")]
        public async Task<ActionResult> Get(string TranscriptionId, CancellationToken cancellationToken)
        {
            try{
                _logger.LogInformation($"{TranscriptionId}. Status API Called");

                var state = await _client.GetState(TranscriptionId);
                
                if( state == null ) {
                    return NotFound();
                }

                _logger.LogInformation($"{TranscriptionId}. Current status is {state.Status}");
                return Ok( new { TranscriptionId = TranscriptionId, StatusMessage = state.Status, LastUpdated = state.LastUpdateTime }  ); 
            }
            catch( Exception ex ) 
            {
                _logger.LogWarning($"Failed to transctionId {TranscriptionId} - {ex.Message}");    
            }

            return BadRequest();
        }
    }
}