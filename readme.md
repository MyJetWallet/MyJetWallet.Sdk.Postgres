![Release API client nuget](https://github.com/MyJetWallet/MyJetWallet.Sdk.Postgres/workflows/Release%20API%20client%20nuget/badge.svg)

![Nuget version](https://img.shields.io/nuget/v/MyJetWallet.Sdk.Postgres?label=MyJetWallet.Sdk.Postgres&style=social)

# Using

install to the project nuget library:

```
dotnet add package Microsoft.EntityFrameworkCore

dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL

dotnet add package Microsoft.EntityFrameworkCore.Design

dotnet add package MyJetWallet.Sdk.Postgres
```

Create DbContext. **NEED INHERITE `MyDbContext`!**

```
    public class MyDbEntity
    {
        public int Id { get; set; }

        public string Text { get; set; }
    }

    public class MyContext: MyDbContext
    {
        public const string Schema = "myschema";
        public const string MyDbTableName = "mydbtable"

        public DbSet<MyDbEntity> MyDbEntity { get; set; }

        public MyContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(Schema);

            modelBuilder.Entity<MyDbEntity>().ToTable(MyDbTableName);
            modelBuilder.Entity<MyDbEntity>().Property(e => e.Id).UseIdentityColumn();
            modelBuilder.Entity<MyDbEntity>().HasKey(e => e.Id);
            modelBuilder.Entity<MyDbEntity>().Property(e => e.Text).HasMaxLength(1024);
            
            base.OnModelCreating(modelBuilder);
        }
    }
```

Setup class to DesignTime connect to the database. The class will auto-used for the creating migration.

```
    public class ContextFactory : MyDesignTimeContextFactory<MyContext>
    {
        public ContextFactory() : base(options => new MyContext(options))
        {
        }
    }
```

Register database into the services

```
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            MyContext.LoggerFactory = Program.LoggerFactory;
            services.AddDatabase(MyContext.Schema, ConnectionString, o => new MyContext(o));            
        }
    }
```

Using cotext into the code

```
    [ApiController]
    [Route("[controller]")]
    public class MyTestController : ControllerBase
    {
        private readonly DbContextOptionsBuilder<MyContext> _dbContextOptionsBuilder;

        public WeatherForecastController(DbContextOptionsBuilder<MyContext> dbContextOptionsBuilder)
        {
            _dbContextOptionsBuilder = dbContextOptionsBuilder;
        }

        [HttpGet("data")]
        public IEnumerable<MyTable> GetData()
        {
            using var ctx = new MyContext(_dbContextOptionsBuilder.Options);
            var data = ctx.MyTable.ToList();
            return data;
        }
    }
```


Create migration

```
dotnet ef migrations add Migration_Name

Build started...
Build succeeded.
Connection string: <Print connection string to the databse where need to create migration>
```

after migration created, migration will stored in the folder `Migrations`

After the start, the application will check the database. And if migration missed then automatically execute migration in the database.


More detail you can read here: https://docs.microsoft.com/en-us/ef/core/cli/dotnet
