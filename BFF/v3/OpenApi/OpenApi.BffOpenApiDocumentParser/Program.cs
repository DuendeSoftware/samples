using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using OpenApi.BffOpenApiDocumentParser;

class Program
{
    static void Main(string[] args)
    {
        var rootCommand = new RootCommand
        {
            new Option<string>(
                aliases: ["-i", "--input-file"],
                description: "The input file to be processed")
            {
                Required = true
            },
            new Option<string>(
                aliases: ["-a", "--api-path"],
                description: "The API path to be used during transformation")
            {
                Required = true
            },
            new Option<string>(
                aliases: ["-s", "--serverUrl"],
                description: "The server URL to be used during transformation")
            {
                Required = true
            },
            new Option<string>(
                aliases: ["-o", "--output-path"],
                description: "The output directory where the modified file will be saved")
            {
                Required = true
            }
        };

        rootCommand.Description = "OpenApi.BffOpenApiDocumentParser";

        rootCommand.Handler = CommandHandler.Create<string, string, string, string>(async (inputFile, apiPath, serverUrl, outputPath) =>
        {
            //if (string.IsNullOrEmpty(infile) || string.IsNullOrEmpty(apiPath) || string.IsNullOrEmpty(serverUrl) || string.IsNullOrEmpty(outputpath))
            //{
            //    Console.WriteLine("Usage: OpenApi.BffOpenApiDocumentParser -i <infile> --apiPath <apiPath> --serverUrl <serverUrl> --outputpath <outputpath>");
            //    return;
            //}

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

        rootCommand.Invoke(args);
    }

    static async Task ModifyAndCopyFile(string sourceFile, string apiPath, string serverUrl, string destFile)
    {
        // Read the file content
        var input = File.OpenRead(sourceFile);

        // Write the modified content to the destination file
        using var output = File.OpenWrite(destFile);

        await OpenApiTransformer.TransformOpenApiDocumentForBff(input, output, new Uri(serverUrl), apiPath);
    }

}
