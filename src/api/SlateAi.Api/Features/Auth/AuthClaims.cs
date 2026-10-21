using System.Security.Claims;

namespace SlateAi.Api.Features.Auth;

/// <summary>Claims in the teacher's session cookie. IDs only, never names or phone numbers.</summary>
public static class AuthClaims
{
    public const string TeacherId = "teacher_id";
    public const string ClassId = "class_id";

    public static ClaimsPrincipal Teacher(Guid teacherId, Guid? classId, string authenticationType)
    {
        var claims = new List<Claim> { new(TeacherId, teacherId.ToString()) };
        if (classId is { } id)
        {
            claims.Add(new Claim(ClassId, id.ToString()));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType));
    }

    public static Guid? GetTeacherId(this ClaimsPrincipal user) => Parse(user, TeacherId);

    public static Guid? GetClassId(this ClaimsPrincipal user) => Parse(user, ClassId);

    private static Guid? Parse(ClaimsPrincipal user, string type) =>
        Guid.TryParse(user.FindFirstValue(type), out var id) ? id : null;
}
