using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.Windows;
using System.Text.Json;

namespace RizaCanKilicIsTakibi.Services;

public sealed class ClipboardService : IClipboardService
{
    private const string CellPayloadFormat = "RizaCanKilicIsTakibi.CellClipboardPayload";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public bool ContainsText()
    {
        try
        {
            return Clipboard.ContainsText();
        }
        catch
        {
            return false;
        }
    }

    public bool TryGetText(out string? text)
    {
        text = null;

        try
        {
            if (!Clipboard.ContainsText())
            {
                return false;
            }

            text = Clipboard.GetText();
            return true;
        }
        catch
        {
            text = null;
            return false;
        }
    }

    public bool TrySetText(string? text)
    {
        try
        {
            Clipboard.SetText(text ?? string.Empty);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool TryGetCellPayload(out CellClipboardPayload? payload)
    {
        payload = null;

        try
        {
            if (!Clipboard.ContainsData(CellPayloadFormat))
            {
                return false;
            }

            var raw = Clipboard.GetData(CellPayloadFormat) as string;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            payload = DeserializePayload(raw);
            return payload is not null;
        }
        catch
        {
            payload = null;
            return false;
        }
    }

    public bool TrySetCellPayload(CellClipboardPayload payload)
    {
        try
        {
            var normalizedPayload = NormalizePayload(payload);
            var dataObject = new DataObject();
            dataObject.SetData(CellPayloadFormat, SerializePayload(normalizedPayload));
            dataObject.SetText(normalizedPayload.Text);
            Clipboard.SetDataObject(dataObject, true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    internal static string SerializePayload(CellClipboardPayload payload)
        => JsonSerializer.Serialize(NormalizePayload(payload), JsonOptions);

    internal static CellClipboardPayload? DeserializePayload(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<CellClipboardPayload>(raw, JsonOptions);
            return payload is null ? null : NormalizePayload(payload);
        }
        catch
        {
            return null;
        }
    }

    private static CellClipboardPayload NormalizePayload(CellClipboardPayload? payload)
        => new()
        {
            Text = payload?.Text ?? string.Empty,
            BackgroundColor = payload?.BackgroundColor ?? string.Empty,
            NoteText = payload?.NoteText ?? string.Empty
        };
}
