﻿using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Elmo.Logging;
using Elmo.Responses;
using Microsoft.Owin;

namespace Elmo.Views
{
    internal abstract class ErrorViewBase : IRequestHandler
    {
        protected IOwinContext OwinContext { get; private set; }
        protected string PageTitle { get; set; }
        protected IErrorLog ErrorLog { get; private set; }
        protected string BasePageName => OwinContext.Request.Path.Value.TrimEnd('/');
        protected string ApplicationName => ErrorLog.ApplicationName;
        
        private async Task RenderDocumentStartAsync(XmlWriter writer)
        {
            await writer.WriteDocTypeAsync("html", null, null, null); // doctype

            await writer.WriteStartElementAsync(null, "html", "http://www.w3.org/1999/xhtml"); // html start

            await writer.WriteStartElementAsync(null, "head", null); // head start
            await RenderHeadAsync(writer);
            await writer.WriteEndElementAsync(); // head end

            await writer.WriteStartElementAsync(null, "body", null); // body start
        }

        private async Task RenderHeadAsync(XmlWriter writer)
        {
            await writer.WriteStartElementAsync(null, "meta", null);
            await writer.WriteAttributeStringAsync(null, "http-equiv", null, "X-UA-Compatible");
            await writer.WriteAttributeStringAsync(null, "content", null, "IE=EmulateIE7");
            await writer.WriteEndElementAsync();

            await writer.WriteElementStringAsync(null, "title", null, PageTitle);

            await writer.WriteStartElementAsync(null, "link", null);
            await writer.WriteAttributeStringAsync(null, "rel", null, "stylesheet");
            await writer.WriteAttributeStringAsync(null, "type", null, "text/css");
            await writer.WriteAttributeStringAsync(null, "href", null, BasePageName + "/stylesheet");
            await writer.WriteEndElementAsync();
        }
        
        private async Task RenderDocumentEndAsync(XmlWriter writer)
        {
            //
            // Write the powered-by signature, that includes version information.
            //

            await writer.WriteStartElementAsync(null, "p", null); // <p>
            await writer.WriteAttributeStringAsync(null, "id", null, "Footer");


            //PoweredBy poweredBy = new PoweredBy();
            //poweredBy.RenderControl(writer);

            //
            // Write out server date, time and time zone details.
            //

            var now = DateTime.Now;

            await writer.WriteStringAsync("Server date is ");
            await writer.WriteStringAsync(now.ToString("D", CultureInfo.InvariantCulture));

            await writer.WriteStringAsync(". Server time is ");
            await writer.WriteStringAsync(now.ToString("T", CultureInfo.InvariantCulture));

            await writer.WriteStringAsync(". All dates and times displayed are in the ");
            await writer.WriteStringAsync(TimeZone.CurrentTimeZone.IsDaylightSavingTime(now) ?
                TimeZone.CurrentTimeZone.DaylightName : TimeZone.CurrentTimeZone.StandardName);
            await writer.WriteStringAsync(" zone. ");

            //
            // Write out the source of the log.
            //

            await writer.WriteStringAsync("This log is provided by the ");
            await writer.WriteStringAsync(ErrorLog.Name);
            await writer.WriteStringAsync(".");

            await writer.WriteEndElementAsync();// </p>

            await writer.WriteEndElementAsync(); // </body>
            await writer.WriteEndElementAsync(); // </html>
        }

        private async Task RenderAsync()
        {
            var settings = new XmlWriterSettings
            {
                Async = true,
                Indent = true,
                IndentChars = "  ",
                Encoding = Encoding.UTF8,
                OmitXmlDeclaration = true
            };

            await LoadContentsAsync();

            var response = OwinContext.Response;
            response.ContentType = "text/html";
            response.StatusCode = 200;
            response.ReasonPhrase = "Ok";

            using (var writer = XmlWriter.Create(response.Body, settings))
            {
                await RenderDocumentStartAsync(writer);
                await RenderContentsAsync(writer);
                await RenderDocumentEndAsync(writer);
            }
        }

        protected abstract Task RenderContentsAsync(XmlWriter writer);

        protected abstract Task LoadContentsAsync();

        public async Task ProcessRequestAsync(IOwinContext owinContext, IErrorLog errorLog)
        {
            OwinContext = owinContext;
            ErrorLog = errorLog;
            await RenderAsync();
            Processed = true;
        }

        public abstract bool CanProcess(string path);

        public bool Processed { get; private set; }
    }
}
