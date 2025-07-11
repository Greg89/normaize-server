using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace Normaize.API.Swagger;

public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var methodInfo = context.MethodInfo;
        
        // Check if this is the upload endpoint
        if (methodInfo.Name == "UploadDataSet" && methodInfo.DeclaringType?.Name == "DataSetsController")
        {
            // Remove the default parameters that Swagger generates incorrectly
            operation.Parameters.Clear();
            
            // Add the correct form data parameters
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, OpenApiSchema>
                            {
                                ["file"] = new OpenApiSchema
                                {
                                    Type = "string",
                                    Format = "binary",
                                    Description = "The file to upload"
                                },
                                ["name"] = new OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "Name for the dataset"
                                },
                                ["description"] = new OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "Optional description for the dataset"
                                }
                            },
                            Required = new HashSet<string> { "file", "name" }
                        }
                    }
                }
            };
        }
    }
} 