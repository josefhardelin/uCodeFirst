using uCodeFirst.Attributes;
using uCodeFirst.DataTypes.Bases;

namespace uCodeFirst.DataTypes;

/// <summary>Applies Umbraco's "True/False" toggle editor to a <see cref="bool"/> property.</summary>
[DataType("True/False", Guid = "dd9a3c19-701c-4fda-bb7a-1c35d2d04acb")]
public sealed class TrueFalse : TrueFalseDataType { }
