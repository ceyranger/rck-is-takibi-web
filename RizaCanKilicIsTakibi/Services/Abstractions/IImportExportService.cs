using RizaCanKilicIsTakibi.Models;
using System.Windows;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface IImportExportService
{
    Task ExportExcelAsync(IEnumerable<TaskItem> tasks, string filePath, CancellationToken cancellationToken = default);
    Task ExportWorkbookAsync(ExcelWorkbookExportModel workbook, string filePath, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskItem>> ImportExcelAsync(string filePath, CancellationToken cancellationToken = default);
    Task ExportPdfAsync(IEnumerable<TaskItem> tasks, string filePath, CancellationToken cancellationToken = default);
    Task ExportPngAsync(UIElement visual, string filePath, CancellationToken cancellationToken = default);
    Task ExportScrollablePngAsync(UIElement visual, string filePath, CancellationToken cancellationToken = default);
}
