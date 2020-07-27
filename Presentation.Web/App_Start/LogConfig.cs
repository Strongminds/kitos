﻿using System;
using Serilog;
using Serilog.Exceptions.Core;
using Serilog.Formatting.Compact;

namespace Presentation.Web
{
    public static class LogConfig
    {
        private static readonly Lazy<ILogger> GlobalLoggerInstance = new Lazy<ILogger>(ConfigureAndCreateSerilogLogger);

        public static ILogger GlobalLogger => GlobalLoggerInstance.Value;

        private static ILogger ConfigureAndCreateSerilogLogger()
        {
            return new LoggerConfiguration()
                .ReadFrom.AppSettings()
                .Enrich.FromLogContext()
                .Enrich.With<ExceptionEnricher>()
                .WriteTo.File(new CompactJsonFormatter(),path: @"C:\Logs\Kitos-.txt", retainedFileCountLimit:10, rollingInterval:RollingInterval.Day)
                .CreateLogger();
        }

        public static void RegisterLog()
        {
            Log.Logger = GlobalLoggerInstance.Value;
        }
    }
}