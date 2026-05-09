using System.Text.Json;
using System.Text.Json.Serialization;

namespace CatalogAPI.Domain.Events;

public record OrderPlacedEvent
{
    public Guid OrderId { get; init; }
    
    public Guid UserId { get; init; }
    
    public Guid GameId { get; init; }
    
    // Aceita tanto string quanto decimal para compatibilidade com MassTransit
    [JsonConverter(typeof(DecimalStringConverter))]
    public decimal Price { get; init; }

    public OrderPlacedEvent() { }

    public OrderPlacedEvent(Guid orderId, Guid userId, Guid gameId, decimal price)
    {
        OrderId = orderId;
        UserId = userId;
        GameId = gameId;
        Price = price;
    }
}

// Converter para aceitar price como string ou número
public class DecimalStringConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (decimal.TryParse(stringValue, out var decimalValue))
            {
                return decimalValue;
            }
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetDecimal();
        }
        
        throw new JsonException($"Unable to convert {reader.TokenType} to decimal");
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}
