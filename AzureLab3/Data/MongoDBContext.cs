using AzureLab3.Models;
using MongoDB.Driver;

namespace AzureLab3.Data;

public class MongoDBContext
{
    public IMongoCollection<RecipeModel> CookBook { get; set; }

    public MongoDBContext(string connectionString, string databaseName, string collectionName)
    {
        IMongoDatabase database = new MongoClient(connectionString).GetDatabase(databaseName);
        CookBook = database.GetCollection<RecipeModel>(collectionName, new() {AssignIdOnInsert = true});
    }
}