using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ovc;

public static class Program
{
    private const string OPEN_AI_URL = "https://api.openai.com/v1/chat/completions";
    
    public static async Task Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Uso: dotnet run <caminho_da_imagem> <prompt>");
            return;
        }

        var imagePath = args[0];
        var prompt = args[1];

        if (!File.Exists(imagePath))
        {
            Console.WriteLine("Erro: O arquivo de imagem não foi encontrado.");
            return;
        }

        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
        {
            Console.WriteLine("Erro: A variável de ambiente OPENAI_API_KEY não está definida.");
            return;
        }

        var imageBytes = await File.ReadAllBytesAsync(imagePath);
        var base64Image = Convert.ToBase64String(imageBytes);

        using HttpClient client = new();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",  apiKey);
        
        const string systemPrompt = 
"""
Instruction for Image Description (LoRA Training)

You are an AI model trained to generate highly detailed descriptions of images for LoRA training. 
Your goal is to provide a concise yet comprehensive single-paragraph description of a given photo, focusing on key visual attributes without making assumptions. 
Follow these guidelines:

    1.  Begin every description with “A photo of ” (if applicable).
    2.  Describe the mouth (e.g., smiling, open, closed), eyes (color and looking direction), body position, clothing, hairstyle (including color), and overall mood.
    3.  Include relevant details about the scene, objects, and background if visible.
    4.  Note any striking, unusual, or visually distinctive features.
    5.  Mention noticeable colors, textures, or patterns present in the image.
    6.  If a detail is unclear or not visible, skip it rather than guessing.
    7.  Keep the response succinct, specific, and in a single paragraph with no bullet points.

Adhere strictly to these rules to maintain consistency and accuracy in descriptions.
""";
        var systemRole = new { role = "system", content = systemPrompt };
        var userRole = new { role = "user", content = new object[] 
            { 
                new { type = "text", text = prompt },
                new { type = "image_url", image_url = new { url = $"data:image/jpeg;base64,{base64Image}" } }
            } 
        };

        var requestBody = new
        {
            model = "gpt-4o",
            messages = new object[]
            {
                systemRole,
                userRole
            },
            max_tokens = 500
        };

        var jsonContent = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(OPEN_AI_URL, content);
        var result = await response.Content.ReadAsStringAsync();

        Console.WriteLine("Resposta da API:");
        Console.WriteLine(result);
    }
}