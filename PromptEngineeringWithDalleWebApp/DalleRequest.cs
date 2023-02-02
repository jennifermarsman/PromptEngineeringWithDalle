namespace PromptEngineeringWithDalleWebApp
{

    public class DalleRequest
    {
        public DalleRequest(string caption, string resolution)
        {
            this.caption = caption;
            this.resolution = resolution;
        }

        public string caption { get; set; } = "";

        // Supported values are “256x256”, “512x512”, “1024x1024”
        public string resolution { get; set; } = "1024x1024";


    }
}
