using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TickerScout.Backend.Models;

public sealed class Instrument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; init; }

    public required string Currency { get; init; }

    public InstrumentType InstrumentType { get; init; }

    public required string Exchange { get; init; }

    public required string Country { get; init; }
}
