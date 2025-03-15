namespace ovc;

public class ApiRequest
{
    public string model { get; set; } = "gpt-4-vision-preview";
    public List<Message> messages { get; set; }
    public int max_tokens { get; set; } = 500;

    public ApiRequest(string prompt, string base64Image, string imageName)
    {
        var systemPrompt = Prompts.SystemPromptSmall.Replace("<imageName>", imageName);
        messages = new List<ApiRequest.Message>
        {
            new() { role = "system", content = systemPrompt },
            new() { role = "user", content = $"{prompt}", image_url = new Image { url = $"data:image/jpeg;base64,{base64Image}" } }
        };
    }

    public class Message
    {
        public string role { get; set; }
        public string content { get; set; }
        public Image image_url { get; set; }
    }
    
    public class Image
    {
        public string url { get; set; }
    }
}

