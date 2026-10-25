namespace SlateAi.Api.Data.Entities;

/// <summary>A teacher who can sign in to a class. <see cref="Class.TeacherId"/> stays the class's main teacher (named to families).</summary>
public sealed class ClassTeacher
{
    public Guid ClassId { get; set; }
    public Guid TeacherId { get; set; }
}
