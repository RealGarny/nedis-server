
using Microsoft.AspNetCore.Http;

namespace Nedis.Server.Common.Web;

public static class ApiProblem
{
    public static IResult Create(int statusCode, string code, string detail) =>
        Results.Problem(
            detail: detail,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?> { ["code"] = code }
        );
}