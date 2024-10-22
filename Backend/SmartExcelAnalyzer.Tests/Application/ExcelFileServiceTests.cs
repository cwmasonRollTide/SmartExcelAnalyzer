using Moq;
using System.Data;
using System.Text;
using OfficeOpenXml;
using FluentAssertions;
using Domain.Application;
using Application.Services;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;

namespace SmartExcelAnalyzer.Tests.Application;

public class ExcelFileServiceTests
{
    private static ExcelFileService Sut => new();
    private readonly Mock<IFormFile> _mockFile = new();

    public ExcelFileServiceTests()
    {
        _mockFile.Setup(f => f.FileName).Returns("test.xlsx");
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    [Fact]
    public async Task PrepareExcelFileForLLMAsync_ValidFile_ReturnsSummarizedExcelData()
    {
        var excelData = CreateTestExcelData();
        SetupMockFileStream(excelData);

        var progressMock = new Mock<IProgress<(double, double)>>();
        var result = await Sut.PrepareExcelFileForLLMAsync(_mockFile.Object, progressMock.Object);

        result.Should().NotBeNull();
        result.Summary.Should().NotBeNull();
        result.Rows.Should().NotBeNull();
        result.Rows.Should().HaveCount(3);
        
        result.Summary.Should().ContainKey("Summary");
        var summary = result!.Summary!["Summary"].Should().BeOfType<ExcelFileSummary>().Subject;
        summary.RowCount.Should().Be(3);
        summary.ColumnCount.Should().Be(4);
        summary.Columns.Should().BeEquivalentTo(new List<string> { "ID", "Name", "Age", "Salary" });
        
        summary.Sums.Should().ContainKey("ID").WhoseValue.Should().Be(6);
        summary.Sums.Should().ContainKey("Age").WhoseValue.Should().Be(105);
        summary.Mins.Should().ContainKey("Age").WhoseValue.Should().Be(30);
        summary.Maxs.Should().ContainKey("ID").WhoseValue.Should().Be(3);
        summary.Maxs.Should().ContainKey("Age").WhoseValue.Should().Be(40);
        summary.Averages.Should().ContainKey("ID").WhoseValue.Should().BeApproximately(2, 0.01);
        summary.Averages.Should().ContainKey("Age").WhoseValue.Should().BeApproximately(35, 0.01);
        summary.Averages.Should().ContainKey("Salary").WhoseValue.Should().BeApproximately(30, 0.01);
        summary.HashedStrings.Should().ContainKey("Name").WhoseValue.Should().HaveCount(3);

        progressMock.Verify(p => p.Report(It.IsAny<(double, double)>()), Times.AtLeastOnce());
    }

    [Fact]
    public async Task PrepareExcelFileForLLMAsync_EmptyFile_ReturnsEmptySummarizedExcelData()
    {
        var excelData = CreateEmptyExcelData();
        SetupMockFileStream(excelData);

        var progressMock = new Mock<IProgress<(double, double)>>();
        var result = await Sut.PrepareExcelFileForLLMAsync(_mockFile.Object, progressMock.Object);

        result.Should().NotBeNull();
        result.Summary.Should().NotBeNull();
        result.Rows.Should().BeEmpty();
        result.Summary.Should().ContainKey("Summary");
        var summary = result!.Summary!["Summary"].Should().BeOfType<ExcelFileSummary>().Subject;
        summary.RowCount.Should().Be(0);
        summary.ColumnCount.Should().Be(4);
        summary.Columns.Should().BeEquivalentTo(new List<string> { "ID", "Name", "Age", "Salary" });
        
        summary.Sums.Should().BeEmpty();
        summary.Mins.Should().BeEmpty();
        summary.Maxs.Should().BeEmpty();
        summary.Averages.Should().BeEmpty();
        summary.HashedStrings.Should().BeEmpty();

        progressMock.Verify(p => p.Report(It.IsAny<(double, double)>()), Times.AtLeastOnce());
    }

    [Fact]
    public async Task PrepareExcelFileForLLMAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        var excelData = CreateTestExcelData();
        SetupMockFileStream(excelData);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        var progressMock = new Mock<IProgress<(double, double)>>();
        await Sut.Invoking(s => s.PrepareExcelFileForLLMAsync(_mockFile.Object, progressMock.Object, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task PrepareExcelFileForLLMAsync_ReportsProgress_DuringFileProcessing()
    {
        var excelData = CreateTestExcelData();
        SetupMockFileStream(excelData);

        var progressReports = new List<(double, double)>();
        var progress = new Progress<(double, double)>(report => progressReports.Add(report));

        await Sut.PrepareExcelFileForLLMAsync(_mockFile.Object, progress);

        progressReports.Should().NotBeEmpty();
        progressReports.First().Item1.Should().Be(0); // Starts at 0
        progressReports.Should().Contain(r => r.Item1 > 0 && r.Item1 < 1); // Has intermediate progress
        progressReports.Should().OnlyContain(report => 
            report.Item1 >= 0 && 
            report.Item1 <= 1 && 
            report.Item2 == 0);

        progressReports.Should().HaveCountGreaterThan(2);
    }

    [Fact]
    public void ComputeHash_ValidInput_ReturnsCorrectHash()
    {
        var input = "test";
        var expectedHash = string.Concat(SHA256.HashData(Encoding.UTF8.GetBytes(input)).Select(b => b.ToString("x2")));

        var result = typeof(ExcelFileService).GetMethod("ComputeHash", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?.Invoke(null, [input]) as string;

        result.Should().Be(expectedHash);
    }

    private static byte[] CreateTestExcelData()
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Sheet1");
        
        worksheet.Cells["A1"].Value = "ID";
        worksheet.Cells["B1"].Value = "Name";
        worksheet.Cells["C1"].Value = "Age";
        worksheet.Cells["D1"].Value = "Salary";

        worksheet.Cells["A2"].Value = 1;
        worksheet.Cells["B2"].Value = "Alice";
        worksheet.Cells["C2"].Value = 30;
        worksheet.Cells["D2"].Value = 25.0;

        worksheet.Cells["A3"].Value = 2;
        worksheet.Cells["B3"].Value = "Bob";
        worksheet.Cells["C3"].Value = 35;
        worksheet.Cells["D3"].Value = 40.0;

        worksheet.Cells["A4"].Value = 3;
        worksheet.Cells["B4"].Value = "Charlie";
        worksheet.Cells["C4"].Value = 40;
        worksheet.Cells["D4"].Value = 25.0;

        worksheet.Column(1).Style.Numberformat.Format = "0";

        return package.GetAsByteArray();
    }

    private static byte[] CreateEmptyExcelData()
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Sheet1");
        
        worksheet.Cells["A1"].Value = "ID";
        worksheet.Cells["B1"].Value = "Name";
        worksheet.Cells["C1"].Value = "Age";
        worksheet.Cells["D1"].Value = "Salary";

        return package.GetAsByteArray();
    }

    private void SetupMockFileStream(byte[] excelData)
    {
        var memoryStream = new MemoryStream(excelData);
        _mockFile.Setup(f => f.OpenReadStream()).Returns(memoryStream);
    }
}
