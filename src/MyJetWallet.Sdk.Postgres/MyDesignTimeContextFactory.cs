using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

// ReSharper disable UnusedMember.Global

namespace MyJetWallet.Sdk.Postgres
{
    public class MyDesignTimeContextFactory<T> : IDesignTimeDbContextFactory<T> where T : MyDbContext
    {
        private readonly Func<DbContextOptions, T> _contextFactory;

        public MyDesignTimeContextFactory(Func<DbContextOptions, T> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public T CreateDbContext(string[] args)
        {
            var connString = string.Empty;

            while (string.IsNullOrEmpty(connString))
            {
                Console.Write("Connection string: ");
                connString = Console.ReadLine();
            }

            var optionsBuilder = new DbContextOptionsBuilder<T>();
            optionsBuilder.UseNpgsql(connString);

            return _contextFactory(optionsBuilder.Options);
        }
    }
}
