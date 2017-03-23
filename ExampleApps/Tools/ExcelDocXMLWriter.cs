using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using RLM.Models.Utility;

namespace Tools
{
    public class ExcelDocXMLWriter
    {
        private SpreadsheetDocument document;
        private WorkbookPart workbookPart;
        //private WorksheetPart worksheetPart;
        //private Sheets sheets;

        private uint sheetIdCounter = 2;
        private uint SUMMARY_SHEET_ID = 1;

        public ExcelDocXMLWriter(string file)
        {
            document = SpreadsheetDocument.Create(file, DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook);

            // Add a WorkbookPart to the document.
            workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            //// Add a WorksheetPart to the WorkbookPart.
            //worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            //worksheetPart.Worksheet = new Worksheet(new SheetData());

            var sheets = workbookPart.Workbook.AppendChild(new Sheets());

            //Sheet sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = SUMMARY_SHEET_ID, Name = "Blank" };
            //sheets.Append(sheet);

            workbookPart.Workbook.Save();
        }

        public void WriteRNNBenchmark(RlmBenchmarkStats stats)
        {
            foreach(var item in stats.Sessions)
            {
                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                worksheetPart.Worksheet = new Worksheet(new SheetData());
                worksheetPart.Worksheet.Save();

                var sheets = workbookPart.Workbook.GetFirstChild<Sheets>(); //workbookPart.Workbook.AppendChild(new Sheets());

                Sheet sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = new DocumentFormat.OpenXml.UInt32Value(sheetIdCounter), Name = item.Key };
                sheets.Append(sheet);

                workbookPart.Workbook.Save();

                SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>(); //worksheetPart.Worksheet.AppendChild(new SheetData());

                // add header
                Row row = new Row();
                row.Append(ConstructCell("Session", CellValues.String),
                    ConstructCell("Score", CellValues.String),
                    ConstructCell("TotalTime (s)", CellValues.String),
                    ConstructCell("TrainingTime (ms)", CellValues.String),
                    ConstructCell("GetBestSolution (ms)", CellValues.String),
                    ConstructCell("% of Training Time", CellValues.String),
                    ConstructCell("RebuildCacheBox (ms)", CellValues.String),
                    ConstructCell("% of Training Time", CellValues.String),
                    ConstructCell("NumCycles", CellValues.String),
                    ConstructCell("NumCacheRebuild", CellValues.String),
                    ConstructCell("LinearTolerance", CellValues.String),
                    ConstructCell("RandomnessLeft", CellValues.String));

                sheetData.AppendChild(row);

                // inserts data
                foreach(var sess in item.Value)
                {
                    Row sessRow = new Row();
                    sessRow.Append(ConstructCell(sess.SessionNumber.ToString(), CellValues.Number),
                        ConstructCell(sess.Score.ToString(), CellValues.Number),
                        ConstructCell(sess.Totaltime.ToString(), CellValues.Number),
                        ConstructCell(sess.TrainingTime.ToString(), CellValues.Number),
                        ConstructCell(sess.GetBestSolution.ToString(), CellValues.Number),
                        ConstructCell(sess.GetBestSolutionOverTrainingTime.ToString(), CellValues.Number),
                        ConstructCell(sess.RebuildCacheBox.ToString(), CellValues.Number),
                        ConstructCell(sess.RebuildCacheBoxOverTrainingTime.ToString(), CellValues.Number),
                        ConstructCell(sess.NumberOfCycles.ToString(), CellValues.Number),
                        ConstructCell(sess.NumberOfCacheBoxRebuilds.ToString(), CellValues.Number),
                        ConstructCell(sess.LinearTolerance.ToString(), CellValues.Number),
                        ConstructCell(sess.RandomnessLeft.ToString(), CellValues.Number));

                    sheetData.AppendChild(sessRow);
                }


                sheetIdCounter++;
                worksheetPart.Worksheet.Save();
            }

            var summWorksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            summWorksheetPart.Worksheet = new Worksheet(new SheetData());
            summWorksheetPart.Worksheet.Save();

            var summSheets = workbookPart.Workbook.GetFirstChild<Sheets>();//workbookPart.Workbook.AppendChild(new Sheets());

            // sheet for summary
            Sheet summSheet = new Sheet() { Id = workbookPart.GetIdOfPart(summWorksheetPart), SheetId = new DocumentFormat.OpenXml.UInt32Value(sheetIdCounter), Name = "Summary" };
            summSheets.Append(summSheet);

            SheetData summSheetData = summWorksheetPart.Worksheet.GetFirstChild<SheetData>();//summWorksheetPart.Worksheet.AppendChild(new SheetData());

            // add header
            Row summHeaderRow = new Row();
            summHeaderRow.Append(ConstructCell("Trial Name", CellValues.String),
                ConstructCell("Average Number of Cycles per session", CellValues.String),
                ConstructCell("Average Number of Cache box rebuild", CellValues.String),
                ConstructCell("Average time for Get Best Solution (inside training)", CellValues.String),
                ConstructCell("Average time for Cache box rebuild (inside training)", CellValues.String),
                ConstructCell("Average time for Training", CellValues.String),
                ConstructCell("Average time for Best Solution rebuild", CellValues.String),
                ConstructCell("Average time for each Session", CellValues.String),
                ConstructCell("Elapsed", CellValues.String),
                ConstructCell("Average score", CellValues.String),
                ConstructCell("Rneurons Count", CellValues.String));

            summSheetData.AppendChild(summHeaderRow);

            // inserts data
            foreach (var summ in stats.Summary)
            {
                Row summRow = new Row();
                summRow.Append(ConstructCell(summ.Key.ToString(), CellValues.String),
                    ConstructCell(summ.Value.AverageNumberOfCycles.ToString(), CellValues.Number),
                    ConstructCell(summ.Value.AverageNumberOfCacheBoxRebuild.ToString(), CellValues.Number),
                    ConstructCell(summ.Value.AverageTimeForGetBestSolution.ToString(), CellValues.Number),
                    ConstructCell(summ.Value.AverageTimeForCacheBoxRebuild.ToString(), CellValues.Number),
                    ConstructCell(summ.Value.AverageTimeForTraining.ToString(), CellValues.Number),
                    ConstructCell(summ.Value.AverageTimeForBestSolutionRebuild.ToString(), CellValues.Number),
                    ConstructCell(summ.Value.AverageTimeForEachSession.ToString(), CellValues.Number),
                    ConstructCell(summ.Value.ElapsedTime.ToString(), CellValues.String),
                    ConstructCell(summ.Value.AverageScore.ToString(), CellValues.Number),
                    ConstructCell(summ.Value.RnueronCount.ToString(), CellValues.Number));

                summSheetData.AppendChild(summRow);
            }


            sheetIdCounter++;
            summWorksheetPart.Worksheet.Save();
        }
        
        private Cell ConstructCell(string value, CellValues dataType)
        {
            return new Cell()
            {
                CellValue = new CellValue(value),
                DataType = new DocumentFormat.OpenXml.EnumValue<CellValues>(dataType)
            };
        }

        public void Save()
        {
            workbookPart.Workbook.Save();
            document.Close();
        }
    }
}
