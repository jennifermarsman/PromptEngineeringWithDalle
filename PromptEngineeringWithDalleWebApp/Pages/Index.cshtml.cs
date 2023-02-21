using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using System.Reflection.Metadata;
using static System.Net.Mime.MediaTypeNames;
using System.Text.Json.Serialization;

// This was helpful:  
// https://www.learnrazorpages.com/razor-pages/forms
// https://www.aspsnippets.com/Articles/ASPNet-Core-Using-Multiple-Submit-Buttons-in-Razor-Pages.aspx

// TODO:
// Create collection of examples - baseball stadium
// Create next button to iterate through
// Move appropriate settings to appsettings.json
// Lock down to Microsoft employees?  
// Alex feedback:
// Originally leave the right image blank.   
// Use DV3 or something to build the prompts?  Maybe not for RAI.  
// For hardcoding, include prompt hints.  (Tell why prompt works well to generate image.)  
// Include link to general prompt guidance.  
// Scale plan?  Release to Microsoft only?  If so, need auth.  

namespace PromptEngineeringWithDalleWebApp.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<IndexModel> _logger;
        private static HttpClient client = new HttpClient();

        // These values will be loaded from configuration file appsettings.json.  
        private string apiKey;
        private string resourceName;

        [BindProperty]
        public string txtInput { get; set; } = "";

        public static string lastContentUrl { get; set; } = "";

        public IndexModel(IConfiguration configuration, ILogger<IndexModel> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // Load configuration.  
            resourceName = _configuration["AzureOpenAIResourceName"];
            apiKey = _configuration["AzureOpenAIResourceKey"];

            // Load image configuration file
            //            string jsonString = File.ReadAllText("ImagePromptConfig.json");
            //using (StreamReader r = new JsonTextReader("ImagePromptConfig.json"))
            //{
            //    string json = r.ReadToEnd();
            //    dynamic array = JsonConvert.DeserializeObject(json);
            //}
        }

        //public IndexModel(ILogger<IndexModel> logger)
        //{
        //    _logger = logger;
        //}

        public void OnGet()
        {
            //await CallDalle("Oil painting of ballerinas warming up in a dance studio");
            ViewData["image1"] = "img/clear.png";
        }

        public async Task OnPostGuess()
        {
            await CallDalle(txtInput);
        }

        public void OnPostReveal()
        {
            ViewData["image1"] = lastContentUrl;
            //ViewData["hiddenPrompt"] = "stained glass window of a wolf howling at the moon";
            //ViewData["hiddenPrompt"] = "lake at sunset with snowflakes falling and pine trees in the background, high-quality digital art";
            ViewData["hiddenPrompt"] = "huskies pulling a dogsled through the snow at sunset with snowflakes falling and pine trees in the background, digital art";
        }

        private async Task CallDalle(string prompt)
        {
            DalleRequest input = new DalleRequest(prompt, "512x512");
            var jsonPayload = JsonSerializer.Serialize(input);
            HttpContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await CallDalleEndpoint(content);
            response.EnsureSuccessStatusCode();
            string operationLocation = response.Headers.GetValues("Operation-Location").First();
            DalleResponse? output = JsonSerializer.Deserialize<DalleResponse>(response.Content.ReadAsStringAsync().Result);
            if (output != null)
            {
                var id = output.id;
                var status = output.status;
                string contentUrl = await PollForDalleImage(operationLocation);

                // Set image src to contentUrl 
                lastContentUrl = contentUrl;
                ViewData["image1"] = contentUrl;
                return;
            }
        }

        // Call Azure OpenAI endpoint
        // Reference: https://learn.microsoft.com/en-us/azure/cognitive-services/openai/reference
        private async Task<HttpResponseMessage> CallDalleEndpoint(HttpContent content)
        {
            string baseUrl = "https://" + resourceName + ".openai.azure.com";   // TODO: error/injection checking
            string url = baseUrl + "/dalle/text-to-image?api-version=2022-08-03-preview";

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("api-key", apiKey);

            var response = await client.PostAsync(url, content);
            return response;
        }

        private async Task<string> PollForDalleImage(string operationLocation)
        {
            var response2 = await client.GetAsync(operationLocation);
            //Debug.WriteLine(response2.ToString());
            response2.EnsureSuccessStatusCode();
            DalleImageResponse? output2 = JsonSerializer.Deserialize<DalleImageResponse>(response2.Content.ReadAsStringAsync().Result);
            while (output2 != null && output2.status != "Succeeded")
            {
                Thread.Sleep(500);  // wait for 0.5 second
                response2 = await client.GetAsync(operationLocation);
                //Debug.WriteLine(response2.ToString());
                response2.EnsureSuccessStatusCode();
                output2 = JsonSerializer.Deserialize<DalleImageResponse>(response2.Content.ReadAsStringAsync().Result);
            }
            
            if (output2 != null && output2.result != null)
            {
                var caption = output2.result.caption;
                var contentUrl = output2.result.contentUrl;
                var contentUrlExpiresAt = output2.result.contentUrlExpiresAt;
                var createdDateTime = output2.result.createdDateTime;

                return contentUrl;
            }

            // This shouldn't happen on successful path.  
            return "";
        }

        
    }
}