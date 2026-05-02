using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Sinks.PostgreSQL;
using NpgsqlTypes;

namespace Mms.Infrastructure.Logging;

public static class SerilogConfiguration
{
    public static void Configure(IConfiguration config, string connectionString)
    {
        var columnOptions = new Dictionary<string, ColumnWriterBase>
        {
            { "message", new RenderedMessageColumnWriter() },
            { "message_template", new MessageTemplateColumnWriter() },
            { "level", new LevelColumnWriter(true, NpgsqlDbType.Varchar) },
            { "raise_date", new TimestampColumnWriter() },
            { "exception", new ExceptionColumnWriter() },
            { "properties", new LogEventSerializedColumnWriter() },
        };

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(config)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                "logs/mms-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 10)
            .WriteTo.PostgreSQL(
                connectionString,
                tableName: "logs",
                columnOptions: columnOptions,
                needAutoCreateTable: true)
            .CreateLogger();
    }
}
