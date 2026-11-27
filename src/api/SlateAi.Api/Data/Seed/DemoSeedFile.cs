using System.Text.Json;

namespace SlateAi.Api.Data.Seed;

/// <summary>
/// The fictional demo class as written in <c>data/seed/demo-class.json</c>; read by the seeder and the demo Reset.
/// <c>Teacher</c> is the class's main teacher; <c>CoTeachers</c> also sign in to the same class. <c>OtherClasses</c> are
/// more fictional classes (T-115), each with its main teacher from <c>Teacher</c> or <c>CoTeachers</c>; the demo story and
/// Reset are only about <c>Class</c>.
/// </summary>
public sealed record DemoSeedFile(DemoSeedFile.SeedTeacher Teacher, DemoSeedFile.SeedClass Class, IReadOnlyList<DemoSeedFile.SeedChild> Children,
    IReadOnlyList<DemoSeedFile.SeedTeacher>? CoTeachers = null, IReadOnlyList<DemoSeedFile.SeedOtherClass>? OtherClasses = null)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<DemoSeedFile> LoadAsync(string seedFile, CancellationToken cancellationToken)
    {
        var path = Path.Combine(AppContext.BaseDirectory, seedFile);
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<DemoSeedFile>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException($"Seed file '{path}' is empty.");
    }

    public sealed record SeedTeacher(Guid Id, string DisplayName, string? Phone);

    public sealed record SeedClass(Guid Id, int Grade, string Medium, string? Name = null);

    /// <summary>A class beside the demo one: its main teacher is <paramref name="TeacherId"/>, its only member at first.</summary>
    public sealed record SeedOtherClass(
        Guid Id, string Name, int Grade, string Medium, Guid TeacherId, IReadOnlyList<SeedChild> Children);

    public sealed record SeedChild(
        Guid Id, int RollNo, string FirstName, string HomeLanguage, bool HasPhone, string? ParentPhone, string? Consent)
    {
        public Entities.Child ToEntity(Guid classId) => new()
        {
            Id = Id,
            ClassId = classId,
            RollNo = RollNo,
            FirstName = FirstName,
            HomeLanguage = HomeLanguage,
            HasPhone = HasPhone,
            ParentPhone = ParentPhone,
            Consent = Consent ?? "no",
        };
    }
}
