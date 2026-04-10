using System;
using System.Drawing;
using System.IO;
using OfficeOpenXml;
using OfficeOpenXml.Style;

public class ExcelHelper
{
    private string _excelFilePath;

    public ExcelHelper(string excelFilePath)
    {
        _excelFilePath = excelFilePath;

        // --- CÚ PHÁP MỚI CỦA EPPLUS 8 ---
        // Khai báo bản quyền phi thương mại/cá nhân
        ExcelPackage.License.SetNonCommercialPersonal("Danh");
    }

    /// <summary>
    /// Hàm ghi kết quả tự động tìm dòng theo Test Case ID
    /// </summary>
    public void WriteTestResultById(string sheetName, string testCaseId, string actualResult, string status, string testerName, string screenshotPath = "")
    {
        FileInfo fileInfo = new FileInfo(_excelFilePath);

        using (ExcelPackage package = new ExcelPackage(fileInfo))
        {
            ExcelWorksheet worksheet = package.Workbook.Worksheets[sheetName];
            if (worksheet == null)
            {
                Console.WriteLine($"Không tìm thấy sheet: {sheetName}");
                return;
            }

            int targetRow = -1;
            int totalRows = worksheet.Dimension?.Rows ?? 0;
            int idColumnIndex = 3; // Cột C chứa ID

            for (int row = 1; row <= totalRows; row++)
            {
                var cell = worksheet.Cells[row, idColumnIndex].Value;
                if (cell != null)
                {
                    string cellValue = cell.ToString().Trim();
                    if (cellValue.Equals(testCaseId, StringComparison.OrdinalIgnoreCase))
                    {
                        targetRow = row;
                        break;
                    }
                }
            }

            if (targetRow == -1)
            {
                Console.WriteLine($"Không tìm thấy ID: {testCaseId} trong sheet {sheetName}");
                return;
            }

            worksheet.Cells[targetRow, 10].Value = actualResult;
            worksheet.Cells[targetRow, 10].Style.WrapText = true;

            var resultCell = worksheet.Cells[targetRow, 11];
            resultCell.Value = status;
            resultCell.Style.Font.Bold = true;

            if (status.Equals("Pass", StringComparison.OrdinalIgnoreCase))
            {
                resultCell.Style.Font.Color.SetColor(Color.Green);
            }
            else if (status.Equals("Fail", StringComparison.OrdinalIgnoreCase))
            {
                resultCell.Style.Font.Color.SetColor(Color.Red);
            }

            worksheet.Cells[targetRow, 12].Value = testerName;

            if (!string.IsNullOrEmpty(screenshotPath))
            {
                var noteCell = worksheet.Cells[targetRow, 13];
                noteCell.Formula = $"HYPERLINK(\"{screenshotPath}\", \"Xem ảnh lỗi\")";
                noteCell.Style.Font.Color.SetColor(Color.Blue);
                noteCell.Style.Font.UnderLine = true;
            }

            package.Save();
        }
    }
}