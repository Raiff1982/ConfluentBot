// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.22.0

using ConfluentBot.Bots;
using ConfluentBot.Dialogs;
using ConfluentBot.Services;
using ConfluentBot.Services.AegisMemory;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ConfluentBot
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient().AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.MaxDepth = HttpHelper.BotMessageSerializerSettings.MaxDepth;
            });

            // Create the Bot Framework Authentication to be used with the Bot Adapter.
            services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

            // Create the Bot Adapter with error handling enabled.
            services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

            // Create the storage we'll be using for User and Conversation state. (Memory is great for testing purposes.)
            services.AddSingleton<IStorage, MemoryStorage>();

            // Create the User state. (Used in this bot's Dialog implementation.)
            services.AddSingleton<UserState>();

            // Create the Conversation state. (Used by the Dialog system itself.)
            services.AddSingleton<ConversationState>();

            // Register LUIS recognizer
            services.AddSingleton<FlightBookingRecognizer>();

            // Register the BookingDialog.
            services.AddSingleton<BookingDialog>();

            // The MainDialog that will be run by the bot.
            services.AddSingleton<MainDialog>();

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            services.AddTransient<IBot, DialogAndWelcomeBot<MainDialog>>();

            // Register Confluent Kafka streaming services
            var kafkaConfig = _configuration.GetSection("Kafka").Get<KafkaConsumerConfig>() ?? new KafkaConsumerConfig();
            services.AddSingleton(kafkaConfig);
            services.AddSingleton<IKafkaConsumerService, KafkaConsumerService>();

            // Register Google Cloud Vertex AI prediction service
            var vertexAiConfig = _configuration.GetSection("VertexAI").Get<VertexAIConfig>() ?? new VertexAIConfig();
            services.AddSingleton(vertexAiConfig);
            services.AddSingleton<IVertexAIPredictionService, VertexAIPredictionService>();

            // Register stream processing pipeline
            services.AddSingleton<IStreamProcessingPipeline, StreamProcessingPipeline>();

            // Register telemetry service
            services.AddSingleton<IStreamTelemetry, StreamTelemetry>();

            // ===== NEW: Aegis Framework Services =====
            // Register regenerative memory as singleton (system-wide state)
            services.AddSingleton<RegenerativeMemory>();

            // Register stream agents (they depend on RegenerativeMemory)
            services.AddSingleton<DataQualityAgent>();
            services.AddSingleton<TrendAgent>();
            services.AddSingleton<StreamHealthAgent>();
            services.AddSingleton<FraudDetectionAgent>();

            // Register MetaCouncil (orchestrates agent decisions)
            services.AddSingleton<MetaCouncil>();

            // Register AegisCouncil (high-level orchestrator)
            services.AddSingleton<AegisStreamCouncil>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseWebSockets()
                .UseRouting()
                .UseAuthorization()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });

            // app.UseHttpsRedirection();
        }
    }
}
