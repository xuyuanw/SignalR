// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class VerifiableLoggedTest : LoggedTest
    {
        public VerifiableLoggedTest(ITestOutputHelper output) : base(output)
        {
        }

        public virtual IDisposable StartVerifiableLog(out ILoggerFactory loggerFactory, [CallerMemberName] string testName = null, Func<WriteContext, bool> expectedErrorsFilter = null)
        {
            var disposable = StartLog(out loggerFactory, testName);

            return CreateScope(ref loggerFactory, disposable, expectedErrorsFilter);
        }

        public virtual IDisposable StartVerifiableLog(out ILoggerFactory loggerFactory, LogLevel minLogLevel, [CallerMemberName] string testName = null, Func<WriteContext, bool> expectedErrorsFilter = null)
        {
            var disposable = StartLog(out loggerFactory, minLogLevel, testName);

            return CreateScope(ref loggerFactory, disposable, expectedErrorsFilter);
        }

        private VerifyNoErrorsScope CreateScope(ref ILoggerFactory loggerFactory, IDisposable wrappedDisposable = null, Func<WriteContext, bool> expectedErrorsFilter = null)
        {
            loggerFactory = new WrappingLoggerFactory(loggerFactory ?? new LoggerFactory());
            return new VerifyNoErrorsScope(loggerFactory, wrappedDisposable, expectedErrorsFilter);
        }
    }
}
