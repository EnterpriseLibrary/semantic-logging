﻿// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Xml.Linq;
using EnterpriseLibrary.SemanticLogging.Observable;
using EnterpriseLibrary.SemanticLogging.Utility;

namespace EnterpriseLibrary.SemanticLogging.Configuration
{
    internal class WindowsAzureTableSinkElement : ISinkElement
    {
        private readonly XName sinkName = XName.Get("windowsAzureTableSink", Constants.Namespace);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Validated with Guard class")]
        public bool CanCreateSink(XElement element)
        {
            Guard.ArgumentNotNull(element, "element");

            return element.Name == this.sinkName;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Validated with Guard class")]
        public IObserver<EventEntry> CreateSink(XElement element)
        {
            Guard.ArgumentNotNull(element, "element");

            var subject = new EventEntrySubject();
            subject.LogToWindowsAzureTable(
                (string)element.Attribute("instanceName"),
                (string)element.Attribute("connectionString"),
                (string)element.Attribute("tableAddress") ?? WindowsAzureTableLog.DefaultTableName,
                element.Attribute("bufferingIntervalInSeconds").ToTimeSpan(),
                (bool?)element.Attribute("sortKeysAscending") ?? false,
                element.Attribute("bufferingFlushAllTimeoutInSeconds").ToTimeSpan() ?? Constants.DefaultBufferingFlushAllTimeout,
                (int?)element.Attribute("maxBufferSize") ?? Buffering.DefaultMaxBufferSize);

            return subject;
        }
    }
}
