using SQLite.CodeFirst;
using System.Data.Entity;
using System.Data.SQLite;

/**
 * Created by Moon on 5/18/2019
 * The base database context for the EF database
 */

namespace EventServer.Discord.Database
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(string location) :
            base(new SQLiteConnection()
            {
                ConnectionString = new SQLiteConnectionStringBuilder() { DataSource = location }.ConnectionString
            }, true) { }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            var sqliteConnectionInitializer = new SqliteCreateDatabaseIfNotExists<DatabaseContext>(modelBuilder);
            System.Data.Entity.Database.SetInitializer(sqliteConnectionInitializer);
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<ReactionRole> ReactionRoles { get; set; }
    }
}
