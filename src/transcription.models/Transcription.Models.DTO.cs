using System;

namespace Transcription.Models
{
    public class TradiureTranscriptionRequest
    {
        public Guid TranscriptionId { get; set; }
        public string BlobUri { get; set; }
    }

    public class TraduireTranscription
    {
        public Guid TranscriptionId { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public TraduireTranscriptionStatus Status { get; set; }
        public string FileName { get; set; }
        public string BlobUri { get; set; }
        public string TranscriptionStatusUri { get; set; }
        public string TranscriptionTextUri { get; set; }
        public string TranscriptionText { get; set; }
        public string StatusDetails { get; set; }
    }

    public enum TraduireTranscriptionStatus
    {
        Started,
        SentToCognitiveServices,
        Pending,
        Completed,
        Failed
    }
}