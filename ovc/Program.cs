using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ovc;

public static class Program
{
    private const string OPEN_AI_URL = "https://api.openai.com/v1/chat/completions";
    
    public static async Task Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Use: dotnet run <caminho_da_imagem> <prompt>");
            return;
        }
        
        var imagePath = args[0];
        var prompt = args[1];
        
        if (!File.Exists(imagePath))
        {
            Console.WriteLine("Error: File not found.");
            return;
        }

        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
        {
            Console.WriteLine("Error: The OPENAI_API_KEY environment variable is not set.");
            return;
        }

        var base64Image = GetBase64Image(imagePath);

        using HttpClient client = new();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",  apiKey);
        var apiRequest = new ApiRequest(prompt, base64Image);
        var jsonContent = JsonSerializer.Serialize(apiRequest);
        Console.WriteLine(jsonContent);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(OPEN_AI_URL, content);
        var result = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse>(result);
        
        if(apiResponse?.choices == null || apiResponse.choices.Length == 0)
        {
            Console.WriteLine("WARNING: Unable to deserialize the API response.");
            Console.WriteLine(result);
            return;
        }
        
        Console.WriteLine(apiResponse.choices[0].message.content);
        DumpToFile(apiResponse.choices[0].message.content, imagePath);
    }
    
    private static string GetBase64Image(string imagePath)
    {
        var bytes = File.ReadAllBytes(imagePath);
        return Convert.ToBase64String(bytes);
    }
    
    private static void DumpToFile(string content, string filePath)
    {
        var destinationFilePath = Path.ChangeExtension(filePath, ".txt");
        File.WriteAllText(destinationFilePath, content);
    }
}