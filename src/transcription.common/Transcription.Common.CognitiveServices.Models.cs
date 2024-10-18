using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Transcription.Common.CognitiveServices
{
    public enum ProfanityFilterMode
    {
        None,
        Removed,
        Tags,
        Masked
    }

    public enum PunctuationMode
    {
        None,
        Dictated,
        Automatic,
        DictatedAndAutomatic
    }

    public class AzureCognitiveServicesTextToSpeechRequest
    {
        public List<string> ContentUrls { get; set; }
        public string DisplayName { get; set; } = "Transcription of file using default model for en-US";
        public AzureCognitiveServicesTextToSpeechProperties Properties { get; set; }
        public string Locale { get; set; } = "en-US";

        public AzureCognitiveServicesTextToSpeechRequest()
        {
            ContentUrls = new List<string>();
            Properties = new AzureCognitiveServicesTextToSpeechProperties();
        }
    }

    public class AzureCognitiveServicesTextToSpeechProperties
    {
        public AzureCognitiveServicesTextToSpeechProperties()
        {
            this.PunctuationMode = PunctuationMode.DictatedAndAutomatic;
            this.ProfanityFilterMode = ProfanityFilterMode.Masked;
            WordLevelTimestampsEnabled = DiarizationEnabled = false;
        }

        public bool DiarizationEnabled { get; set; }
        public bool WordLevelTimestampsEnabled { get; set; }
        public PunctuationMode PunctuationMode { get; set; }
        public ProfanityFilterMode ProfanityFilterMode { get; set; }
    }

    //Reference - https://westus.dev.cognitive.microsoft.com/docs/services/speech-to-text-api-v3-0/operations/GetTranscription
    public class Transcription
    {
        public string Self { get; set; }
        public TranscriptionModel Model { get; set; }
        public TranscriptionLinks Links { get; set; }
        public TranscriptionProperties Properties { get; set; }
        public DateTime LastActionDateTime { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public string Locale { get; set; }
        public string DisplayName { get; }
    }

    public class TranscriptionLinks
    {
        public string Files { get; set; }
    }

    public class TranscriptionModel
    {
        public string Self { get; set; }
    }
    public class TranscriptionProperties
    {
        public bool DiarizationEnabled { get; set; }
        public bool WordLevelTimestampsEnabled { get; set; }
        public IList<int> Channels { get; set; }
        public PunctuationMode PunctuationMode { get; set; }
        public ProfanityFilterMode ProfanityFilterMode { get; set; }
    }

    //Reference - https://westus.dev.cognitive.microsoft.com/docs/services/speech-to-text-api-v3-0/operations/GetTranscriptionFiles
    public class TranscriptionFiles
    {
        public IList<TranscriptionFilesValues> Values { get; set; }
    }

    public class TranscriptionFilesValues
    {
        public string Self { get; set; }
        public string Name { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public TranscriptionFilesLink Links { get; set; }
        public TranscriptionFilesProperties Properties { get; set; }
        public TranscriptionFileType Kind { get; set; }
    }

    public enum TranscriptionFileType
    {
        DatasetReport,
        Audio,
        LanguageData,
        PronunciationData,
        AcousticDataArchive,
        AcousticDataTranscriptionV2,
        Transcription,
        TranscriptionReport,
        EvaluationDetails
    }

    public class TranscriptionFilesLink
    {
        public string ContentUrl { get; set; }
    }

    public class TranscriptionFilesProperties
    {
        public long Size { get; set; }
    }

    //Reference - https://azure.microsoft.com/en-us/services/cognitive-services/
    public class TranscriptionResults
    {
        public string Source { get; set; }
        public DateTime Timestamp { get; set; }
        public long DurationInTicks { get; set; }
        public string Duration { get; set; }
        public IEnumerable<CombinedResults> CombinedRecognizedPhrases { get; set; }

    }

    public class CombinedResults
    {
        public int Channel { get; set; }
        public string Lexical { get; set; }
        public string ITN { get; set; }
        public string MaskedITN { get; set; }
        public string Display { get; set; }
    }

}
