using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MyJetWallet.Sdk.Postgres
{
    public static class DataBaseHelper
    {
        public static void AddDatabase<T>(this IServiceCollection services, string schema, string connectionString, Func<DbContextOptions, T> contextFactory)
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

            var contextOptions = services.BuildServiceProvider().GetRequiredService<DbContextOptionsBuilder<T>>();

            using var context = contextFactory(contextOptions.Options);
            context.Database.Migrate();
        }
    }
}