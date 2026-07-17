using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Wadio.Platform.Web.UI.Components.Forms;

namespace Wadio.Platform.Web.UI.Components;

[JsonSerializable( typeof( ImmutableArray<FilterOption> ) )]
[JsonSourceGenerationOptions( DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, WriteIndented = false )]
public sealed partial class ComponentsJsonContext : JsonSerializerContext;