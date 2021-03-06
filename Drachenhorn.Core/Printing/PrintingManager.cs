﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Drachenhorn.Core.IO;
using Drachenhorn.Core.ViewModels.Sheet;
using Drachenhorn.Xml.Sheet;
using GalaSoft.MvvmLight.Ioc;
using iText.Html2pdf;
using RazorLight;
using RazorLight.Caching;

namespace Drachenhorn.Core.Printing
{
    public static class PrintingManager
    {
        private static readonly string TemplatesDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Drachenhorn",
                "PrintTemplates");

        public static IEnumerable<FileInfo> GetPrintTemplates()
        {
            if (!Directory.Exists(TemplatesDirectory)) Directory.CreateDirectory(TemplatesDirectory);

            return Directory.GetFiles(TemplatesDirectory).Where(x => x.EndsWith(".cshtml"))
                .Select(x => new FileInfo(x));
        }


        #region Generate HTML

        public static string GenerateHtml(CharacterSheet sheet)
        {
            var template = "";

            var assembly = Assembly.GetExecutingAssembly();

            using (var stream =
                assembly.GetManifestResourceStream("Drachenhorn.Core.Printing.Templates.CharacterSheet.cshtml"))
            using (var reader = new StreamReader(stream))
            {
                template = reader.ReadToEnd();
            }

            return GenerateHtml(sheet, template);
        }

        public static string GenerateHtml(CharacterSheet sheet, FileInfo file)
        {
            return GenerateHtml(sheet, file.OpenText().ReadToEnd());
        }

        private static string GenerateHtml(CharacterSheet sheet, string template, Assembly operatingAssembly = null)
        {
            try
            {
                var engine = new RazorLightEngineBuilder()
                    .SetOperatingAssembly(Assembly.GetCallingAssembly())
                    .UseCachingProvider(new MemoryCachingProvider())
                    .Build();

                var result = engine.CompileRenderAsync("CharacterSheet", template, new CharacterSheetViewModel(sheet))
                    .Result;

                return result;
            }
            catch (AggregateException e)
            {
                throw new InvalidOperationException("Unable to create PrintView.", e);
            }
        }

        public static void GeneratePDF(CharacterSheet sheet)
        {
            var html = GenerateHtml(sheet);

            SimpleIoc.Default.GetInstance<IIoService>().FileSaverDialog(
                sheet.Characteristics.Name,
                ".pdf", "PDF", "PDF-Export", x =>
                {
                    using (var fs = new FileStream(x, FileMode.Create))
                    {
                        HtmlConverter.ConvertToPdf(html, fs);
                    }
                });
        }

        public static Task GeneratePDFAsync(CharacterSheet sheet)
        {
            return Task.Run(() => GeneratePDF(sheet));
        }

        #endregion Generate HTML
    }
}