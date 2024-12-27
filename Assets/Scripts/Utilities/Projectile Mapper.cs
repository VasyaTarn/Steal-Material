using System.Collections.Generic;
using System;

public enum ProjectileType
{
    Basic,
    Fire,
    Plant
}

public class ProjectileMapper
{
    private static readonly Dictionary<ProjectileType, string> ProjectileKeys = new()
    {
        { ProjectileType.Basic, "Basic_range" },
        { ProjectileType.Fire, "Fire Projectile" },
        { ProjectileType.Plant, "Plant_range" },
    };

    public static string GetProjectileKey(ProjectileType type)
    {
        return ProjectileKeys.TryGetValue(type, out var key) ? key : throw new ArgumentException("Invalid projectile type.");
    }
}
