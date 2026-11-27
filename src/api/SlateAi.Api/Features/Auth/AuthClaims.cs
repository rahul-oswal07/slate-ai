using System.Security.Claims;

namespace SlateAi.Api.Features.Auth;

/// <summary>Claims in the teacher's session cookie. IDs only, never names or phone numbers.</summary>
public static class AuthClaims
{
    public const string TeacherId = "teacher_id";

    /// <summary>One claim per class she belongs to (T-115); which one the app shows is the browser's choice.</summary>
    public const string ClassId = "class_id";

    public static ClaimsPrincipal Teacher(Guid teacherId, IEnumerable<Guid> classIds, string authenticationType)
    {
        var claims = new List<Claim> { new(TeacherId, teacherId.ToString()) };
        claims.AddRange(classIds.Distinct().Select(id => new Claim(ClassId, id.ToString())));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType));
    }

    public static Guid? GetTeacherId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(TeacherId), out var id) ? id : null;

    /// <summary>Every class she can open, from the session.</summary>
    public static IReadOnlyList<Guid> GetClassIds(this ClaimsPrincipal user) =>
        user.FindAll(ClassId).Select(c => Guid.TryParse(c.Value, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList();

    /// <summary>Whether she belongs to <paramref name="classId"/>: the check for every class route (T-115).</summary>
    public static bool IsInClass(this ClaimsPrincipal user, Guid classId) =>
        user.FindAll(ClassId).Any(c => Guid.TryParse(c.Value, out var id) && id == classId);
}
