// See https://aka.ms/new-console-template for more information
using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI;
using StepWise.Core;
using StepWise.WebAPI;

var host = Host.CreateDefaultBuilder()
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.UseUrls("http://localhost:5123");
    })
    .UseStepWiseServer()
    .Build();

await host.StartAsync();

var stepWiseClient = host.Services.GetRequiredService<StepWiseClient>();

var instance = new EquationSolver();
var workflow = Workflow.CreateFromInstance(instance);
stepWiseClient.AddWorkflow(workflow);

await host.WaitForShutdownAsync();

public class EquationSolver
{
    private string _apiKey;
    private IAgent _agent;

    [StepWiseUIImageInput(description: "Please provide the image of the equation")]
    public async Task<StepWiseImage?> InputImage()
    {
        return null;
    }

    [StepWiseUITextInput(description: "Please provide the openai api key if env:OPENAI_API_KEY is not set, otherwise leave empty and submit")]
    public async Task<string?> OpenAIApiKey()
    {
        return null;
    }

    [Step(description: "Validate the openai api key")]
    public async Task<string> ValidateOpenAIApiKey(
        [FromStep(nameof(OpenAIApiKey))] string apiKey)
    {
        if (Environment.GetEnvironmentVariable("OPENAI_API_KEY") is not string envApiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new Exception("Please provide the openai api key");
            }

            _apiKey = apiKey;
            var client = new OpenAIClient(_apiKey);
            var chatClient = client.GetChatClient("gpt-4o");

            _agent = new OpenAIChatAgent(
                chatClient: chatClient,
                name: "assistant",
                systemMessage: "Please solve the equation in the image"
            )
            .RegisterMessageConnector();

            return "Use provided api key";
        }
        else
        {
            _apiKey = envApiKey;
            var client = new OpenAIClient(_apiKey);
            var chatClient = client.GetChatClient("gpt-4o");

            _agent = new OpenAIChatAgent(
                chatClient: chatClient,
                name: "assistant",
                systemMessage: "Please solve the equation in the image"
            )
            .RegisterMessageConnector();
            return "Use env:OPENAI_API_KEY";
        }
    }

    [Step(description: "Valid image input to confirm it contains exactly one equation")]
    [DependOn(nameof(InputImage))]
    [DependOn(nameof(ValidateOpenAIApiKey))]
    public async Task<bool> ValidateImageInput(
        [FromStep(nameof(InputImage))] StepWiseImage image)
    {
        var prompt = """
            Please confirm the image contains exactly one mathematical equation.
            If the image satisfies the condition, say `yes, the image contains exactly one equation`.
            Otherwise, say `no, the image does not contain exactly one equation`.
            """;
        var imageMessage = new ImageMessage(Role.User, image.Blob!);
        var userMessage = new TextMessage(Role.User, prompt);
        var multiModalMessage = new MultiModalMessage(Role.User, [imageMessage, userMessage]);
        var response = await _agent.SendAsync(multiModalMessage);
        return response.GetContent()?.ToLower().Contains("yes, the image contains exactly one equation") is true;
    }

    [Step(description: "Extract the equation from the image into latex format")]
    [DependOn(nameof(ValidateImageInput))]
    public async Task<string?> ExtractEquationFromImage(
        [FromStep(nameof(ValidateImageInput))] bool valid,
        [FromStep(nameof(InputImage))] StepWiseImage image)
    {
        if (!valid)
        {
            return null;
        }

        var prompt = "Please extract the equation from the image into latex format, save your response within ```equation and ```";
        var imageMessage = new ImageMessage(Role.User, image.Blob!);
        var userMessage = new TextMessage(Role.User, prompt);
        var multiModalMessage = new MultiModalMessage(Role.User, [imageMessage, userMessage]);
        var response = await _agent.SendAsync(multiModalMessage);
        return response.GetContent();
    }

    [Step(description: "Solve the equation")]
    [DependOn(nameof(ExtractEquationFromImage))]
    public async Task<string?> SolveEquation(
        [FromStep(nameof(InputImage))] StepWiseImage image,
        [FromStep(nameof(ExtractEquationFromImage))] string equation)
    {
        var prompt = $"""
            Please solve the following equation in latex format:

            {equation}
            """;
        var userMessage = new TextMessage(Role.User, prompt);
        var imageMessage = new ImageMessage(Role.User, image.Blob!);
        var multiModalMessage = new MultiModalMessage(Role.User, [imageMessage, userMessage]);
        var response = await _agent.SendAsync(multiModalMessage);
        return response.GetContent();
    }
}