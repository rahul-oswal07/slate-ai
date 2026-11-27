using System.Net;
using SlateAi.Api.Features.Auth;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.Auth;

/// <summary>A teacher in more than one class (T-115): the session lists each, and every class route accepts any of them.</summary>
public sealed class ClassMembershipTests
{
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private static readonly Guid ClassA = Guid.NewGuid();
    private static readonly Guid ClassB = Guid.NewGuid();

    [Fact]
    public void The_session_holds_every_class_once()
    {
        var user = AuthClaims.Teacher(Guid.NewGuid(), [ClassA, ClassB, ClassA], "Test");

        Assert.Equal([ClassA, ClassB], user.GetClassIds());
        Assert.True(user.IsInClass(ClassA));
        Assert.True(user.IsInClass(ClassB));
        Assert.False(user.IsInClass(Guid.NewGuid()));
    }

    [Fact]
    public void A_teacher_without_a_class_is_in_none()
    {
        var user = AuthClaims.Teacher(Guid.NewGuid(), [], "Test");

        Assert.Empty(user.GetClassIds());
        Assert.False(user.IsInClass(ClassA));
    }

    [Fact]
    public async Task A_class_route_opens_for_each_of_her_classes_and_is_404_for_any_other()
    {
        await using var factory = FakeApi.Create();
        using var client = factory.CreateTeacherClient([ClassA, ClassB]);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(CurriculumUrl(ClassA), Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(CurriculumUrl(ClassB), Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(CurriculumUrl(Guid.NewGuid()), Ct)).StatusCode);
    }

    private static string CurriculumUrl(Guid classId) => $"/api/classes/{classId}/curriculum";
}
