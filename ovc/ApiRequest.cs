namespace ovc;

public class ApiRequest
{
    public string model { get; set; } = "gpt-4o";
    public List<Message> messages { get; set; }
    public int max_tokens { get; set; } = 500;

    public ApiRequest(string prompt, string base64Image)
    {
        messages = new List<ApiRequest.Message>
        {
            new() { role = "system", content = Prompts.SystemPrompt },
            new() { role = "user", content = $"{prompt}\n\n![image](data:image/jpeg;base64,{base64Image})" }
        };
    }

    public class Message
    {
        public string role { get; set; }
        public string content { get; set; }
    }
}

