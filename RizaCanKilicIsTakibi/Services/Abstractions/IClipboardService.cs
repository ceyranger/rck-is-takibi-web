using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface IClipboardService
{
    bool ContainsText();
    bool TryGetText(out string? text);
    bool TrySetText(string? text);
    bool TryGetCellPayload(out CellClipboardPayload? payload);
    bool TrySetCellPayload(CellClipboardPayload payload);
}
