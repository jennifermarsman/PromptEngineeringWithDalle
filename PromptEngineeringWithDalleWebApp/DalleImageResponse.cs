namespace PromptEngineeringWithDalleWebApp
{
    public class DalleImageResponse
    {
        public string id { get; set; } = "";
        public Result? result { get; set; } = null;
        public string status { get; set; } = "";
        public Error? error { get; set; } = null;
    }

    public class Result
    {
        public string caption { get; set; } = "";
        public string contentUrl { get; set; } = "";
        public string contentUrlExpiresAt { get; set; } = "";
        public string createdDateTime { get; set; } = "";
    }

    public class Error
    {
        public string code { get; set; } = "";
        public string message { get; set; } = "";
    }
}
