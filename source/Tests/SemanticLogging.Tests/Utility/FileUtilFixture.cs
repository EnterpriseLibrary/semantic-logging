// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.IO;
using EnterpriseLibrary.SemanticLogging.Tests.TestSupport;
using EnterpriseLibrary.SemanticLogging.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EnterpriseLibrary.SemanticLogging.Tests.Utility
{
    [TestClass]
    public class FileUtilFixture
    {
        [TestMethod]
        public void ThrowOnInvalidFileChars()
        {
            foreach (var c in Path.GetInvalidFileNameChars().Where(ch => ch != ':'))
            {
                AssertEx.Throws<ArgumentException>(() => FileUtil.ValidFile(c.ToString()));
            }

            foreach (var c in Path.GetInvalidPathChars().Where(ch => ch != ':'))
            {
                AssertEx.Throws<ArgumentException>(() => FileUtil.ValidFile(c.ToString()));
            }
        }

        [TestMethod]
        public void ThrowOnInvalidOSFileNames()
        {
            AssertEx.Throws<ArgumentException>(() => FileUtil.ValidFile("PRN.log"));
            AssertEx.Throws<ArgumentException>(() => FileUtil.ValidFile("AUX.log"));
            AssertEx.Throws<ArgumentException>(() => FileUtil.ValidFile("CON.log"));
        }

        [TestMethod]
        public void ThrowOnPathNavigationFileName()
        {
            AssertEx.Throws<ArgumentException>(() => FileUtil.ValidFile("."));
            AssertEx.Throws<ArgumentException>(() => FileUtil.ValidFile(@"..\"));
            AssertEx.Throws<ArgumentException>(() => FileUtil.ValidFile(@"..\..\.."));
            AssertEx.Throws<ArgumentException>(() => FileUtil.ValidFile(@"C:\Test\..\"));
        }
    }
}
