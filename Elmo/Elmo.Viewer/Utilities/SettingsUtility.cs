﻿using System.Text;
using System.Xml;

namespace Elmo.Viewer.Utilities
{
    internal static class SettingsUtility
    {
        public static XmlWriterSettings XmlWriterSettings => new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = true
        };
    }
}
