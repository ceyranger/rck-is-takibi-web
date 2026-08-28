using System.Text.Json;
using System.Text.Json.Serialization;

namespace RizaCanKilicIsTakibi.Models;

public sealed class WebViewSnapshotEnvelope
{
    public const string ExpectedKind = "web-view";

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = ExpectedKind;

    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = 1;

    [JsonPropertyName("exportedAt")]
    public DateTime ExportedAt { get; set; }

    [JsonPropertyName("appVersion")]
    public string AppVersion { get; set; } = string.Empty;

    [JsonPropertyName("checksum")]
    public string Checksum { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public JsonElement Data { get; set; }

    [JsonPropertyName("derived")]
    public WebViewSnapshotDerived Derived { get; set; } = new();
}

public sealed class WebViewSnapshotDerived
{
    [JsonPropertyName("tumEksikler")]
    public List<WebViewTumEksiklerGroupDto> TumEksikler { get; set; } = [];

    [JsonPropertyName("projeOnayItems")]
    public List<WebViewProjeOnayGroupDto> ProjeOnayItems { get; set; } = [];

    [JsonPropertyName("personnelGorevItems")]
    public List<WebViewPersonnelGorevRowDto> PersonnelGorevItems { get; set; } = [];
}

public sealed class WebViewTumEksiklerGroupDto
{
    [JsonPropertyName("headerText")]
    public string HeaderText { get; set; } = string.Empty;

    [JsonPropertyName("detailText")]
    public string DetailText { get; set; } = string.Empty;

    [JsonPropertyName("adaParsel")]
    public string AdaParsel { get; set; } = string.Empty;

    [JsonPropertyName("yapiSahibi")]
    public string YapiSahibi { get; set; } = string.Empty;

    [JsonPropertyName("matchStatus")]
    public string MatchStatus { get; set; } = string.Empty;

    [JsonPropertyName("eksikCount")]
    public int EksikCount { get; set; }

    [JsonPropertyName("criticalCount")]
    public int CriticalCount { get; set; }

    [JsonPropertyName("items")]
    public List<WebViewTumEksiklerItemDto> Items { get; set; } = [];
}

public sealed class WebViewTumEksiklerItemDto
{
    [JsonPropertyName("sourceModule")]
    public string SourceModule { get; set; } = string.Empty;

    [JsonPropertyName("fieldLabel")]
    public string FieldLabel { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("currentValue")]
    public string CurrentValue { get; set; } = string.Empty;

    [JsonPropertyName("noteText")]
    public string NoteText { get; set; } = string.Empty;

    [JsonPropertyName("sourceContext")]
    public string SourceContext { get; set; } = string.Empty;

    [JsonPropertyName("assignedPersonnelBadge")]
    public string AssignedPersonnelBadge { get; set; } = string.Empty;

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = string.Empty;

    [JsonPropertyName("severityLabel")]
    public string SeverityLabel { get; set; } = string.Empty;

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

public sealed class WebViewProjeOnayGroupDto
{
    [JsonPropertyName("titleText")]
    public string TitleText { get; set; } = string.Empty;

    [JsonPropertyName("adaParsel")]
    public string AdaParsel { get; set; } = string.Empty;

    [JsonPropertyName("yapiSahibi")]
    public string YapiSahibi { get; set; } = string.Empty;

    [JsonPropertyName("isOverdue")]
    public bool IsOverdue { get; set; }

    [JsonPropertyName("events")]
    public List<WebViewProjeOnayEventDto> Events { get; set; } = [];
}

public sealed class WebViewProjeOnayEventDto
{
    [JsonPropertyName("statusLabel")]
    public string StatusLabel { get; set; } = string.Empty;

    [JsonPropertyName("filterKey")]
    public string FilterKey { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("eventDateText")]
    public string EventDateText { get; set; } = string.Empty;

    [JsonPropertyName("daysElapsedText")]
    public string DaysElapsedText { get; set; } = string.Empty;

    [JsonPropertyName("isOverdue")]
    public bool IsOverdue { get; set; }

    [JsonPropertyName("categoryColor")]
    public string CategoryColor { get; set; } = string.Empty;
}

public sealed class WebViewPersonnelGorevRowDto
{
    [JsonPropertyName("personnelName")]
    public string PersonnelName { get; set; } = string.Empty;

    [JsonPropertyName("moduleLabel")]
    public string ModuleLabel { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("fieldLabel")]
    public string FieldLabel { get; set; } = string.Empty;

    [JsonPropertyName("projectIdentity")]
    public string ProjectIdentity { get; set; } = string.Empty;

    [JsonPropertyName("priorityLabel")]
    public string PriorityLabel { get; set; } = string.Empty;

    [JsonPropertyName("statusLabel")]
    public string StatusLabel { get; set; } = string.Empty;

    [JsonPropertyName("assignedAtText")]
    public string AssignedAtText { get; set; } = string.Empty;

    [JsonPropertyName("isOpen")]
    public bool IsOpen { get; set; }
}

public sealed class WebViewSnapshotExportRequest
{
    public required IEnumerable<TaskItem> Tasks { get; init; }
    public IEnumerable<ActionEntry>? ActionEntries { get; init; }
    public IEnumerable<MissingProjectEntry>? MissingProjectEntries { get; init; }
    public IEnumerable<MissingProjectCellState>? MissingProjectCellStates { get; init; }
    public IEnumerable<KarotEntry>? KarotEntries { get; init; }
    public IEnumerable<KarotCellState>? KarotCellStates { get; init; }
    public IEnumerable<TadilatEntry>? TadilatEntries { get; init; }
    public IEnumerable<YibfAnaBilgiEntry>? YibfAnaBilgiEntries { get; init; }
    public IEnumerable<YibfAnaBilgiEvent>? YibfAnaBilgiEvents { get; init; }
    public IEnumerable<YibfIsTakibiEntry>? YibfIsTakibiEntries { get; init; }
    public IEnumerable<YibfCellState>? YibfCellStates { get; init; }
    public IEnumerable<TadilatCellState>? TadilatCellStates { get; init; }
    public IEnumerable<QuickTaskTemplate>? QuickTaskTemplates { get; init; }
    public IEnumerable<ProjectCatalogEntry>? ProjectCatalogEntries { get; init; }
    public IEnumerable<Personnel>? Personnel { get; init; }
    public IEnumerable<PersonnelAssignment>? PersonnelAssignments { get; init; }
    public required WebViewSnapshotDerived Derived { get; init; }
}

public sealed class WebViewSnapshotExportResult
{
    public required string FilePath { get; init; }
    public required DateTime ExportedAt { get; init; }
    public required long FileSizeBytes { get; init; }
}
