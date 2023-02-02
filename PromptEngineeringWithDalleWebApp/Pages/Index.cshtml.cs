﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using System.Reflection.Metadata;
using static System.Net.Mime.MediaTypeNames;

// This was helpful:  
// https://www.learnrazorpages.com/razor-pages/forms
// https://www.aspsnippets.com/Articles/ASPNet-Core-Using-Multiple-Submit-Buttons-in-Razor-Pages.aspx

// TODO:
// Create collection of examples - baseball stadium
// Create next button to iterate through
// Rework UI to have one image instead of 4 output from DALL-E


namespace PromptEngineeringWithDalleWebApp.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private static HttpClient client = new HttpClient();
        
        [BindProperty]
        public string txtInput { get; set; } = "";

        public string lastContentUrl { get; set; } = "";

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public async Task OnGet()
        {
            //await CallDalle(txtInput);
            // TODO: this seems to be broken now.  
            await CallDalle("Oil painting of ballerinas warming up in a dance studio");
        }

        public async Task OnPostGuess()
        {
            await CallDalle(txtInput);
        }

        public void OnPostReveal()
        {
            // TODO: revealing the prompt removes the last image
            //ViewData["image1"] = lastContentUrl;
            ViewData["hiddenPrompt"]= "stained glass window of a wolf howling at the moon";
        }

        private async Task CallDalle(string prompt)
        {
            //var image = document.createElement("img");
            //var imageParent = document.getElementById("body");
            //image.id = "id";
            //image.className = "class";
            //image.src = searchPic.src;            // image.src = "IMAGE URL/PATH"
            //imageParent.appendChild(image);


            //var x = Request("txtInput");
            //Request.Body["txtInput"];
            //string test = Request.Form["txtInput"].ToString();



            DalleRequest input = new DalleRequest(prompt, "256x256");
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
                //Debug.WriteLine(response2.ToString());
                //response2.EnsureSuccessStatusCode();
                //DalleImageResponse? output2 = JsonSerializer.Deserialize<DalleImageResponse>(response2.Content.ReadAsStringAsync().Result);
                //while (output2 != null && output2.status != "Succeeded")
                //{
                //    Thread.Sleep(500);  // wait for 0.5 second
                //    response2 = await PollForDalleImage(operationLocation);
                //    Debug.WriteLine(response2.ToString());
                //    response2.EnsureSuccessStatusCode();
                //    output2 = JsonSerializer.Deserialize<DalleImageResponse>(response2.Content.ReadAsStringAsync().Result);
                //}
                //if (output2 != null && output2.result != null)
                //{
                //    var caption = output2.result.caption;
                //    var contentUrl = output2.result.contentUrl;
                //    var contentUrlExpiresAt = output2.result.contentUrlExpiresAt;
                //    var createdDateTime = output2.result.createdDateTime;

                // Set image src to contentUrl 
                lastContentUrl = contentUrl;
                ViewData["image1"] = contentUrl;
                return;
                //}
            }
        }

        // Call Azure OpenAI endpoint
        // Reference: https://learn.microsoft.com/en-us/azure/cognitive-services/openai/reference
        private async static Task<HttpResponseMessage> CallDalleEndpoint(HttpContent content, string resourceName = "jen-aoai")
        {
            string apiKey = "";
            string baseUrl = "https://" + resourceName + ".openai.azure.com";   // TODO: error/injection checking
            string url = baseUrl + "/dalle/text-to-image?api-version=2022-08-03-preview";

            // System.InvalidOperationException: 'Misused header name, 'Content-Type'. Make sure request headers are used with HttpRequestMessage, response headers with HttpResponseMessage, and content headers with HttpContent objects.'

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            //client.DefaultRequestHeaders.Add("Content-Type", "application/json");
            client.DefaultRequestHeaders.Add("api-key", apiKey);

            var response = await client.PostAsync(url, content);
            return response;
        }

        private async static Task<string> PollForDalleImage(string operationLocation, string resourceName = "jen-aoai")
        {

            //// TODO: put this in for loop to poll?  Or use a timer?
            ////var response;
            ////while ()
            ////{ 
            //    var response = await client.GetAsync(operationLocation);
            ////}
            //return response;

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