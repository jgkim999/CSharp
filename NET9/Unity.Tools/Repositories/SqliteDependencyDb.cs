using SQLite;

namespace Unity.Tools.Repositories;

public class SqliteDependencyDb : IDependencyDb
{
    public SqliteDependencyDb(string dbPath)
    {
        var databasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MyData.db");

        var db = new SQLiteConnection(databasePath);
        db.CreateTable<Stock>();
        db.CreateTable<Valuation>();
    }
}
