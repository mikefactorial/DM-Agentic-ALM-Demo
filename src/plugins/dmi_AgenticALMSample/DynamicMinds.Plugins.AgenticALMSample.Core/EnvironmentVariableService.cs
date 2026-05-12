using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace DynamicMinds.Plugins.AgenticALMSample.Core;

public static class EnvironmentVariableService
{
    public static string GetRequiredValue(IOrganizationService service, string schemaName)
    {
        var value = GetValue(service, schemaName);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidPluginExecutionException($"Environment variable '{schemaName}' is not configured.");
        }

        return value;
    }

    public static string GetValue(IOrganizationService service, string schemaName)
    {
        if (service == null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        if (string.IsNullOrWhiteSpace(schemaName))
        {
            throw new ArgumentException("Schema name is required.", nameof(schemaName));
        }

        var query = new QueryExpression("environmentvariabledefinition")
        {
            ColumnSet = new ColumnSet("schemaname", "defaultvalue"),
            TopCount = 1
        };

        query.Criteria.AddCondition("schemaname", ConditionOperator.Equal, schemaName);
        var valueLink = query.AddLink("environmentvariablevalue", "environmentvariabledefinitionid", "environmentvariabledefinitionid", JoinOperator.LeftOuter);
        valueLink.Columns = new ColumnSet("value");
        valueLink.EntityAlias = "value";

        var definition = service.RetrieveMultiple(query).Entities.Count > 0
            ? service.RetrieveMultiple(query).Entities[0]
            : null;

        if (definition == null)
        {
            return null;
        }

        if (definition.Attributes.TryGetValue("value.value", out var aliased) && aliased is AliasedValue aliasedValue)
        {
            return aliasedValue.Value?.ToString();
        }

        return definition.GetAttributeValue<string>("defaultvalue");
    }
}
