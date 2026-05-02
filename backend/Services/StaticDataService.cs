using MongoDB.Driver;
using TickerScout.Backend.Models;

namespace TickerScout.Backend.Services;

public sealed class StaticDataService(IConfiguration configuration) : IStaticDataService
{
    private const string DatabaseName = "tickerscout";
    private const string CollectionName = "instruments";

    public IEnumerable<Instrument> GetAllInstruments()
    {
        string connectionString = configuration.GetConnectionString("MongoDb")
            ?? throw new InvalidOperationException("MongoDB connection string 'MongoDb' is not configured.");

        MongoClient client = new(connectionString);
        try
        {
            IMongoDatabase database = client.GetDatabase(DatabaseName);
            IMongoCollection<Instrument> collection = database.GetCollection<Instrument>(CollectionName);
            return collection.Find(FilterDefinition<Instrument>.Empty).ToList();
        }
        finally
        {
            client.Dispose();
        }
    }
}
