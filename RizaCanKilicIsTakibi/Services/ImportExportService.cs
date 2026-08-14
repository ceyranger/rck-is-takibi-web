using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Media = System.Windows.Media;
using WpfFontWeight = System.Windows.FontWeight;

namespace RizaCanKilicIsTakibi.Services;

public sealed class ImportExportService : IImportExportService
{
    public Task ExportExcelAsync(IEnumerable<TaskItem> tasks, string filePath, CancellationToken cancellationToken = default)
    {
        var workbook = new ExcelWorkbookExportModel
        {
            Sheets =
            [
                new ExcelSheetExportModel
                {
                    Name = "İşler",
                    Headers = ["Başlık", "Açıklama", "Bitiş Tarihi", "Tablo"],
                    Rows = tasks.Select(task => new ExcelRowExportModel
                    {
                        Cells =
                        [
                            new ExcelCellExportModel
                            {
                                Value = task.Title,
                                Comment = string.Join(Environment.NewLine, task.Notes.Select(note => note.Text).Where(text => !string.IsNullOrWhiteSpace(text)))
                            },
                            new ExcelCellExportModel { Value = task.Description },
                            new ExcelCellExportModel { Value = task.DueDate?.ToString("dd.MM.yyyy") ?? string.Empty },
                            new ExcelCellExportModel { Value = task.BoardType == TaskBoardType.Acil ? "Acil" : "Genel" }
                        ]
                    }).ToList()
                }
            ]
        };

        return ExportWorkbookAsync(workbook, filePath, cancellationToken);
    }

    public Task ExportWorkbookAsync(ExcelWorkbookExportModel workbook, string filePath, CancellationToken cancellationToken = default)
    {
        if (workbook.Sheets.Count == 0)
        {
            throw new InvalidOperationException("Excel dışa aktarma için geçerli sheet bulunamadı.");
        }

        using var xlWorkbook = new XLWorkbook();
        var usedSheetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var headerFill = XLColor.FromHtml("#1F3147");
        var zebraFill = XLColor.FromHtml("#F5F8FB");
        var gridBorder = XLColor.FromHtml("#D0D7E2");
        var headerBottom = XLColor.FromHtml("#0F1C2C");

        foreach (var sheet in workbook.Sheets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sheetName = GetUniqueSheetName(usedSheetNames, sheet.Name);
            var worksheet = xlWorkbook.Worksheets.Add(sheetName);
            var columnCount = Math.Max(1, sheet.Headers.Count);

            for (var column = 0; column < sheet.Headers.Count; column++)
            {
                var headerCell = worksheet.Cell(1, column + 1);
                headerCell.Value = sheet.Headers[column];
                headerCell.Style.Font.Bold = true;
                headerCell.Style.Fill.BackgroundColor = headerFill;
                headerCell.Style.Font.FontColor = XLColor.White;
                headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                headerCell.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
                headerCell.Style.Border.BottomBorderColor = headerBottom;
            }

            for (var rowIndex = 0; rowIndex < sheet.Rows.Count; rowIndex++)
            {
                var row = sheet.Rows[rowIndex];
                for (var columnIndex = 0; columnIndex < row.Cells.Count; columnIndex++)
                {
                    var exportCell = row.Cells[columnIndex];
                    var cell = worksheet.Cell(rowIndex + 2, columnIndex + 1);
                    cell.Value = exportCell.Value;
                    cell.Style.Alignment.WrapText = true;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    var normalizedColor = NormalizeExcelColor(exportCell.BackgroundColor);
                    if (!string.IsNullOrWhiteSpace(normalizedColor))
                    {
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml(normalizedColor);
                    }
                    else if (rowIndex % 2 == 1)
                    {
                        cell.Style.Fill.BackgroundColor = zebraFill;
                    }

                    if (!string.IsNullOrWhiteSpace(exportCell.Comment))
                    {
                        var comment = cell.CreateComment();
                        comment.ClearText();
                        comment.AddText(exportCell.Comment);
                    }
                }
            }

            var lastRow = Math.Max(1, sheet.Rows.Count + 1);
            var usedRange = worksheet.Range(1, 1, lastRow, columnCount);
            usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            usedRange.Style.Border.OutsideBorderColor = gridBorder;
            usedRange.Style.Border.InsideBorderColor = gridBorder;
            usedRange.SetAutoFilter();

            worksheet.SheetView.FreezeRows(1);
            worksheet.ColumnsUsed().AdjustToContents(8, 80);
        }

        xlWorkbook.SaveAs(filePath);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<TaskItem>> ImportExcelAsync(string filePath, CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.First();
        var tasks = new List<TaskItem>();

        var firstRow = worksheet.FirstRowUsed();
        if (firstRow is null)
        {
            return Task.FromResult<IReadOnlyList<TaskItem>>(tasks);
        }

        var headerMap = firstRow.Cells()
            .ToDictionary(cell => Normalize(cell.GetString()), cell => cell.Address.ColumnNumber);

        foreach (var row in worksheet.RowsUsed().Skip(1))
        {
            var title = ReadCell(row, headerMap, "baslik");
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = ReadCell(row, headerMap, "aciklama"),
                BoardType = ParseBoard(ReadCell(row, headerMap, "tablo")),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                SortOrder = tasks.Count
            };

            var dueDateText = ReadCell(row, headerMap, "bitistarihi");
            if (DateTime.TryParse(dueDateText, out var dueDate))
            {
                task.DueDate = dueDate;
            }

            var notes = ReadCell(row, headerMap, "notlar");
            if (!string.IsNullOrWhiteSpace(notes))
            {
                foreach (var note in notes.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                {
                    task.Notes.Add(new TaskNote { Text = note, CreatedAt = DateTime.Now });
                }
            }

            tasks.Add(task);
        }

        return Task.FromResult<IReadOnlyList<TaskItem>>(tasks);
    }

    public Task ExportPdfAsync(IEnumerable<TaskItem> tasks, string filePath, CancellationToken cancellationToken = default)
    {
        var data = tasks.ToList();
        const string headerColor = "#1F3147";
        const string zebraColor = "#F5F8FB";
        const string borderColor = "#D0D7E2";

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(QuestPDF.Helpers.PageSizes.A4);
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(column =>
                {
                    column.Item().Text("RIZA CAN KILIÇ İŞ TAKİBİ RAPORU").FontSize(18).Bold();
                    column.Item().Text($"Tarih: {DateTime.Now:dd.MM.yyyy HH:mm}").FontColor(QuestPDF.Helpers.Colors.Grey.Darken2);
                    column.Item().PaddingTop(6).LineHorizontal(1).LineColor(headerColor);
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("sayfa ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });

                page.Content().PaddingVertical(16).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(1.5f);
                        columns.RelativeColumn(1.2f);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(c => HeaderCellStyle(c, headerColor)).Text("Başlık").Bold().FontColor(QuestPDF.Helpers.Colors.White);
                        header.Cell().Element(c => HeaderCellStyle(c, headerColor)).Text("Açıklama").Bold().FontColor(QuestPDF.Helpers.Colors.White);
                        header.Cell().Element(c => HeaderCellStyle(c, headerColor)).Text("Tablo").Bold().FontColor(QuestPDF.Helpers.Colors.White);
                        header.Cell().Element(c => HeaderCellStyle(c, headerColor)).Text("Not Sayısı").Bold().FontColor(QuestPDF.Helpers.Colors.White);
                    });

                    for (var rowIndex = 0; rowIndex < data.Count; rowIndex++)
                    {
                        var item = data[rowIndex];
                        var background = rowIndex % 2 == 1 ? zebraColor : null;
                        table.Cell().Element(c => BodyCellStyle(c, borderColor, background)).Text(item.Title);
                        table.Cell().Element(c => BodyCellStyle(c, borderColor, background)).Text(item.Description);
                        table.Cell().Element(c => BodyCellStyle(c, borderColor, background)).Text(item.BoardType == TaskBoardType.Acil ? "Acil" : "Genel");
                        table.Cell().Element(c => BodyCellStyle(c, borderColor, background)).Text(item.Notes.Count.ToString());
                    }
                });
            });
        }).GeneratePdf(filePath);

        return Task.CompletedTask;
    }

    public Task ExportReportPackAsync(ReportPackExportModel pack, string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pack);

        const string headerColor = "#1F3147";
        const string zebraColor = "#F5F8FB";
        const string borderColor = "#D0D7E2";

        Document.Create(container =>
        {
            foreach (var section in pack.Sections)
            {
                cancellationToken.ThrowIfCancellationRequested();

                container.Page(page =>
                {
                    page.Size(QuestPDF.Helpers.PageSizes.A3);
                    page.Margin(18);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(column =>
                    {
                        column.Item().Text(pack.Title).FontSize(14).Bold();
                        column.Item().Text($"{section.Title}  •  {DateTime.Now:dd.MM.yyyy HH:mm}")
                            .FontColor(QuestPDF.Helpers.Colors.Grey.Darken2);
                        column.Item().PaddingTop(4).LineHorizontal(1).LineColor(headerColor);
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("sayfa ");
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });

                    page.Content().PaddingTop(10).Table(table =>
                    {
                        var columnCount = Math.Max(1, section.Headers.Count);
                        table.ColumnsDefinition(columns =>
                        {
                            for (var index = 0; index < columnCount; index++)
                            {
                                columns.RelativeColumn();
                            }
                        });

                        table.Header(header =>
                        {
                            foreach (var title in section.Headers)
                            {
                                header.Cell().Element(c => HeaderCellStyle(c, headerColor))
                                    .Text(title).Bold().FontColor(QuestPDF.Helpers.Colors.White);
                            }

                            for (var index = section.Headers.Count; index < columnCount; index++)
                            {
                                header.Cell().Element(c => HeaderCellStyle(c, headerColor)).Text(string.Empty);
                            }
                        });

                        if (section.Rows.Count == 0)
                        {
                            table.Cell().ColumnSpan((uint)columnCount)
                                .Element(c => BodyCellStyle(c, borderColor, null))
                                .Text("Kayıt yok.");
                        }
                        else
                        {
                            for (var rowIndex = 0; rowIndex < section.Rows.Count; rowIndex++)
                            {
                                var row = section.Rows[rowIndex];
                                for (var index = 0; index < columnCount; index++)
                                {
                                    var cell = index < row.Count ? row[index] : null;
                                    var value = cell?.Value ?? string.Empty;
                                    var cellColor = NormalizeExcelColor(cell?.BackgroundColor);
                                    var background = !string.IsNullOrWhiteSpace(cellColor)
                                        ? cellColor
                                        : rowIndex % 2 == 1 ? zebraColor : null;
                                    table.Cell().Element(c => BodyCellStyle(c, borderColor, background)).Text(value);
                                }
                            }
                        }
                    });
                });
            }
        }).GeneratePdf(filePath);

        return Task.CompletedTask;
    }

    public Task ExportWorkbookAsPdfAsync(ExcelWorkbookExportModel workbook, string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workbook);

        var sheet = workbook.Sheets.FirstOrDefault()
            ?? throw new InvalidOperationException("PDF dışa aktarma için geçerli sheet bulunamadı.");

        const string headerColor = "#1F3147";
        const string zebraColor = "#F5F8FB";
        const string borderColor = "#D0D7E2";
        var columnCount = Math.Max(1, sheet.Headers.Count);
        var title = string.IsNullOrWhiteSpace(sheet.Name) ? "Rapor" : sheet.Name;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(QuestPDF.Helpers.PageSizes.A4);
                page.Margin(18);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(column =>
                {
                    column.Item().Text(title).FontSize(14).Bold();
                    column.Item().Text($"Tarih: {DateTime.Now:dd.MM.yyyy HH:mm}").FontColor(QuestPDF.Helpers.Colors.Grey.Darken2);
                    column.Item().PaddingTop(4).LineHorizontal(1).LineColor(headerColor);
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("sayfa ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });

                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        for (var i = 0; i < columnCount; i++)
                        {
                            columns.RelativeColumn();
                        }
                    });

                    table.Header(header =>
                    {
                        for (var i = 0; i < columnCount; i++)
                        {
                            var headerText = i < sheet.Headers.Count ? sheet.Headers[i] : string.Empty;
                            header.Cell().Element(c => HeaderCellStyle(c, headerColor))
                                .Text(headerText).Bold().FontColor(QuestPDF.Helpers.Colors.White);
                        }
                    });

                    for (var rowIndex = 0; rowIndex < sheet.Rows.Count; rowIndex++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var row = sheet.Rows[rowIndex];
                        var background = rowIndex % 2 == 1 ? zebraColor : null;
                        for (var i = 0; i < columnCount; i++)
                        {
                            var value = i < row.Cells.Count ? row.Cells[i].Value ?? string.Empty : string.Empty;
                            table.Cell().Element(c => BodyCellStyle(c, borderColor, background)).Text(value);
                        }
                    }
                });
            });
        }).GeneratePdf(filePath);

        return Task.CompletedTask;
    }

    private static IContainer HeaderCellStyle(IContainer container, string headerColor)
        => container
            .Background(headerColor)
            .PaddingVertical(5)
            .PaddingHorizontal(3)
            .BorderBottom(1)
            .BorderColor("#0F1C2C");

    private static IContainer BodyCellStyle(IContainer container, string borderColor, string? backgroundColor)
    {
        var styled = container.PaddingVertical(4).PaddingHorizontal(3)
            .BorderBottom(1)
            .BorderColor(borderColor);
        return string.IsNullOrWhiteSpace(backgroundColor)
            ? styled
            : styled.Background(backgroundColor);
    }

    public Task ExportPngAsync(UIElement visual, string filePath, CancellationToken cancellationToken = default)
        => ExportScrollablePngAsync(visual, filePath, cancellationToken);

    public Task ExportScrollablePngAsync(UIElement visual, string filePath, CancellationToken cancellationToken = default)
    {
        if (visual is not FrameworkElement rootElement)
        {
            throw new InvalidOperationException("PNG dışa aktarma için geçerli bir UI öğesi seçilmedi.");
        }

        if (rootElement.ActualWidth < 1 || rootElement.ActualHeight < 1)
        {
            throw new InvalidOperationException("Görüntü alınacak öğenin boyutu geçersiz.");
        }

        if (TryExportActionListFromData(rootElement, filePath, cancellationToken))
        {
            return Task.CompletedTask;
        }

        var exportDataGrid = ResolveExportDataGrid(rootElement);
        var scrollViewer = exportDataGrid is not null
            ? ResolveDataGridScrollViewer(exportDataGrid)
            : ResolvePrimaryScrollViewer(rootElement);
        if (scrollViewer is null)
        {
            return ExportStaticVisualAsync(rootElement, filePath);
        }

        var bodyViewport = exportDataGrid is not null
            ? ResolveDataGridBodyViewport(exportDataGrid, scrollViewer)
            : ResolveScrollableViewport(scrollViewer) ?? (FrameworkElement)scrollViewer;
        var originalOffset = scrollViewer.VerticalOffset;
        var exportScrollSettings = CaptureAndApplyExportScrollSettings(
            rootElement,
            scrollViewer,
            bodyViewport,
            exportDataGrid is null ? null : new[] { exportDataGrid });
        var dataGridVirtualizationSetting = CaptureAndApplyDataGridVirtualizationSettings(exportDataGrid);

        rootElement.UpdateLayout();
        bodyViewport.UpdateLayout();
        scrollViewer.UpdateLayout();

        var rootWidth = (int)Math.Ceiling(rootElement.ActualWidth);
        var rootHeight = (int)Math.Ceiling(rootElement.ActualHeight);
        var bodyWidth = (int)Math.Ceiling(Math.Max(1, bodyViewport.ActualWidth));
        var bodyFrameHeight = (int)Math.Ceiling(Math.Max(1, bodyViewport.ActualHeight));
        if (rootWidth <= 0 || rootHeight <= 0 || bodyWidth <= 0 || bodyFrameHeight <= 0)
        {
            throw new InvalidOperationException("Görüntü alınacak tablo ölçüleri hesaplanamadı.");
        }

        var bodyOffset = bodyViewport.TransformToAncestor(rootElement).Transform(new Point(0, 0));
        var bodyOffsetX = Math.Max(0, (int)Math.Round(bodyOffset.X));
        var headerHeight = Math.Max(0, (int)Math.Round(bodyOffset.Y));
        headerHeight = Math.Min(headerHeight, rootHeight);
        bodyOffsetX = Math.Min(bodyOffsetX, Math.Max(0, rootWidth - bodyWidth));

        const int maxPngHeight = 16000;
        var totalBodyHeight = CalculateTotalBodyHeight(scrollViewer, bodyFrameHeight, exportDataGrid);
        var maxScrollableOffset = Math.Max(0, totalBodyHeight - bodyFrameHeight);
        try
        {
            var firstFrame = CaptureViewportFrame(rootElement, scrollViewer, rootElement, 0, rootWidth, rootHeight, out _);
            var headerBitmap = headerHeight > 0
                ? new CroppedBitmap(firstFrame, new Int32Rect(0, 0, rootWidth, Math.Min(headerHeight, firstFrame.PixelHeight)))
                : null;

            var directory = Path.GetDirectoryName(filePath);
            var filename = Path.GetFileNameWithoutExtension(filePath);
            var extension = Path.GetExtension(filePath);
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".png";
            }

            var globalBodyOffset = 0d;
            var pageIndex = 1;
            var exportedAnyPage = false;

            while (globalBodyOffset < totalBodyHeight - 0.5)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var includeHeader = pageIndex == 1 && headerBitmap is not null;
                var pageHeaderBitmap = includeHeader ? headerBitmap : null;
                var pageHeaderHeight = includeHeader ? headerHeight : 0;
                var pageBodyCapacity = Math.Max(1, maxPngHeight - pageHeaderHeight);
                var pageSlices = new List<(BitmapSource Slice, int Height)>();
                var drawnBodyHeight = 0;
                var previousGlobalOffset = globalBodyOffset;

                while (drawnBodyHeight < pageBodyCapacity && globalBodyOffset < totalBodyHeight - 0.5)
                {
                    var requestedOffset = Math.Max(0, Math.Min(maxScrollableOffset, globalBodyOffset));
                    var bodyFrame = CaptureViewportFrame(
                        bodyViewport,
                        scrollViewer,
                        rootElement,
                        requestedOffset,
                        bodyWidth,
                        bodyFrameHeight,
                        out var actualOffset);

                    var sourceY = (int)Math.Max(0, Math.Floor(globalBodyOffset - actualOffset));
                    if (sourceY >= bodyFrame.PixelHeight)
                    {
                        if (actualOffset >= maxScrollableOffset - 0.5)
                        {
                            break;
                        }

                        var forcedAdvance = Math.Max(globalBodyOffset + 1, Math.Floor(actualOffset + bodyFrame.PixelHeight));
                        if (forcedAdvance <= globalBodyOffset + 0.1)
                        {
                            break;
                        }

                        globalBodyOffset = forcedAdvance;
                        continue;
                    }

                    var availableInFrame = bodyFrame.PixelHeight - sourceY;
                    var pageRemaining = pageBodyCapacity - drawnBodyHeight;
                    var totalRemaining = Math.Max(0, (int)Math.Ceiling(totalBodyHeight - globalBodyOffset));
                    var sliceHeight = Math.Min(availableInFrame, Math.Min(pageRemaining, totalRemaining));
                    if (sliceHeight <= 0)
                    {
                        break;
                    }

                    var slice = new CroppedBitmap(bodyFrame, new Int32Rect(0, sourceY, bodyWidth, sliceHeight));
                    pageSlices.Add((slice, sliceHeight));
                    drawnBodyHeight += sliceHeight;
                    globalBodyOffset += sliceHeight;
                }

                if (drawnBodyHeight <= 0)
                {
                    break;
                }

                var pageVisual = new DrawingVisual();
                var rawPageHeight = pageHeaderHeight + drawnBodyHeight;
                using (var drawingContext = pageVisual.RenderOpen())
                {
                    drawingContext.DrawRectangle(Brushes.White, null, new Rect(0, 0, rootWidth, rawPageHeight));
                    if (pageHeaderBitmap is not null)
                    {
                        drawingContext.DrawImage(pageHeaderBitmap, new Rect(0, 0, rootWidth, pageHeaderHeight));
                    }

                    var currentY = pageHeaderHeight;
                    foreach (var (slice, height) in pageSlices)
                    {
                        drawingContext.DrawImage(slice, new Rect(bodyOffsetX, currentY, bodyWidth, height));
                        currentY += height;
                    }
                }

                var rawPageBitmap = new RenderTargetBitmap(rootWidth, Math.Max(1, rawPageHeight), 96, 96, PixelFormats.Pbgra32);
                rawPageBitmap.Render(pageVisual);
                var pageBitmap = TrimBottomBlankRows(rawPageBitmap);

                var outputPath = pageIndex == 1
                    ? filePath
                    : Path.Combine(directory ?? string.Empty, $"{filename}_{pageIndex:000}{extension}");

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(pageBitmap));
                using var stream = File.Create(outputPath);
                encoder.Save(stream);

                exportedAnyPage = true;
                if (globalBodyOffset <= previousGlobalOffset + 0.1)
                {
                    break;
                }

                pageIndex++;
            }

            if (!exportedAnyPage)
            {
                throw new InvalidOperationException("Görüntü alınacak içerik bulunamadı.");
            }
        }
        finally
        {
            RestoreDataGridVirtualizationSettings(dataGridVirtualizationSetting);
            RestoreExportScrollSettings(exportScrollSettings);
            scrollViewer.ScrollToVerticalOffset(originalOffset);
            rootElement.UpdateLayout();
            bodyViewport.UpdateLayout();
        }

        return Task.CompletedTask;
    }

    private static string GetUniqueSheetName(ISet<string> usedNames, string name)
    {
        var baseName = SanitizeSheetName(name);
        var candidate = baseName;
        var suffix = 2;

        while (!usedNames.Add(candidate))
        {
            var suffixText = $"_{suffix}";
            var maxBaseLength = Math.Max(1, 31 - suffixText.Length);
            candidate = $"{baseName[..Math.Min(baseName.Length, maxBaseLength)]}{suffixText}";
            suffix++;
        }

        return candidate;
    }

    private static string SanitizeSheetName(string? name)
    {
        var value = string.IsNullOrWhiteSpace(name) ? "Sheet" : name.Trim();
        foreach (var invalid in new[] { '[', ']', '*', '?', '/', '\\', ':' })
        {
            value = value.Replace(invalid, '-');
        }

        return value.Length <= 31 ? value : value[..31];
    }

    private static string NormalizeExcelColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var color = value.Trim();
        if (color.Length == 9 && color[0] == '#')
        {
            return $"#{color[3..]}";
        }

        return color.Length == 7 && color[0] == '#' ? color : string.Empty;
    }

    private static Task ExportStaticVisualAsync(FrameworkElement element, string filePath)
    {
        var width = (int)Math.Ceiling(Math.Max(1, element.ActualWidth));
        var height = (int)Math.Ceiling(Math.Max(1, element.ActualHeight));
        var renderTarget = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        renderTarget.Render(element);
        var finalBitmap = TrimBottomBlankRows(renderTarget);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(finalBitmap));
        using var stream = File.Create(filePath);
        encoder.Save(stream);
        return Task.CompletedTask;
    }

    private static bool TryExportActionListFromData(FrameworkElement rootElement, string filePath, CancellationToken cancellationToken)
    {
        if (rootElement.FindName("ActionListExportVisual") is not ListBox actionList)
        {
            return false;
        }

        var rows = BuildActionExportRows(actionList);
        if (rows.Count == 0)
        {
            return false;
        }

        rootElement.UpdateLayout();

        var width = (int)Math.Ceiling(Math.Max(1, rootElement.ActualWidth));
        var districtColumnWidth = 170;
        var ownerColumnWidth = 300;
        var workColumnWidth = width - districtColumnWidth - ownerColumnWidth;
        if (workColumnWidth < 220)
        {
            ownerColumnWidth = Math.Max(180, width - districtColumnWidth - 220);
            workColumnWidth = Math.Max(220, width - districtColumnWidth - ownerColumnWidth);
        }

        var listOffset = actionList.TransformToAncestor(rootElement).Transform(new Point(0, 0));
        var headerHeight = Math.Max(34, (int)Math.Round(listOffset.Y));
        var pixelsPerDip = VisualTreeHelper.GetDpi(rootElement).PixelsPerDip;

        const int maxPngHeight = 16000;
        var extension = Path.GetExtension(filePath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".png";
        }

        var directory = Path.GetDirectoryName(filePath);
        var filename = Path.GetFileNameWithoutExtension(filePath);
        var rowIndex = 0;
        var pageIndex = 1;
        var exportedAny = false;

        while (rowIndex < rows.Count)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var includeHeader = pageIndex == 1;
            var pageHeaderHeight = includeHeader ? headerHeight : 0;
            var pageRows = new List<(ActionExportRow Row, int Height)>();
            var usedHeight = pageHeaderHeight;

            while (rowIndex < rows.Count)
            {
                var rowHeight = MeasureActionRowHeight(rows[rowIndex], ownerColumnWidth, workColumnWidth, pixelsPerDip);
                if (usedHeight + rowHeight > maxPngHeight && pageRows.Count > 0)
                {
                    break;
                }

                if (usedHeight + rowHeight > maxPngHeight && pageRows.Count == 0)
                {
                    rowHeight = Math.Max(42, maxPngHeight - usedHeight);
                }

                pageRows.Add((rows[rowIndex], rowHeight));
                usedHeight += rowHeight;
                rowIndex++;
            }

            if (pageRows.Count == 0)
            {
                break;
            }

            var bitmap = DrawActionExportPage(
                width,
                pageHeaderHeight,
                districtColumnWidth,
                ownerColumnWidth,
                workColumnWidth,
                pageRows,
                pixelsPerDip,
                includeHeader);

            var outputPath = pageIndex == 1
                ? filePath
                : Path.Combine(directory ?? string.Empty, $"{filename}_{pageIndex:000}{extension}");

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = File.Create(outputPath);
            encoder.Save(stream);

            exportedAny = true;
            pageIndex++;
        }

        return exportedAny;
    }

    private static RenderTargetBitmap DrawActionExportPage(
        int width,
        int headerHeight,
        int districtColumnWidth,
        int ownerColumnWidth,
        int workColumnWidth,
        IReadOnlyList<(ActionExportRow Row, int Height)> pageRows,
        double pixelsPerDip,
        bool includeHeader)
    {
        var totalHeight = headerHeight + pageRows.Sum(item => item.Height);
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, totalHeight));

            if (includeHeader)
            {
                var headerBrush = ParseBrush("#FF121A22", Brushes.Black);
                var headerPen = new Pen(ParseBrush("#FF1D2B3A", Brushes.Black), 1);
                dc.DrawRectangle(headerBrush, headerPen, new Rect(0, 0, width, headerHeight));
                DrawWrappedText(dc, "İLÇE", new Rect(0, 0, districtColumnWidth, headerHeight), Brushes.White, 13, FontWeights.SemiBold, TextAlignment.Center, pixelsPerDip, 0);
                DrawWrappedText(dc, "ADA PARSEL YAPI SAHİBİ", new Rect(districtColumnWidth, 0, ownerColumnWidth, headerHeight), Brushes.White, 13, FontWeights.SemiBold, TextAlignment.Center, pixelsPerDip, 0);
                DrawWrappedText(dc, "YAPILACAK İŞ", new Rect(districtColumnWidth + ownerColumnWidth, 0, workColumnWidth, headerHeight), Brushes.White, 13, FontWeights.SemiBold, TextAlignment.Center, pixelsPerDip, 0);
            }

            var rowY = headerHeight;
            var rowStarts = new List<int>(pageRows.Count);
            for (var i = 0; i < pageRows.Count; i++)
            {
                rowStarts.Add(rowY);
                var (row, rowHeight) = pageRows[i];
                var pen = new Pen(row.BorderBrush, 1);
                dc.DrawRectangle(row.RowBackground, pen, new Rect(districtColumnWidth, rowY, ownerColumnWidth, rowHeight));
                dc.DrawRectangle(row.RowBackground, pen, new Rect(districtColumnWidth + ownerColumnWidth, rowY, workColumnWidth, rowHeight));

                DrawWrappedText(dc, row.OwnerText, new Rect(districtColumnWidth + 8, rowY + 4, ownerColumnWidth - 16, rowHeight - 8), row.TextBrush, 12, FontWeights.Medium, TextAlignment.Center, pixelsPerDip, 0);
                DrawWrappedText(dc, row.WorkText, new Rect(districtColumnWidth + ownerColumnWidth + 10, rowY + 4, workColumnWidth - 16, rowHeight - 8), row.TextBrush, 12, FontWeights.Medium, TextAlignment.Left, pixelsPerDip, 0);
                rowY += rowHeight;
            }

            var runStart = 0;
            while (runStart < pageRows.Count)
            {
                var runDistrict = pageRows[runStart].Row.District;
                var runEnd = runStart;
                var runHeight = 0;
                while (runEnd < pageRows.Count && string.Equals(pageRows[runEnd].Row.District, runDistrict, StringComparison.OrdinalIgnoreCase))
                {
                    runHeight += pageRows[runEnd].Height;
                    runEnd++;
                }

                var runRow = pageRows[runStart].Row;
                var runY = rowStarts[runStart];
                var pen = new Pen(runRow.BorderBrush, 1);
                dc.DrawRectangle(runRow.DistrictBackground, pen, new Rect(0, runY, districtColumnWidth, runHeight));
                DrawWrappedText(dc, runDistrict, new Rect(6, runY + 4, districtColumnWidth - 12, runHeight - 8), runRow.TextBrush, 12, FontWeights.SemiBold, TextAlignment.Center, pixelsPerDip, 0);
                runStart = runEnd;
            }
        }

        var bmp = new RenderTargetBitmap(width, Math.Max(1, totalHeight), 96, 96, PixelFormats.Pbgra32);
        bmp.Render(visual);
        return bmp;
    }

    private static int MeasureActionRowHeight(ActionExportRow row, int ownerColumnWidth, int workColumnWidth, double pixelsPerDip)
    {
        var ownerHeight = MeasureTextHeight(row.OwnerText, ownerColumnWidth - 16, 12, FontWeights.Medium, pixelsPerDip);
        var workHeight = MeasureTextHeight(row.WorkText, workColumnWidth - 16, 12, FontWeights.Medium, pixelsPerDip);
        var contentHeight = Math.Max(ownerHeight, workHeight);
        return Math.Max(42, (int)Math.Ceiling(contentHeight + 10));
    }

    private static double MeasureTextHeight(string text, double maxWidth, double fontSize, WpfFontWeight weight, double pixelsPerDip)
    {
        var formatted = new FormattedText(
            text ?? string.Empty,
            CultureInfo.GetCultureInfo("tr-TR"),
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, weight, FontStretches.Normal),
            fontSize,
            Brushes.Black,
            pixelsPerDip)
        {
            MaxTextWidth = Math.Max(1, maxWidth),
            TextAlignment = TextAlignment.Left
        };

        return formatted.Height;
    }

    private static void DrawWrappedText(
        DrawingContext dc,
        string text,
        Rect bounds,
        Brush brush,
        double fontSize,
        WpfFontWeight weight,
        TextAlignment alignment,
        double pixelsPerDip,
        int leftPadding)
    {
        var formatted = new FormattedText(
            text ?? string.Empty,
            CultureInfo.GetCultureInfo("tr-TR"),
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, weight, FontStretches.Normal),
            fontSize,
            brush,
            pixelsPerDip)
        {
            MaxTextWidth = Math.Max(1, bounds.Width - leftPadding),
            TextAlignment = alignment
        };

        var drawX = bounds.X + leftPadding;
        var drawY = bounds.Y + Math.Max(0, (bounds.Height - formatted.Height) / 2);
        dc.PushClip(new RectangleGeometry(bounds));
        dc.DrawText(formatted, new Point(drawX, drawY));
        dc.Pop();
    }

    private static List<ActionExportRow> BuildActionExportRows(ListBox actionList)
    {
        var result = new List<ActionExportRow>();
        var groups = actionList.ItemsSource as IEnumerable ?? actionList.Items;
        foreach (var group in groups)
        {
            if (group is null)
            {
                continue;
            }

            var district = GetStringProperty(group, "District");
            if (string.IsNullOrWhiteSpace(district))
            {
                continue;
            }

            var districtBackground = ParseBrush(GetPropertyValue(group, "DistrictBackground"), ParseBrush("#FFE7ECF3", Brushes.White));
            var districtForeground = ParseBrush(GetPropertyValue(group, "DistrictForeground"), ParseBrush("#FF223142", Brushes.Black));
            var districtBorderBrush = ParseBrush(GetPropertyValue(group, "DistrictBorderBrush"), ParseBrush("#FFC8D3E2", Brushes.Gray));

            var rows = GetPropertyValue(group, "Rows") as IEnumerable;
            if (rows is null)
            {
                continue;
            }

            foreach (var row in rows)
            {
                if (row is null)
                {
                    continue;
                }

                if (GetBoolProperty(row, "IsPlaceholder"))
                {
                    continue;
                }

                var ownerText = GetStringProperty(row, "OwnerParcelText");
                var workText = GetStringProperty(row, "WorkText");
                if (string.IsNullOrWhiteSpace(ownerText) && string.IsNullOrWhiteSpace(workText))
                {
                    continue;
                }

                var rowBackground = ParseBrush(GetPropertyValue(row, "RowBackground"), districtBackground);
                var rowBorderBrush = ParseBrush(GetPropertyValue(row, "RowBorderBrush"), districtBorderBrush);
                var rowForeground = ParseBrush(GetPropertyValue(row, "RowForeground"), districtForeground);

                result.Add(new ActionExportRow(
                    district,
                    ownerText,
                    workText,
                    districtBackground,
                    rowBackground,
                    rowBorderBrush,
                    rowForeground));
            }
        }

        return result;
    }

    private static object? GetPropertyValue(object source, string propertyName)
        => source.GetType().GetProperty(propertyName)?.GetValue(source);

    private static string GetStringProperty(object source, string propertyName)
        => GetPropertyValue(source, propertyName)?.ToString() ?? string.Empty;

    private static bool GetBoolProperty(object source, string propertyName)
        => GetPropertyValue(source, propertyName) is bool flag && flag;

    private static Brush ParseBrush(object? value, Brush fallback)
    {
        if (value is Brush brush)
        {
            return brush;
        }

        if (value is string text && !string.IsNullOrWhiteSpace(text))
        {
            try
            {
                var parsed = new BrushConverter().ConvertFromString(text);
                if (parsed is Brush converted)
                {
                    return converted;
                }
            }
            catch
            {
            }
        }

        return fallback;
    }

    private sealed record ActionExportRow(
        string District,
        string OwnerText,
        string WorkText,
        Brush DistrictBackground,
        Brush RowBackground,
        Brush BorderBrush,
        Brush TextBrush);

    private static BitmapSource CaptureViewportFrame(
        FrameworkElement viewportElement,
        ScrollViewer scrollViewer,
        FrameworkElement layoutRoot,
        double verticalOffset,
        int width,
        int height,
        out double actualOffset)
    {
        scrollViewer.ScrollToVerticalOffset(verticalOffset);
        layoutRoot.UpdateLayout();
        viewportElement.UpdateLayout();
        actualOffset = scrollViewer.VerticalOffset;
        var frame = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        frame.Render(viewportElement);
        return frame;
    }

    private static List<ScrollExportSetting> CaptureAndApplyExportScrollSettings(
        FrameworkElement rootElement,
        ScrollViewer scrollViewer,
        FrameworkElement bodyViewport,
        IEnumerable<FrameworkElement>? additionalElements)
    {
        var settings = new List<ScrollExportSetting>();
        var candidates = new List<FrameworkElement> { rootElement, bodyViewport, scrollViewer };
        var ownerItemsControl = FindAncestor<ItemsControl>(scrollViewer);
        if (ownerItemsControl is FrameworkElement ownerElement)
        {
            candidates.Add(ownerElement);
        }

        if (additionalElements is not null)
        {
            candidates.AddRange(additionalElements.Where(element => element is not null));
        }

        foreach (var candidate in candidates.Distinct())
        {
            settings.Add(new ScrollExportSetting(
                candidate,
                ScrollViewer.GetCanContentScroll(candidate),
                VirtualizingPanel.GetIsVirtualizing(candidate),
                VirtualizingPanel.GetScrollUnit(candidate),
                ScrollViewer.GetIsDeferredScrollingEnabled(candidate)));

            ScrollViewer.SetCanContentScroll(candidate, false);
            VirtualizingPanel.SetIsVirtualizing(candidate, false);
            VirtualizingPanel.SetScrollUnit(candidate, ScrollUnit.Pixel);
            ScrollViewer.SetIsDeferredScrollingEnabled(candidate, false);
        }

        return settings;
    }

    private static DataGridVirtualizationSetting? CaptureAndApplyDataGridVirtualizationSettings(DataGrid? dataGrid)
    {
        if (dataGrid is null)
        {
            return null;
        }

        var setting = new DataGridVirtualizationSetting(
            dataGrid,
            dataGrid.EnableRowVirtualization,
            dataGrid.EnableColumnVirtualization);

        dataGrid.EnableRowVirtualization = false;
        dataGrid.EnableColumnVirtualization = false;
        dataGrid.UpdateLayout();
        return setting;
    }

    private static void RestoreDataGridVirtualizationSettings(DataGridVirtualizationSetting? setting)
    {
        if (setting is null)
        {
            return;
        }

        setting.Grid.EnableRowVirtualization = setting.EnableRowVirtualization;
        setting.Grid.EnableColumnVirtualization = setting.EnableColumnVirtualization;
    }

    private static void RestoreExportScrollSettings(IEnumerable<ScrollExportSetting> settings)
    {
        foreach (var setting in settings)
        {
            ScrollViewer.SetCanContentScroll(setting.Element, setting.CanContentScroll);
            VirtualizingPanel.SetIsVirtualizing(setting.Element, setting.IsVirtualizing);
            VirtualizingPanel.SetScrollUnit(setting.Element, setting.ScrollUnit);
            ScrollViewer.SetIsDeferredScrollingEnabled(setting.Element, setting.IsDeferredScrollingEnabled);
        }
    }

    private sealed record ScrollExportSetting(
        FrameworkElement Element,
        bool CanContentScroll,
        bool IsVirtualizing,
        ScrollUnit ScrollUnit,
        bool IsDeferredScrollingEnabled);

    private sealed record DataGridVirtualizationSetting(
        DataGrid Grid,
        bool EnableRowVirtualization,
        bool EnableColumnVirtualization);

    private static int CalculateTotalBodyHeight(ScrollViewer scrollViewer, int bodyFrameHeight, DataGrid? dataGrid)
    {
        var extentHeight = Math.Ceiling(scrollViewer.ExtentHeight);
        var viewportHeight = Math.Ceiling(scrollViewer.ViewportHeight > 0 ? scrollViewer.ViewportHeight : bodyFrameHeight);

        if (dataGrid is null)
        {
            return Math.Max(bodyFrameHeight, (int)extentHeight);
        }

        if (extentHeight > viewportHeight + 1 && extentHeight > bodyFrameHeight)
        {
            return (int)extentHeight;
        }

        var estimatedRowHeight = EstimateAverageDataGridRowHeight(dataGrid);
        var estimatedHeight = Math.Ceiling(dataGrid.Items.Count * estimatedRowHeight);
        return Math.Max(bodyFrameHeight, (int)estimatedHeight);
    }

    private static double EstimateAverageDataGridRowHeight(DataGrid dataGrid)
    {
        var rows = new List<DataGridRow>();
        CollectDescendants(dataGrid, rows);

        var realizedHeights = rows
            .Select(row => row.ActualHeight)
            .Where(height => height > 1)
            .ToList();

        if (realizedHeights.Count > 0)
        {
            return Math.Max(20, realizedHeights.Average());
        }

        if (!double.IsNaN(dataGrid.RowHeight) && dataGrid.RowHeight > 1)
        {
            return dataGrid.RowHeight;
        }

        if (dataGrid.MinRowHeight > 1)
        {
            return dataGrid.MinRowHeight;
        }

        return 36;
    }

    private static BitmapSource TrimBottomBlankRows(BitmapSource source)
    {
        if (source.PixelWidth <= 0 || source.PixelHeight <= 1)
        {
            return source;
        }

        var converted = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        var width = converted.PixelWidth;
        var height = converted.PixelHeight;
        var stride = width * 4;
        var pixels = new byte[stride * height];
        converted.CopyPixels(pixels, stride, 0);

        static bool IsBackground(byte b, byte g, byte r, byte a)
        {
            const int tolerance = 8;
            return a >= 255 - tolerance &&
                   r >= 255 - tolerance &&
                   g >= 255 - tolerance &&
                   b >= 255 - tolerance;
        }

        var lastContentRow = height - 1;
        while (lastContentRow > 0)
        {
            var rowStart = lastContentRow * stride;
            var nonBackgroundPixels = 0;
            for (var x = 0; x < width; x++)
            {
                var idx = rowStart + (x * 4);
                if (!IsBackground(pixels[idx], pixels[idx + 1], pixels[idx + 2], pixels[idx + 3]))
                {
                    nonBackgroundPixels++;
                }
            }

            var nonBackgroundRatio = nonBackgroundPixels / (double)width;
            if (nonBackgroundRatio > 0.002)
            {
                break;
            }

            lastContentRow--;
        }

        var trimmedHeight = Math.Max(1, lastContentRow + 1);
        if (trimmedHeight >= height)
        {
            return converted;
        }

        return new CroppedBitmap(converted, new Int32Rect(0, 0, width, trimmedHeight));
    }

    private static T? FindDescendant<T>(DependencyObject? source) where T : DependencyObject
    {
        if (source is null)
        {
            return null;
        }

        var count = VisualTreeHelper.GetChildrenCount(source);
        for (var index = 0; index < count; index++)
        {
            var child = VisualTreeHelper.GetChild(source, index);
            if (child is T typed)
            {
                return typed;
            }

            var nested = FindDescendant<T>(child);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }

    private static DataGrid? ResolveExportDataGrid(FrameworkElement rootElement)
    {
        if (rootElement is DataGrid dataGrid)
        {
            return dataGrid;
        }

        return FindDescendant<DataGrid>(rootElement);
    }

    private static ScrollViewer? ResolveDataGridScrollViewer(DataGrid dataGrid)
    {
        dataGrid.ApplyTemplate();
        if (dataGrid.Template?.FindName("PART_ScrollViewer", dataGrid) is ScrollViewer partScrollViewer)
        {
            return partScrollViewer;
        }

        if (dataGrid.Template?.FindName("DG_ScrollViewer", dataGrid) is ScrollViewer legacyScrollViewer)
        {
            return legacyScrollViewer;
        }

        return FindBestScrollableViewer(dataGrid);
    }

    private static FrameworkElement ResolveDataGridBodyViewport(DataGrid dataGrid, ScrollViewer scrollViewer)
    {
        var rowsPresenter = FindDescendant<DataGridRowsPresenter>(scrollViewer);
        if (rowsPresenter is FrameworkElement rows && rows.ActualWidth > 1 && rows.ActualHeight > 1)
        {
            return rows;
        }

        return ResolveScrollableViewport(scrollViewer) ?? scrollViewer;
    }

    private static ScrollViewer? ResolvePrimaryScrollViewer(FrameworkElement rootElement)
    {
        if (rootElement.FindName("ActionListExportVisual") is ListBox actionList)
        {
            var fromActionList = ResolveListBoxScrollViewer(actionList);
            if (fromActionList is not null)
            {
                return fromActionList;
            }
        }

        return FindBestScrollableViewer(rootElement);
    }

    private static FrameworkElement? ResolveScrollableViewport(ScrollViewer scrollViewer)
    {
        var presenters = new List<ScrollContentPresenter>();
        CollectDescendants(scrollViewer, presenters);
        if (presenters.Count == 0)
        {
            return null;
        }

        return presenters
            .Where(presenter => presenter.ActualWidth > 1 && presenter.ActualHeight > 1)
            .OrderByDescending(presenter => presenter.ActualWidth * presenter.ActualHeight)
            .FirstOrDefault();
    }

    private static ScrollViewer? FindBestScrollableViewer(DependencyObject root)
    {
        var all = new List<ScrollViewer>();
        CollectDescendants(root, all);
        if (all.Count == 0)
        {
            return null;
        }

        var scrollable = all
            .Where(viewer => viewer.ViewportHeight > 0 && viewer.ExtentHeight - viewer.ViewportHeight > 0.5)
            .OrderByDescending(viewer => viewer.ExtentHeight - viewer.ViewportHeight)
            .ThenByDescending(viewer => viewer.ExtentHeight)
            .FirstOrDefault();

        return scrollable
            ?? all
                .Where(viewer => viewer.ViewportHeight > 0)
                .OrderByDescending(viewer => viewer.ExtentHeight)
                .FirstOrDefault()
            ?? all.FirstOrDefault();
    }

    private static ScrollViewer? ResolveListBoxScrollViewer(ListBox listBox)
    {
        listBox.ApplyTemplate();
        if (listBox.Template?.FindName("PART_ScrollViewer", listBox) is ScrollViewer partScrollViewer)
        {
            return partScrollViewer;
        }

        return FindBestScrollableViewer(listBox);
    }

    private static void CollectDescendants(DependencyObject parent, List<ScrollViewer> viewers)
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var index = 0; index < count; index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is ScrollViewer scrollViewer)
            {
                viewers.Add(scrollViewer);
            }

            CollectDescendants(child, viewers);
        }
    }

    private static void CollectDescendants(DependencyObject parent, List<ScrollContentPresenter> presenters)
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var index = 0; index < count; index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is ScrollContentPresenter presenter)
            {
                presenters.Add(presenter);
            }

            CollectDescendants(child, presenters);
        }
    }

    private static void CollectDescendants(DependencyObject parent, List<DataGridRow> rows)
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var index = 0; index < count; index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is DataGridRow row)
            {
                rows.Add(row);
            }

            CollectDescendants(child, rows);
        }
    }

    private static T? FindAncestor<T>(DependencyObject? child) where T : DependencyObject
    {
        var current = child;
        while (current is not null)
        {
            if (current is T typed)
            {
                return typed;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static string Normalize(string text)
        => text.Trim().ToLowerInvariant().Replace(" ", string.Empty).Replace("ı", "i");

    private static string ReadCell(IXLRow row, IReadOnlyDictionary<string, int> map, string key)
    {
        if (!map.TryGetValue(key, out var index))
        {
            return string.Empty;
        }

        return row.Cell(index).GetString().Trim();
    }

    private static TaskBoardType ParseBoard(string value)
        => value.ToLowerInvariant().Contains("acil") ? TaskBoardType.Acil : TaskBoardType.Genel;
}

