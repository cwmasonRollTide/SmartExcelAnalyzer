using System.Diagnostics.CodeAnalysis;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace API.Properties;

/// <summary>
/// SwaggerFileOperationFilter
/// This class makes it so that the Swagger UI can accept file uploads by file picker.
/// </summary>
[ExcludeFromCodeCoverage]
public class SwaggerFileOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var fileUploadMime = "multipart/form-data";
        
        if (operation.RequestBody is null)
            return;

        if (!operation.RequestBody.Content.Any(x => x.Key.Equals(fileUploadMime, StringComparison.InvariantCultureIgnoreCase)))
            return;

        var fileParams = context.MethodInfo.GetParameters().Where(p => p.ParameterType == typeof(IFormFile));
        operation.RequestBody.Content[fileUploadMime].Schema.Properties = 
            fileParams.ToDictionary(k => k.Name!, 
            v => new OpenApiSchema()
            {
                Type = "string",
                Format = "binary"
            });
    }
}