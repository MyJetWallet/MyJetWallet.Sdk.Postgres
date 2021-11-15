using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MyJetWallet.Sdk.Postgres
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class MyDateTimeConverterToUtc: ValueConverter<DateTime, DateTime>
    {
        public MyDateTimeConverterToUtc()
            : base(
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
        { }
    }
}