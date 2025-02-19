using System;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Messaging.WebPubSub;

namespace Transcription.Common
{
    public static class Helper
    {
        public static string NewGuid()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
