using System;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service.Tools;

namespace MyJetWallet.Sdk.Postgres;

public class SqlLiveChecker<T> : IHostedService where T : DbContext
{
    private readonly ILogger<SqlLiveChecker<T>> _logger;
    private readonly MyTaskTimer _timer;
    private readonly DbContextOptionsBuilder<T> _dbContextOptionsBuilder;

    public SqlLiveChecker(ILogger<SqlLiveChecker<T>> logger, DbContextOptionsBuilder<T> dbContextOptionsBuilder)
    {
        _logger = logger;
        _dbContextOptionsBuilder = dbContextOptionsBuilder;
        _timer = new MyTaskTimer(typeof(SqlLiveChecker<T>), TimeSpan.FromSeconds(10), logger, DoTime);

        _logger.LogInformation("Database liveness checker is started");
    }

    private async Task DoTime()
    {
        try
        {
            await using var context = (T)Activator.CreateInstance(typeof(T), _dbContextOptionsBuilder.Options);
            var sql = $@"SELECT * from {DataBaseHelper.MigrationTableSchema}.""{DataBaseHelper.MigrationTableName}"" LIMIT 1;";
            await context.Database.GetDbConnection().QueryAsync($"{sql}");
            if(MyDbContext.IsAlive == false)
                _logger.LogInformation("Connection to database is restored");
            
            MyDbContext.IsAlive = true;
        }
        catch (Exception e)
        {
            _logger.LogInformation(e, "Connection to database is lost");
            MyDbContext.IsAlive = false;
        } 
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _timer.Start();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Dispose();
    }
}