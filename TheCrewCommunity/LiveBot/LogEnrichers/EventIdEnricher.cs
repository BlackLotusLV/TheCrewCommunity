using Serilog.Core;
using Serilog.Events;

namespace TheCrewCommunity.LiveBot.LogEnrichers;

public class EventIdEnricher : ILogEventEnricher
{
    private const int MaxEventIdLength = 3;
    private const int MaxEventNameLength = 12;
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (!logEvent.Properties.TryGetValue("EventId", out LogEventPropertyValue? eventIdValue) || eventIdValue is not StructureValue eventIdStructure)
        {
            var property = new LogEventProperty("FormattedEventId", new ScalarValue($"{"",MaxEventIdLength}|{"",MaxEventNameLength}"));
            logEvent.AddPropertyIfAbsent(property);
            return;
        }
        LogEventProperty? idProperty = eventIdStructure.Properties.FirstOrDefault(p => p.Name == "Id");
        string id = idProperty != null ? idProperty.Value.ToString() : "";
        LogEventProperty? nameProperty = eventIdStructure.Properties.FirstOrDefault(p => p.Name == "Name");
        string name = nameProperty != null ? nameProperty.Value.ToString().Trim('"') : "";
        string formattedId = id.Length > MaxEventIdLength ? id[..MaxEventIdLength] : id.PadLeft(MaxEventIdLength, ' ');
        string formattedName = name.Length > MaxEventNameLength ? name[..MaxEventNameLength] : name.PadRight(MaxEventNameLength);
        var formattedEventId = $"{formattedId}|{formattedName}";
        var eventIdProperty = new LogEventProperty("FormattedEventId", new ScalarValue(formattedEventId));
        logEvent.AddPropertyIfAbsent(eventIdProperty);
    }
}