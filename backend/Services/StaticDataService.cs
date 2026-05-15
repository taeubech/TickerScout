using MongoDB.Driver;
using System.Text.RegularExpressions;
using TickerScout.Backend.Models;

namespace TickerScout.Backend.Services;

public sealed class StaticDataService(IConfiguration configuration) : IStaticDataService
{
    private const string DatabaseName = "TickerScout";
    private const string CollectionName = "Instruments";

    public IEnumerable<Instrument> GetAllInstruments()
    {
        string connectionString = ResolveEnvironmentVariables(configuration.GetConnectionString("MongoDb") ?? throw new InvalidOperationException("MongoDB connection string 'MongoDb' is not configured."));

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

    static string ResolveEnvironmentVariables(string input, int maxDepth = 10)
    {
        if (string.IsNullOrEmpty(input) || maxDepth <= 0)
            return input;

        var previousValue = input;
        var resolvedValue = Regex.Replace(
            input,
            @"\$\{\{([^}]+)\}\}",
            match =>
            {
                var envVarName = match.Groups[1].Value;
                return Environment.GetEnvironmentVariable(envVarName) ?? match.Value;
            });

        if (resolvedValue != previousValue && Regex.IsMatch(resolvedValue, @"\$\{\{([^}]+)\}\}"))
        {
            return ResolveEnvironmentVariables(resolvedValue, maxDepth - 1);
        }

        return resolvedValue;
    }

}
