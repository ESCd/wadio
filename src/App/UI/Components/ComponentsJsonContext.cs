using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Wadio.App.UI.Components.Forms;

namespace Wadio.App.UI.Components;

[JsonSerializable( typeof( ImmutableArray<FilterOption> ) )]
[JsonSourceGenerationOptions( DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, WriteIndented = false )]
public sealed partial class ComponentsJsonContext : JsonSerializerContext;