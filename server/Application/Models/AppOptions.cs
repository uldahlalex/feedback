using System.ComponentModel.DataAnnotations;

// ReSharper disable InconsistentNaming

namespace Application.Models;

public sealed class AppOptions
{
    [Required] public string JwtSecret { get; set; } = string.Empty!;
    [Required] public string DbConnectionString { get; set; } = string.Empty!;
    [Required] public string Pass { get; set; } = string.Empty!;
    public bool Seed { get; set; } = true;
    public int PORT { get; set; } = 8080;
    public int WS_PORT { get; set; } = 8181;
    public int REST_PORT { get; set; } = 5000;
}
