using System.ComponentModel.DataAnnotations;

namespace Monody.Data;

public sealed class DataOptions
{
    /// <summary>
    /// SQLite connection string. The file must live on a mounted volume, or memories and
    /// reminders are lost whenever the container is replaced.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string ConnectionString { get; set; } = "Data Source=monody.db";
}
