using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service.Tools;

namespace MyJetWallet.Sdk.Postgres;

public static class SqlLiveCheckerSettings
{
    public static int DelayToReport = 5;
    public static bool IsReportLongDelay = true;
}

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
            var sql = $@"SELECT * from ""{DataBaseHelper.MigrationTableSchema}"".""{DataBaseHelper.MigrationTableName}"" LIMIT 1;";
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

        try
        {
            CheckLongContextUsage();
        }
        catch (Exception e)
        {
            _logger.LogInformation(e, "Cannot check long context usage");
            MyDbContext.IsAlive = false;
        }
    }

    private void CheckLongContextUsage()
    {
        if (!SqlLiveCheckerSettings.IsReportLongDelay)
            return;
        
        var report = "";
        lock (MyDbContext.Sync)
        {
            report = MyDbContext
                .ContextList
                .Where(e => (DateTime.UtcNow - e.Value.Item1).TotalSeconds > SqlLiveCheckerSettings.DelayToReport)
                .Aggregate("",
                    (s, e) => $"{s}; '{e.Value.Item2}'::{(DateTime.UtcNow - e.Value.Item1).TotalSeconds} seconds");
        }
        
        if (!string.IsNullOrEmpty(report))
            _logger.LogError("Detect long DB usage: {jsonText}", report);
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