using Mms.Domain.Common;
using Mms.Domain.Enums;

namespace Mms.Domain.Entities;

public class Template : BaseEntity
{
    public Guid? MeetingId { get; set; }        // null = global library template
    public Meeting? Meeting { get; set; }
    public string Name { get; set; } = "";      // tên gợi nhớ do admin đặt
    public TemplateType TemplateType { get; set; }
    public string Language { get; set; } = "VN"; // VN / EN / DUAL
    public int Version { get; set; } = 1;
    public string? FilePath { get; set; }        // DOCX gốc (backup)
    public long? FileSize { get; set; }          // bytes
    public string? FieldsConfig { get; set; }    // legacy — token scan JSON
    public string? HtmlContent { get; set; }     // HTML from WYSIWYG editor
    public string? SelectedTokens { get; set; }  // JSON array: ["[1]","[2]","[5]"]
    public bool UseSignatureAndSeal { get; set; } // chèn chữ ký + con dấu
    public bool IsFinalized { get; set; }
    
    // Page margins (in cm)
    public float MarginTop { get; set; } = 2.0f;
    public float MarginBottom { get; set; } = 2.0f;
    public float MarginLeft { get; set; } = 3.0f;
    public float MarginRight { get; set; } = 2.0f;

    public Guid? UploadedBy { get; set; }
    public DateTime? UploadedAt { get; set; }
}
