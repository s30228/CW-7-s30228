using System.Text.Json.Serialization;

namespace CW7.Models.DTOs;

public class TripCountryGetDTO : TripGetDTO
{
    [JsonPropertyOrder(6)]
    public List<string> Countries { get; set; } = new List<string>();
}