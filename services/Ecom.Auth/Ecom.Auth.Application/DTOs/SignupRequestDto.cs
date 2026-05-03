using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ecom.Auth.Application.DTOs;

public class SignupRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}
