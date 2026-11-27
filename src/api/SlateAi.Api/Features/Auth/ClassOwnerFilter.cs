namespace SlateAi.Api.Features.Auth;

/// <summary>
/// Lets a request through only if the signed-in teacher belongs to the route's <c>classId</c> (any of her classes, T-115).
/// Another class is 404, so the response doesn't reveal that it exists.
/// </summary>
public sealed class ClassOwnerFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        var owns = Guid.TryParse(http.GetRouteValue("classId") as string, out var routeClassId)
            && http.User.IsInClass(routeClassId);
        return owns ? next(context) : ValueTask.FromResult<object?>(TypedResults.NotFound());
    }
}

public static class ClassOwnerExtensions
{
    /// <summary>Requires a signed-in teacher who belongs to the class in the route.</summary>
    public static RouteHandlerBuilder RequireClassOwner(this RouteHandlerBuilder builder) =>
        builder
            .RequireAuthorization()
            .AddEndpointFilter<ClassOwnerFilter>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);
}
