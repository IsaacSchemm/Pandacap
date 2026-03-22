Imports Newtonsoft.Json.Linq

Public Interface IJsonLdExpansionService
    Function Expand(jObject As JObject) As JToken
End Interface
