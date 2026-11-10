using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using OpenApi.BffOpenApiDocumentParser;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var inputFileOption = new Option<string>("--input-file", "-i")
        {
            Description = "The input file to be processed",
            Required = true
        };

        var apiPathOption = new Option<string>("--api-path", "-a")
        {
            Description = "The API path to be used during transformation",
            Required = true
        };

        var serverUrlOption = new Option<string>("--serverUrl", "-s")
        {
            Description = "The server URL to be used during transformation",
            Required = true
        };

        var outputPathOption = new Option<string>("--output-path", "-o")
        {
            Description = "The output directory where the modified file will be saved",
            Required = true
        };

        var rootCommand = new RootCommand("OpenApi.BffOpenApiDocumentParser")
        {
            inputFileOption,
            apiPathOption,
            serverUrlOption,
            outputPathOption
        };

        rootCommand.SetAction(async parseResult =>
        {
            var inputFile = parseResult.GetValue(inputFileOption);
            var outputPath = parseResult.GetValue(outputPathOption);
            var apiPath = parseResult.GetValue(apiPathOption);
            var serverUrl = parseResult.GetValue(serverUrlOption);

            if (!File.Exists(inputFile))
            {
                Console.WriteLine($"File not found: {inputFile}");
                return;
            }

            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            string destFile = Path.Combine(outputPath, Path.GetFileName(inputFile));
            await ModifyAndCopyFile(inputFile, apiPath, serverUrl, destFile);
        });

        var parseResult = rootCommand.Parse(args);
        return await parseResult.InvokeAsync();
    }


   static async Task ModifyAndCopyFile(string sourceFile, string apiPath, string serverUrl, string destFile)
    {
        // Read the file content
        using var input = File.OpenRead(sourceFile);

        // Write the modified content to the destination file
        using var output = File.Create(destFile);

        await OpenApiTransformer.TransformOpenApiDocumentForBff(input, output, new Uri(serverUrl), apiPath);
    }

}
