using System;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
// ReSharper disable UnusedMember.Global

namespace MyJetWallet.Sdk.Postgres
{
    public static class DataBaseHelper
    {
        public static void AddDatabase<T>(this IServiceCollection services, string schema, string connectionString, Func<DbContextOptions, T> contextFactory)
            where T : DbContext
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

            services.AddSingleton<DbContextOptionsBuilder<T>>(x =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<T>();
                optionsBuilder.UseNpgsql(connectionString,
                    builder =>
                        builder.MigrationsHistoryTable(
                            $"__EFMigrationsHistory_{schema}",
                            schema));

                return optionsBuilder;
            });

            var contextOptions = services.BuildServiceProvider().GetRequiredService<DbContextOptionsBuilder<T>>();

            using var context = contextFactory(contextOptions.Options);
            context.Database.Migrate();
        }

        public static void AddDatabaseWithoutMigrations<T>(this IServiceCollection services, string schema, string connectionString)
            where T : DbContext
        {
            services.AddSingleton<DbContextOptionsBuilder<T>>(x =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<T>();
                optionsBuilder.UseNpgsql(connectionString,
                    builder =>
                        builder.MigrationsHistoryTable(
                            $"__EFMigrationsHistory_{schema}",
                            schema));

                return optionsBuilder;
            });
        }
    }
}