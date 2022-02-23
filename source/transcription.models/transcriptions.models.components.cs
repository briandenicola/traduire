using System;

namespace transcription.models
{
    public static class Components 
    {
        public const string BlobStoreName    = "storage";
        public const string PubSubName       = "pubsub";
        public const string StateStoreName   = "sql";
        public const string SecureStore      = "keyvault";
        public const string SecretName       = "speech2textkey";
        public const string PubSubSecretName = "pubsubkey";
        public const string PubSubHubName    = "transcription";
        public const string CreateOperation  = "create";
        
    }
}