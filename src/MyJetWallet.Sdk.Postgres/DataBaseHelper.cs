using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;
using MyJetWallet.Sdk.Service;
using Npgsql;

// ReSharper disable UnusedMember.Global

namespace MyJetWallet.Sdk.Postgres
{
    public static class DataBaseHelper
    {
        public static string MigrationTableName = "__EFMigrationsHistory";
        public static string MigrationTableSchema = "";
        
        public static void AddDatabase<T>(this IServiceCollection services, string schema, string connectionString, Func<DbContextOptions, T> contextFactory,
            bool replaceSllInstruction = true)
            where T : DbContext
        {
            AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
            
            NpgsqlConnection.GlobalTypeMapper
                .EnableDynamicJson()
                .EnableUnmappedTypes()
                .EnableRecordsAsTuples()
                .UseJsonNet();

            connectionString = PrepareConnectionString(connectionString, replaceSllInstruction);

            MigrationTableName = $"__EFMigrationsHistory_{schema}";
            MigrationTableSchema = schema;
            
            services.AddSingleton<DbContextOptionsBuilder<T>>(x =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<T>();
                optionsBuilder.UseNpgsql(connectionString,
                    builder =>
                    {
                        builder.MigrationsHistoryTable(MigrationTableName, schema);
                        builder.EnableRetryOnFailure(2, TimeSpan.FromSeconds(5), null);
                    });
                return optionsBuilder;
            });
            
            services.AddHostedService<SqlLiveChecker<T>>();

            var contextOptions = services.BuildServiceProvider().GetRequiredService<DbContextOptionsBuilder<T>>();

            using var activity = MyTelemetry.StartActivity("database migration");
            {
                Console.WriteLine("======= begin database migration =======");
                var sw = new Stopwatch();
                sw.Start();
                
                using var context = contextFactory(contextOptions.Options);

                context.Database.Migrate();

                sw.Stop();
                Console.WriteLine($"======= end database migration ({sw.Elapsed.ToString()}) =======");
            }
        }

        private static string PrepareConnectionString(string connectionString, bool replaceSllInstruction)
        {
            if (!connectionString.Contains("ApplicationName"))
            {
                var appName = Environment.GetEnvironmentVariable("ENV_INFO");
                if (appName == null)
                {
                    appName = Assembly.GetEntryAssembly()?.GetName().Name;
                }

                connectionString = connectionString.Last() != ';'
                    ? $"{connectionString};ApplicationName={appName}"
                    : $"{connectionString}ApplicationName={appName}";
            }

            if (replaceSllInstruction)
                connectionString = connectionString.Replace("Ssl Mode=Require", "Ssl Mode=VerifyFull");
            
            return connectionString;
        }

        public static void AddDatabaseWithoutMigrations<T>(this IServiceCollection services, string schema, string connectionString,
            bool replaceSllInstruction = true)
            where T : DbContext
        {
            AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);

            connectionString = PrepareConnectionString(connectionString, replaceSllInstruction);
            MigrationTableName = $"__EFMigrationsHistory_{schema}";
            MigrationTableSchema = schema;

            services.AddSingleton<DbContextOptionsBuilder<T>>(x =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<T>();
                optionsBuilder.UseNpgsql(connectionString,
                    builder =>
                        builder.MigrationsHistoryTable(
                            MigrationTableName,
                            schema));

                return optionsBuilder;
            });

            services.AddHostedService<SqlLiveChecker<T>>();
        }

        public static PropertyBuilder<DateTime> SpecifyKindUtc<TEntity>(
            this EntityTypeBuilder<TEntity> builder,
            Expression<Func<TEntity, DateTime>> propertyExpression)
            where TEntity : class
        {
            var res = builder
                .Property(propertyExpression)
                .HasConversion(
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc),
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            return res;

        }
    }
}