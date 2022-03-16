using DevExpress.Spreadsheet;
using DevExpress.Xpf.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace UCSReports
{
    class ExportHelper
    {

        public void ExportAlgorithm(IEnumerable<string> data, IEnumerable<ReportObject> steps, AlgorithmType typeOfAlgorithm, out string savedFileName)
        {
            string templateName = typeOfAlgorithm == AlgorithmType.Emergency ? @"Templates\EmergencyAlg.xlsx" : @"Templates\RegularAlg.xlsx";

            Workbook workbook = new Workbook();
            workbook.LoadDocument(templateName);
            Worksheet worksheet = workbook.Worksheets[0];
            workbook.Unit = DevExpress.Office.DocumentUnit.Point;
            workbook.BeginUpdate();

            // print opts
            worksheet.ActiveView.Orientation = PageOrientation.Landscape;
            worksheet.ActiveView.PaperKind = System.Drawing.Printing.PaperKind.A4;
            worksheet.PrintOptions.FitToWidth = 1;

            try
            {
                // Технологический участок
                worksheet.Columns["A"][0].Value = $"Технологический участок: {data.ToList()[0]}";
                // Название алгоритма
                worksheet.Columns["A"][1].Value = $"Название алгоритма: {data.ToList()[1]}";
                // Интервал времени
                worksheet.Columns["A"][2].Value = $"Интервал времени: {data.ToList()[2]}";
                // Статус алгоритма
                worksheet.Columns["A"][3].Value = $"Статус алгоритма: {data.ToList()[3]}";

                // steps
                int rowNumber = 8;
                if (typeOfAlgorithm == AlgorithmType.Emergency)
                {
                    foreach (EmergencyStep step in steps)
                    {
                        worksheet.Columns["A"][rowNumber].Value = $"{step.ID}. {step.Name}";
                        worksheet.Columns["B"][rowNumber].Value = "'" + step.StartTime.ToLongTimeString();
                        worksheet.Columns["C"][rowNumber].Value = "'" + step.EndTime.ToLongTimeString();
                        worksheet.Columns["D"][rowNumber].Value = step.ControlTime > TimeSpan.Zero ? $"'{step.ControlTime}" : "---";
                        worksheet.Columns["E"][rowNumber].Value = step.Status;

                        worksheet.Range[$"A{rowNumber + 1}:E{rowNumber + 1}"].FillColor = GetFillColor(step.CodeOfStatus);
                        worksheet.Range[$"A{rowNumber + 1}:E{rowNumber + 1}"].Borders.SetAllBorders(Color.Black, BorderLineStyle.Thin);

                        rowNumber++;
                    }
                }
                if (typeOfAlgorithm == AlgorithmType.Regular)
                {
                    foreach (RegularStep step in steps)
                    {
                        worksheet.Columns["A"][rowNumber].Value = $"{step.ID}. {step.Name}";
                        worksheet.Columns["B"][rowNumber].Value = "'" + step.StartTime.ToLongTimeString();
                        worksheet.Columns["C"][rowNumber].Value = "'" + step.EndTime.ToLongTimeString();
                        worksheet.Columns["D"][rowNumber].Value = step.ControlTime > TimeSpan.Zero ? $"'{step.ControlTime}" : "---";
                        worksheet.Columns["H"][rowNumber].Value = step.Status;

                        worksheet.Range[$"A{rowNumber + 1}:H{rowNumber + 1}"].FillColor = GetFillColor(step.CodeOfStatus);
                        worksheet.Range[$"A{rowNumber + 1}:H{rowNumber + 1}"].Borders.SetAllBorders(Color.Black, BorderLineStyle.Thin);

                        rowNumber++;

                        if (step.Acts.Count > 0)
                        {
                            foreach (Act act in step.Acts)
                            {
                                worksheet.Columns["A"][rowNumber].Value = $"  {step.ID}.{act.ID}. {act.Name}";
                                worksheet.Columns["B"][rowNumber].Value = "'" + act.StartTime.ToLongTimeString();
                                worksheet.Columns["C"][rowNumber].Value = "'" + act.EndTime.ToLongTimeString();
                                worksheet.Columns["D"][rowNumber].Value = step.ControlTime > TimeSpan.Zero ? $"'{act.ControlTime}" : "---";
                                worksheet.Columns["E"][rowNumber].Value = act.EquipmentStart;
                                worksheet.Columns["F"][rowNumber].Value = act.EquipmentEnd;
                                worksheet.Columns["G"][rowNumber].Value = act.EquipmentControl;
                                worksheet.Columns["H"][rowNumber].Value = act.Status;

                                worksheet.Range[$"A{rowNumber + 1}:H{rowNumber + 1}"].FillColor = GetFillColor(act.CodeOfStatus);
                                worksheet.Range[$"A{rowNumber + 1}:H{rowNumber + 1}"].Borders.SetAllBorders(Color.Black, BorderLineStyle.Thin);

                                rowNumber++;
                            }
                        }
                    }
                }

                // auto-fit
                worksheet.Columns.AutoFit(0, 7);
                // center aligment
                for (int j = 1; j < 7; j++)
                    worksheet.Columns[j].Alignment.Horizontal = SpreadsheetHorizontalAlignment.Center;

            }
            finally
            {
                workbook.EndUpdate();
            }

            var fileName = data.ToList()[4];
            savedFileName = SaveWorkbook(workbook, fileName);
        }

        //public void ExportProtections(IEnumerable<string> data, IEnumerable<Protection> protections, out string savedFileName)
        //{
        //    Workbook workbook = new Workbook();
        //    workbook.LoadDocument(@"Templates\Protections.xlsx");
            
        //    Worksheet worksheet = workbook.Worksheets[0];
        //    Worksheet groupedWorksheet = workbook.Worksheets[1];
        //    workbook.Unit = DevExpress.Office.DocumentUnit.Point;
        //    workbook.BeginUpdate();

        //    // print opts
        //    worksheet.ActiveView.Orientation = PageOrientation.Landscape;
        //    worksheet.ActiveView.PaperKind = System.Drawing.Printing.PaperKind.A4;
        //    worksheet.PrintOptions.FitToWidth = 1;

        //    // export usually view
        //    try
        //    {
        //        worksheet.Columns["A"][0].Value = $"{data.ToList()[0]}";
        //        worksheet.Columns["A"][1].Value = $"Технологический участок: {data.ToList()[1]}";
        //        worksheet.Columns["A"][2].Value = $"Интервал времени: {data.ToList()[2]}";

        //        // steps
        //        int rowNumber = 6;
        //        int index = 1;
        //        foreach (var prot in protections)
        //        {
        //            // index
        //            worksheet.Columns["A"][rowNumber].Value = index;
        //            // tag
        //            worksheet.Columns["B"][rowNumber].Value = prot.Tag;
        //            // name
        //            worksheet.Columns["C"][rowNumber].Value = prot.Name;
        //            // activation time
        //            worksheet.Columns["D"][rowNumber].Value = $"'{prot.TimeSet}";
        //            // deactivation time
        //            worksheet.Columns["E"][rowNumber].Value = $"'{prot.TimeUnset}";
        //            // duration
        //            worksheet.Columns["F"][rowNumber].Value = $"'{prot.Duration}";
        //            // duration secs
        //            worksheet.Columns["G"][rowNumber].Value = $"'{prot.DurationInSeconds}";

        //            worksheet.Range[$"A{rowNumber + 1}:G{rowNumber + 1}"].Borders.SetAllBorders(Color.Black, BorderLineStyle.Thin);

        //            rowNumber++;
        //            index++;
        //        }

        //        // auto-fit
        //        worksheet.Columns.AutoFit(0, 7);
        //        // center aligment
        //        for (int j = 0; j < 7; j++)
        //            worksheet.Columns[j].Alignment.Horizontal = SpreadsheetHorizontalAlignment.Center;

        //        var groupedList = GroupedProtection.MakeGroups(protections);
        //        rowNumber = 1;
        //        foreach (var item in groupedList)
        //        {
        //            groupedWorksheet.Columns["A"][rowNumber].Value = item.GroupName;
        //            groupedWorksheet.Columns["B"][rowNumber].Value = item.ActivationCount;
        //            groupedWorksheet.Columns["C"][rowNumber].Value = item.DurationInSeconds;
        //            rowNumber++;
        //        }

        //    }
        //    finally
        //    {
        //        workbook.EndUpdate();
        //    }

        //    savedFileName = SaveWorkbook(workbook, data.ToList()[3]);
        //}

        //public void ExportMasks(IEnumerable<string> data, IEnumerable<Mask> masks, out string savedFileName)
        //{
        //    Workbook workbook = new Workbook();
        //    workbook.LoadDocument(@"Templates\Masks.xlsx");
        //    Worksheet worksheet = workbook.Worksheets[0];
        //    Worksheet groupedWorksheet = workbook.Worksheets[1];
        //    workbook.Unit = DevExpress.Office.DocumentUnit.Point;
        //    workbook.BeginUpdate();

        //    // print opts
        //    worksheet.ActiveView.Orientation = PageOrientation.Landscape;
        //    worksheet.ActiveView.PaperKind = System.Drawing.Printing.PaperKind.A4;
        //    worksheet.PrintOptions.FitToWidth = 1;

        //    try
        //    {
        //        worksheet.Columns["A"][0].Value = $"{data.ToList()[0]}";
        //        worksheet.Columns["A"][1].Value = $"Технологический участок: {data.ToList()[1]}";
        //        worksheet.Columns["A"][2].Value = $"Интервал времени: {data.ToList()[2]}";

        //        // steps
        //        int rowNumber = 6;
        //        int index = 1;
        //        foreach (var mask in masks)
        //        {
        //            // index
        //            worksheet.Columns["A"][rowNumber].Value = index;
        //            // tag
        //            worksheet.Columns["B"][rowNumber].Value = mask.Tag;
        //            // name
        //            worksheet.Columns["C"][rowNumber].Value = mask.Name;
        //            // activation time
        //            worksheet.Columns["D"][rowNumber].Value = $"'{mask.TimeSet}";
        //            // deactivation time
        //            worksheet.Columns["E"][rowNumber].Value = $"'{mask.TimeUnset}";
        //            // duration
        //            worksheet.Columns["F"][rowNumber].Value = $"'{mask.Duration}";
        //            // duration in days
        //            worksheet.Columns["G"][rowNumber].Value = $"'{mask.DurationInDays}";
        //            // tor flag
        //            worksheet.Columns["H"][rowNumber].Value = $"'{mask.IsTorMask}";

        //            worksheet.Range[$"A{rowNumber + 1}:H{rowNumber + 1}"].Borders.SetAllBorders(Color.Black, BorderLineStyle.Thin);

        //            rowNumber++;
        //            index++;
        //        }

        //        // auto-fit
        //        worksheet.Columns.AutoFit(0, 7);
        //        // center aligment
        //        for (int j = 0; j < 7; j++)
        //            worksheet.Columns[j].Alignment.Horizontal = SpreadsheetHorizontalAlignment.Center;

        //        // groups in next worksheet
        //        var groupedList = GroupedProtection.MakeGroups(masks);
        //        rowNumber = 1;
        //        foreach (var item in groupedList)
        //        {
        //            groupedWorksheet.Columns["A"][rowNumber].Value = item.GroupName;
        //            groupedWorksheet.Columns["B"][rowNumber].Value = item.ActivationCount;
        //            groupedWorksheet.Columns["C"][rowNumber].Value = item.DurationInDays;
        //            rowNumber++;
        //        }

        //    }
        //    finally
        //    {
        //        workbook.EndUpdate();
        //    }

        //    savedFileName = SaveWorkbook(workbook, data.ToList()[3]);
        //}

        private Color GetFillColor(int code)
        {
            Color fillColor;
            if (code == 3)
                fillColor = Color.LightGreen;
            else if (code == 5)
                fillColor = Color.LightPink;
            else if (code == 7)
                fillColor = Color.Yellow;
            else if (code == 8 || code == 9)
                fillColor = Color.LightGray;
            else
                fillColor = Color.White;

            return fillColor;
        }

        private string SaveWorkbook(Workbook workbook, string fileName)
        {
            int i = 1;
            while (File.Exists($@"Reports\{fileName}.xlsx"))
            {
                if (!fileName.Contains($"({i})"))
                    fileName += $" ({i})";
                else
                {
                    fileName = fileName.Replace($"({i})", $"({i += 1})");
                }
            }
            workbook.SaveDocument($@"Reports\{fileName}.xlsx", DocumentFormat.OpenXml);
            FileInfo fileInfo = new FileInfo($@"Reports\{fileName}.xlsx");
            return fileInfo.FullName;
        }
    }

    public enum ExportType
    {
        Excel,
        Pdf,
        Word
    }
}
