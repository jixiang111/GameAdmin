using Admin.NET.Core;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Admin.NET.Core;

/// <summary>
/// 租户头部参数过滤器
/// </summary>
public class TenantHeaderOperationFilter : IOperationFilter
{
    /// <summary>
    /// 应用租户头部参数过滤器
    /// </summary>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<OpenApiParameter>();

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = ClaimConst.TenantId,
            In = ParameterLocation.Header,
            Required = false,
            AllowEmptyValue = true,
            Description = "租户ID（留空表示默认租户）"
        });
    }
}
