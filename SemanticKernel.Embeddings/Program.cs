using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

var deploymentName = "text-embedding-3-large";
var apiKey = configuration["CHAT_GPT_API_KEY"] ?? throw new InvalidOperationException("CHAT_GPT_API_KEY not found in configuration");

#pragma warning disable SKEXP0010
IKernelBuilder kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.AddOpenAIEmbeddingGenerator(
    modelId: deploymentName,          // Name of the embedding model, e.g. "text-embedding-ada-002".
    apiKey: apiKey,
    httpClient: new HttpClient(), // Optional; if not provided, the HttpClient from the kernel will be used
    dimensions: 1536              // Optional number of dimensions to generate embeddings with.
);
Kernel kernel = kernelBuilder.Build();
var embeddingGenerator = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

var embeddings = await embeddingGenerator.GenerateAsync(
    [
        "sample text 1",
        "sample text 2",
        "dog",
        "cat"
    ]);

foreach (var embedding in embeddings)
{
    ReadOnlyMemory<float> vector = embedding.Vector;
    Console.WriteLine($"Embedding Vector: [{string.Join(", ", vector.ToArray()[..10])}]");
}