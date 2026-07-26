using uCodeFirst.Attributes;
using uCodeFirst.DataTypes;
using Basicv17.Models.Pages;

namespace Basicv17.Models.DataTypes;

// Restricts the content picker to News Article nodes only, demonstrating AllowedContentTypes
// (roadmap #2) by subclassing ContentPickerDataType (see PrioritySlider.cs for the same
// subclassing pattern, needed since DataTypeBase config properties are get-only).
[DataType("Related Article Picker", Guid = "c1d2e3f4-5a6b-4c7d-8e9f-0a1b2c3d4e5f")]
public sealed class RelatedArticlePicker : ContentPickerDataType
{
    public override Type[] AllowedContentTypes { get; } = [typeof(NewsArticle)];
}
