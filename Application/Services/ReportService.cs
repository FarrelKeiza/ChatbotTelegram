// Application/Services/ReportService.cs
using Application.Dtos;
using ClosedXML.Excel; // Import ClosedXML
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Services
{
    public class ReportService
    {
        private readonly UserService _userService;

        public ReportService(UserService userService)
        {
            _userService = userService;
        }

        public async Task<(Stream fileStream, string fileName)> GenerateUserReportAsync()
        {
            var users = await _userService.GetAllUsersAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.AddWorksheet("User Report");

            // Tambahkan header
            worksheet.Cell("A1").Value = "Name";
            worksheet.Cell("B1").Value = "Age";
            worksheet.Cell("C1").Value = "Hobby";
            //worksheet.Cell("D1").Value = "Username";
            // Anda bisa tambahkan kolom lain dari UserDto jika diperlukan (misal Email)
            // worksheet.Cell("E1").Value = "Email";

            // Aplikasikan gaya ke header
            worksheet.Range("A1:D1").Style.Font.Bold = true;
            worksheet.Range("A1:D1").Style.Fill.BackgroundColor = XLColor.LightGray;
            worksheet.Range("A1:D1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Isi data user
            int row = 2;
            foreach (var user in users)
            {
                worksheet.Cell(row, 1).Value = user.Name;
                worksheet.Cell(row, 2).Value = user.Age;
                worksheet.Cell(row, 3).Value = user.Hobby;
                //worksheet.Cell(row, 4).Value = user.Username;
                // worksheet.Cell(row, 5).Value = user.Email; // Jika Anda menambahkan kolom Email di header
                row++;
            }

            // Atur lebar kolom secara otomatis
            worksheet.Columns().AdjustToContents();

            // Simpan workbook ke MemoryStream
            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0; // Reset stream position ke awal

            var fileName = $"User_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return (stream, fileName);
        }
    }
}