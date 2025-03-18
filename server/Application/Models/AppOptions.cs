using System.ComponentModel.DataAnnotations;

// ReSharper disable InconsistentNaming

namespace Application.Models;

public sealed class AppOptions
{
    [Required] public string JwtSecret { get; set; } = null!;
    [Required] public string DbConnectionString { get; set; } = null!;
    [Required] public string Pass { get; set; } = null!;
    public bool Seed { get; set; } = true;
    public int PORT { get; set; } = 8080;
    public int WS_PORT { get; set; } = 8181;
    public int REST_PORT { get; set; } = 5000;
}
