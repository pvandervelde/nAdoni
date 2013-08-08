//-----------------------------------------------------------------------
// <copyright company="NAdoni">
//     Copyright 2013 NAdoni. Licensed under the Microsoft Public License (MS-PL).
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Autofac;
using NLog;
using Nuclei.Configuration;
using Nuclei.Diagnostics;
using Nuclei.Diagnostics.Logging;
using Nuclei.Diagnostics.Profiling;
using Nuclei.Diagnostics.Profiling.Reporting;

namespace NAdoni.ManifestBuilder
{
    /// <summary>
    /// Handles the component registrations for the Nuclei part.
    /// </summary>
    internal sealed class NucleiModule : Autofac.Module
    {
        /// <summary>
        /// The default name for the error log.
        /// </summary>
        private const string DefaultInfoFileName = "console.info.log";

        /// <summary>
        /// The default name for the profiler log.
        /// </summary>
        private const string DefaultProfilerFileName = "console.profile";

        private static void RegisterDiagnostics(ContainerBuilder builder)
        {
            builder.Register(
                c =>
                {
                    var loggers = c.Resolve<IEnumerable<ILogger>>();
                    Action<LevelToLog, string> action = (p, s) =>
                    {
                        var msg = new LogMessage(p, s);
                        foreach (var logger in loggers)
                        {
                            try
                            {
                                logger.Log(msg);
                            }
                            catch (NLogRuntimeException)
                            {
                                // Ignore it and move on to the next logger.
                            }
                        }
                    };

                    Profiler profiler = null;
                    if (c.IsRegistered<Profiler>())
                    {
                        profiler = c.Resolve<Profiler>();
                    }

                    return new SystemDiagnostics(action, profiler);
                })
                .As<SystemDiagnostics>()
                .SingleInstance();
        }

        private static void RegisterLoggers(ContainerBuilder builder)
        {
            builder.Register(
                c => LoggerBuilder.ForFile(
                    Path.Combine(c.Resolve<FileConstants>().LogPath(), DefaultInfoFileName),
                    new DebugLogTemplate(
                        c.Resolve<IConfiguration>(),
                        () => DateTimeOffset.Now)))
                .As<ILogger>()
                .SingleInstance();

            builder.Register(
                c => LoggerBuilder.ForEventLog(
                    Assembly.GetExecutingAssembly().GetName().Name,
                    new DebugLogTemplate(
                        c.Resolve<IConfiguration>(),
                        () => DateTimeOffset.Now)))
                .As<ILogger>()
                .SingleInstance();
        }

        private static void RegisterProfiler(ContainerBuilder builder)
        {
            if (ConfigurationHelpers.ShouldBeProfiling())
            {
                builder.Register((c, p) => new TextReporter(p.TypedAs<Func<Stream>>()))
                    .As<TextReporter>()
                    .As<ITransformReports>();

                builder.Register(c => new TimingStorage())
                    .OnRelease(
                        storage =>
                        {
                            // Write all the profiling results out to disk. Do this the ugly way 
                            // because we don't know if any of the other items in the container have
                            // been removed yet.
                            Func<Stream> factory =
                                () => new FileStream(
                                    Path.Combine(new FileConstants(new ApplicationConstants()).LogPath(), DefaultProfilerFileName),
                                    FileMode.Append,
                                    FileAccess.Write,
                                    FileShare.Read);
                            var reporter = new TextReporter(factory);
                            reporter.Transform(storage.FromStartTillEnd());
                        })
                    .As<IStoreIntervals>()
                    .As<IGenerateTimingReports>()
                    .SingleInstance();

                builder.Register(
                    c => new Profiler(
                        c.Resolve<IStoreIntervals>()))
                    .SingleInstance();
            }
        }

        /// <summary>
        /// Override to add registrations to the container.
        /// </summary>
        /// <param name="builder">The builder through which components can be registered.</param>
        /// <remarks>
        /// Note that the ContainerBuilder parameter is not the same one
        /// that the module is being registered by (i.e. it can have its own defaults).
        /// </remarks>
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            // Register the global application objects
            {
                builder.Register(c => new ApplicationConstants())
                    .As<ApplicationConstants>();

                builder.Register(c => new FileConstants(c.Resolve<ApplicationConstants>()))
                    .As<FileConstants>();

                builder.Register(c => new XmlConfiguration(
                        DiagnosticsConfigurationKeys.ToCollection(),
                        ManifestBuilderConstants.ConfigurationSectionApplicationSettings))
                    .As<IConfiguration>()
                    .SingleInstance();

                RegisterLoggers(builder);
                RegisterProfiler(builder);
                RegisterDiagnostics(builder);
            }
        }
    }
}
