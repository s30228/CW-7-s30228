using System.Text.Json.Serialization;

namespace CW7.Models.DTOs;

public class TripGetDTO
{
    [JsonPropertyOrder(0)]
    public int IdTrip { get; set; }
    [JsonPropertyOrder(1)]
    public string Name { get; set; }
    [JsonPropertyOrder(2)]
    public string? Description { get; set; }
    [JsonPropertyOrder(3)]
    public DateTime DateFrom { get; set; }
    [JsonPropertyOrder(4)]
    public DateTime DateTo { get; set; }
    [JsonPropertyOrder(5)]
    public int MaxPeople { get; set; }
}