using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.TextManager.Interop;
using ReplaceTypeKeywords.Core;

namespace ReplaceTypeKeywords.VSE
{
    internal class DocumentFormatter
    {
        private readonly IVsTextManager txtMngr;
        private readonly DTE dte;

        public DocumentFormatter(IVsTextManager txtMngr, DTE dte)
        {
            this.txtMngr = txtMngr;
            this.dte = dte;
        }

        public void FormatCurrentActiveDocument()
        {
            try
            {
                if (dte.ActiveWindow.Kind == "Document")
                {
                    FormatDocument(dte.ActiveDocument);
                }
            }
            catch (Exception) { }
        }

        public void FormatDocuments(IEnumerable<Document> documents)
        {
            foreach (var document in documents)
            {
                FormatDocument(document);
            }
        }

        public void FormatDocument(Document document)
        {
            try
            {
                var textDocument = document?.Object("TextDocument") as TextDocument;
                if (textDocument == null)
                {
                    return;
                }

                if (textDocument.Language != "CSharp")
                {
                    return;
                }

                var source = textDocument
                   .CreateEditPoint(textDocument.StartPoint)
                   .GetText(textDocument.EndPoint);

                source = TypeKeywordsReplacer.Process(source);

                textDocument
                    .CreateEditPoint(textDocument.StartPoint)
                    .ReplaceText(
                        textDocument.EndPoint,
                        source,
                        (int)vsEPReplaceTextOptions.vsEPReplaceTextKeepMarkers
                    );
            }
            catch (Exception) { }
        }

        public void FormatNonSavedDocuments()
        {
            FormatDocuments(GetNonSavedDocuments());
        }

        IEnumerable<Document> GetNonSavedDocuments()
        {
            return dte.Documents.OfType<Document>().Where(document => !document.Saved);
        }
    }
}