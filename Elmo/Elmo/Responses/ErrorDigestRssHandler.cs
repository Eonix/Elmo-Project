﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Elmo.Logging;
using Elmo.Utilities;
using Microsoft.Owin;

namespace Elmo.Responses
{
    internal class ErrorDigestRssHandler
    {
        private readonly IOwinContext owinContext;
        private readonly IErrorLog errorLog;

        public ErrorDigestRssHandler(IOwinContext owinContext, IErrorLog errorLog)
        {
            this.owinContext = owinContext;
            this.errorLog = errorLog;
        }

        public async Task ProcessRequestAsync()
        {
            var syndicationFeed = new SyndicationFeed();

            var hostName = EnvironmentUtilities.GetMachineNameOrDefault("Unknown Host");
            syndicationFeed.Title = new TextSyndicationContent($"Daily digest of errors in {errorLog.ApplicationName} on {hostName}.");
            syndicationFeed.Description = new TextSyndicationContent("Daily digest of application errors");
            syndicationFeed.Language = "en";

            var baseUri = owinContext.Request.Uri;
            syndicationFeed.Links.Add(SyndicationLink.CreateAlternateLink(baseUri));

            var logEntries = await GetAllEntriesAsync();
            var groupBy = logEntries.GroupBy(
                entry => new DateTime(entry.Error.Time.Year, entry.Error.Time.Month, entry.Error.Time.Day));

            
            var itemList = new List<SyndicationItem>();
            foreach (var grouping in groupBy)
            {
                var syndicationItem = new SyndicationItem
                {
                    Title =
                        new TextSyndicationContent(
                            $"Digest for {grouping.Key.ToString("yyyy-MM-dd")} ({grouping.Key.ToLongDateString()})"),
                    PublishDate = grouping.Key,
                    Id = grouping.Key.ToString("yyyy-MM-dd")
                };

                var builder = new StringBuilder();

                builder.AppendLine("<ul>");
                foreach (var errorLogEntry in grouping)
                {
                    builder.AppendLine("<li>");
                    builder.AppendLine($"{errorLogEntry.Error.TypeName}: <a href=\"{baseUri}/detail?id={errorLogEntry.Id}\">{errorLogEntry.Error.Message}</a>");
                    builder.AppendLine("</li>");
                }
                builder.AppendLine("</ul>");

                syndicationItem.Content = SyndicationContent.CreateHtmlContent(builder.ToString());

                itemList.Add(syndicationItem);
            }

            syndicationFeed.Items = itemList;

            owinContext.Response.ContentType = "application/xml";
            owinContext.Response.StatusCode = 200;
            owinContext.Response.ReasonPhrase = "Ok";

            var xmlWriterSettings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                Encoding = Encoding.UTF8,
                OmitXmlDeclaration = true
            };

            using (var writer = XmlWriter.Create(owinContext.Response.Body, xmlWriterSettings))
            {
                var formatter = new Rss20FeedFormatter(syndicationFeed);
                formatter.WriteTo(writer);
            }
        }

        private async Task<IEnumerable<ErrorLogEntry>> GetAllEntriesAsync()
        {
            const int defaultPageSize = 30;
            const int maxPageLimit = 30;
            var totalErrorCount = await errorLog.GetTotalErrorCountAsync();

            var count = 0;
            var list = new List<ErrorLogEntry>();
            for (var pageIndex = 0; pageIndex < maxPageLimit && count < totalErrorCount; pageIndex++)
            {
                var entries = await errorLog.GetErrorsAsync(pageIndex, defaultPageSize);
                count += entries.Count;
                list.AddRange(entries);
            }

            return list;
        }
    }
}
