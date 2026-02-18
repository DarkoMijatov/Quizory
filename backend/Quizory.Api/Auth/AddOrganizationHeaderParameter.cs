using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Quizory.Api.Auth
{
    public class AddOrganizationHeaderParameter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            operation.Parameters ??= new List<OpenApiParameter>();

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-Organization-Id",
                In = ParameterLocation.Header,
                Description = "Active organization ID (GUID)",
                Required = false,
                Schema = new OpenApiSchema
                {
                    Type = "string",
                    Format = "uuid"
                }
            });
        }
    }
}
