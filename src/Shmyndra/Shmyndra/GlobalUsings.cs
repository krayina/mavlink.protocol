global using System.Collections.Immutable;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Localization;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
global using Shmyndra.Models;
global using Shmyndra.Presentation;
global using Shmyndra.DataContracts;
global using Shmyndra.DataContracts.Serialization;
global using Shmyndra.Services.Caching;
global using Shmyndra.Services.Endpoints;
#if MAUI_EMBEDDING
global using Shmyndra.MauiControls;
#endif
global using ApplicationExecutionState = Windows.ApplicationModel.Activation.ApplicationExecutionState;
