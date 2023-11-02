using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MyJetWallet.Sdk.Postgres
{
    public class MyDbContext : DbContext
    {
        public static bool IsAlive = true;
        
        public static object Sync = new();
        public static Dictionary<string, (DateTime, string)> ContextList = new();
        
        public string ContextId { get; set; }
        public string ContextName { get; set; }
        
        public static ILoggerFactory LoggerFactory { get; set; }

        public MyDbContext(DbContextOptions options) : base(options)
        {
            ContextName = string.Empty;
            ContextId = Guid.NewGuid().ToString("N");
            lock (Sync) ContextList[ContextId] = (DateTime.UtcNow, ContextName);
        }
        
        public MyDbContext(DbContextOptions options, string contextName) : base(options)
        {
            ContextName = contextName;
            ContextId = Guid.NewGuid().ToString("N");
            lock (Sync) ContextList[ContextId] = (DateTime.UtcNow, ContextName);
        }

        protected MyDbContext()
        {
            ContextName = string.Empty;
            ContextId = Guid.NewGuid().ToString("N");
            lock (Sync) ContextList[ContextId] = (DateTime.UtcNow, ContextName);
        }

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

        public override void Dispose()
        {
            ContextList.Remove(ContextId);
            base.Dispose();
        }

        public override ValueTask DisposeAsync()
        {
            lock (Sync) ContextList.Remove(ContextId);
            lock (Sync) return base.DisposeAsync();
        }
    }
}