using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service.Tools;

namespace MyJetWallet.Sdk.Postgres;

public class SqlLiveChecker<T> where T : DbContext
{
    private readonly ILogger<SqlLiveChecker<T>> _logger;
    private readonly MyTaskTimer _timer;
    private readonly DbContextOptionsBuilder<T> _dbContextOptionsBuilder;
    private static string _tableName;
    public SqlLiveChecker(ILogger<SqlLiveChecker<T>> logger, DbContextOptionsBuilder<T> dbContextOptionsBuilder, string migrationTableName)
    {
        _logger = logger;
        _dbContextOptionsBuilder = dbContextOptionsBuilder;
        _timer = new MyTaskTimer(typeof(SqlLiveChecker<T>), TimeSpan.FromSeconds(10), logger, DoTime);
        _tableName = migrationTableName;
        
        _timer.Start();
    }

    private async Task DoTime()
    {
        try
        {
            await using var context = new MyDbContext(_dbContextOptionsBuilder.Options);
            await context.Database.ExecuteSqlAsync($"SELECT * from {_tableName} LIMIT 1");
            MyDbContext.IsAlive = true;
        }
        catch (Exception e)
        {
            _logger.LogInformation(e, "Connection to database is lost");
            MyDbContext.IsAlive = false;
        } 
    }
}