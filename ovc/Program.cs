using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace ovc;

public static class Program
{
    private const string OPEN_AI_URL = "https://api.openai.com/v1/chat/completions";
    private const int MAX_DIMENSION = 396;

    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    
    public static async Task Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Use: dotnet run <caminho_da_imagem> <prompt>");
            return;
        }
        
        var imagePath = args[0];
        var prompt = args[1];

        while (true)
        {
            if (!File.Exists(imagePath))
            {
                Console.WriteLine("Error: File not found.");
                return;
            }

            var destinationFilePath = Path.ChangeExtension(imagePath, ".txt");
            if (File.Exists(destinationFilePath))
            {
                Console.WriteLine("Error: The destination file already exists.");
                return;
            }

            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("Error: The OPENAI_API_KEY environment variable is not set.");
                return;
            }

            var base64Image = ConvertToJpegAsBase64String(imagePath);
            if (base64Image == string.Empty)
            {
                return;
            }

            var imageName = Path.GetFileName(imagePath);
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            var jsonContent = CreateJson(prompt, base64Image, imageName);

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(OPEN_AI_URL, content);
            var result = await response.Content.ReadAsStringAsync();

            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(result);

            if (apiResponse?.choices == null || apiResponse.choices.Length == 0)
            {
                PrintError("WARNING: Unable to deserialize the API response.");
                Console.WriteLine(result);
                return;
            }

            var description = apiResponse.choices[0].message.content;
            if (result.Contains("I'm sorry") || result.Contains("I'm unable"))
            {
                PrintError("WARNING: The API refused to provide a description for this image.");
                Console.WriteLine(description);
                return;
            }

            if (result.Contains("error") && result.Contains("Rate limit reached"))
            {
                PrintError("WARNING: The API rate limit reached. Will wait 5 seconds and try again.");
                Console.WriteLine(description);
                Thread.Sleep(5000);
                continue;
            }
            
            Console.WriteLine(description);
            DumpToFile(description, destinationFilePath);
            return;
        }
    }
   
    private static void DumpToFile(string content, string filePath)
    {
        if(File.Exists(filePath))
        {
            PrintError("WARNING: The destination file already exists.");
            return;
        }
        File.WriteAllText(filePath, content);
    }
    
    private static void PrintError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
    }
    
    private static string ConvertToJpegAsBase64String(string imagePath)
    {
        try
        {
            using var originalImage = Image.Load(imagePath);
        
            var resizeOptions = new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(MAX_DIMENSION, MAX_DIMENSION)
            };
            originalImage.Mutate(x => x.Resize(resizeOptions));
        
            // aparentemente, se ler uma imagem jpg diretamente, a API OpenAI não consegue processar
            // então, salva a imagem em png e lê novamente
            using var pngMemStream = new MemoryStream();
            originalImage.Save(pngMemStream, new PngEncoder());
            using var pngImage = Image.Load(pngMemStream.ToArray());
        
            using var jpgMemStream = new MemoryStream();
            pngImage.Save(jpgMemStream, new JpegEncoder() { Quality = 85 });
        
            return Convert.ToBase64String(jpgMemStream.ToArray());
        }
        catch (Exception e)
        {
            PrintError($"Failed processing {imagePath}");
            Console.WriteLine(e.Message);
            return string.Empty;
        }
    }

    private static string CreateJson(string prompt, string base64Image, string imageName)
    {
        var json = """
                    {"model":"gpt-4o",
                     "messages":[
                       {"role":"system","content":"<system_role_content>"},
                       {"role":"user",
                        "content":[
                          {"type":"text","text":"<user_text_content>"},
                          {"type":"image_url","image_url":{"url":"data:image/jpeg;base64,<base64_image>"}}
                        ]
                       }
                     ],
                     "max_tokens":500
                    } 
                   """;

        json = json.Replace("<system_role_content>", Prompts.SystemPrompt.Replace("\r", " ").Replace("\n", " "));
        json = json.Replace("<user_text_content>", prompt);
        json = json.Replace("<base64_image>", base64Image);
        
        return json;
    }
}