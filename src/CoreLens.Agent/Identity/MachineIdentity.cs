namespace CoreLens.Agent.Identity;

public static class MachineIdentity
{
    public static Guid GetOrCreate()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "CoreLens");
        Directory.CreateDirectory(dir);

        var path = Path.Combine(dir, "machine-id");
        if (File.Exists(path) && Guid.TryParse(File.ReadAllText(path).Trim(), out var existing))
        {
            return existing;
        }

        var id = Guid.NewGuid();
        File.WriteAllText(path, id.ToString("D"));
        return id;
    }
}
