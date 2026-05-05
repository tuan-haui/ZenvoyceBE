namespace Zenvoyce.Domain.Entities;

/// <summary>Bảng nối Nhóm quyền — Menu (N-N).</summary>
public class Sysgroupmenu
{
    public Guid Id { get; set; }
    public Guid Quyenid { get; set; }
    public Guid Menuid { get; set; }
    public DateTime? CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
}
