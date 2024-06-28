using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using myreka_sunlife.models;

namespace myreka_sunlife
{
    public class myreka
    {
        private readonly ILogger<myreka> _logger;

        private static readonly string BaseUrl = "https://rsp-demo-ai-service.cognitiveservices.azure.com/customvision/v3.0/Prediction/add5be53-368e-4459-b3d5-9491f866c93c/detect/iterations/";

        public myreka(ILogger<myreka> logger)
        {
            _logger = logger;
           
        }

        [Function("myreka")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string inputPath = req.Query["inputPath"];
            if (string.IsNullOrWhiteSpace(inputPath))
            {
                try
                {
                    using var reader = new StreamReader(req.Body);
                    var requestBody = await reader.ReadToEndAsync();
                    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(requestBody);
                    inputPath = data?["inputPath"];
                }
                catch(Exception ex)
                {
                    return new BadRequestObjectResult(ex.Message);
                }
            }

            if (string.IsNullOrWhiteSpace(inputPath))
            {
                return new BadRequestObjectResult("Please provide a valid input path.");
            }

            try
            {
                using HttpClient client = new ();
                client.BaseAddress = new Uri(BaseUrl);
                client.DefaultRequestHeaders.Add("Prediction-Key", Environment.GetEnvironmentVariable("PREDICTION_KEY") ?? throw new InvalidOperationException("Prediction key is missing"));

                string body = "{\"Url\":\"" + $"{inputPath}" + "\"}";
                var content = new StringContent(body, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("Iteration9/url", content);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation(responseBody);

                var prediction = JsonSerializer.Deserialize<ImagePrediction>(responseBody);
                
                if (prediction?.predictions?.Count > 0)
                {
                    var firstPrediction = prediction.predictions[0];
                    return new OkObjectResult(new 
                    { 
                        Message = "Found a logo",
                        Left = firstPrediction.boundingBox.left, 
                        Top = firstPrediction.boundingBox.top,
                        Width = firstPrediction.boundingBox.width,
                        Height = firstPrediction.boundingBox.height 
                    });
                }
                else
                {
                    return new OkObjectResult("No predictions found.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                return new BadRequestObjectResult(ex.Message);
            }
        }
    }
}