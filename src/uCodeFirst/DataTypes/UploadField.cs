using uCodeFirst.Attributes;
using uCodeFirst.DataTypes.Bases;

namespace uCodeFirst.DataTypes;

/// <summary>Applies Umbraco's "Upload Field" editor to a <see cref="string"/> property for uploading a single file.</summary>
[DataType("Upload Field", Guid = "9f91b775-63d7-4dbe-896c-37299a1806ae")]
public sealed class UploadField : UploadFieldDataType { }
