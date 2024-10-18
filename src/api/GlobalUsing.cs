global using OpenTelemetry.Metrics;
global using OpenTelemetry.Trace;
global using OpenTelemetry.Logs;
global using OpenTelemetry.Resources;
global using System.Diagnostics.Metrics;
global using System.Diagnostics;
global using Azure.Identity;
global using OpenTelemetry.Exporter;
global using Microsoft.Extensions.Logging;
global using System;
global using System.Collections.Generic;

global using System.Threading.Tasks;
global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Hosting;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;

global using Dapr;
global using Azure.Messaging.WebPubSub;

global using Transcription.Models;
global using Transcription.Api.Dapr;
global using Transcription.Common.CognitiveServices;
