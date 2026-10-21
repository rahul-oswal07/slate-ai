namespace SlateAi.Api.Features.Auth;

/// <summary>
/// Lets a request through only if the route's <c>classId</c> is the signed-in teacher's class.
/// Another class is 404, so the response doesn't reveal that it exists.
/// </summary>
public sealed class ClassOwnerFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        var owns = Guid.TryParse(http.GetRouteValue("classId") as string, out var routeClassId)
            && http.User.GetClassId() == routeClassId;
        return owns ? next(context) : ValueTask.FromResult<object?>(TypedResults.NotFound());
    }
}

public static class ClassOwnerExtensions
{
    /// <summary>Requires a signed-in teacher who owns the class in the route.</summary>
    public static RouteHandlerBuilder RequireClassOwner(this RouteHandlerBuilder builder) =>
        builder
            .RequireAuthorization()
            .AddEndpointFilter<ClassOwnerFilter>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);
}
