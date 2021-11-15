using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MyJetWallet.Sdk.Postgres
{
    public class MyDbContext : DbContext
    {
        public static ILoggerFactory LoggerFactory { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (LoggerFactory != null)
            {
                optionsBuilder.UseLoggerFactory(LoggerFactory).EnableSensitiveDataLogging();
            }
        }
        
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<DateTime>().HaveConversion<MyDateTimeConverterToUtc>();
        }

    }
}