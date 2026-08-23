using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Nedis.Server.Common;

namespace Nedis.Server.Modules.Chat.Features.CreateChannel;

public record CreateChannelRequest(
    [property: JsonConverter(typeof(TrimmingStringConverter))]
    [Required, StringLength(64, MinimumLength = 2)]
    string Name
);