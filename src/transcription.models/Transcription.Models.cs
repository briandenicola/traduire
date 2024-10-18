using System;

namespace Transcription.Models
{
    public class BlobBindingResponse
    {
        public string blobURL { get; set; }
    }

    public class TranscriptionReferenceRequest
    {
        public string blobURL { get; set; }
    }
}
