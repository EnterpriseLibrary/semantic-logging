// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.

using System.IO;
using EnterpriseLibrary.SemanticLogging.Formatters;

namespace EnterpriseLibrary.SemanticLogging.Tests.TestObjects
{
    internal class SimpleMessageFormatter : IEventTextFormatter
    {
        public void WriteEvent(EventEntry eventEntry, TextWriter writer)
        {
            writer.Write(eventEntry.FormattedMessage);
        }
    }
}
