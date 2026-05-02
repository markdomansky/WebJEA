Public Interface ITelemetryService

    Sub Add(key As String, value As Object)
    Sub Clear(key As String)
    Sub Remove(key As String)
    Sub SendTelemetry()
    Sub AddIDs(DomainSid As String, DomainDNSRoot As String, ScriptID As String, UserID As String, Optional Permitted As Boolean = True)
    Sub AddIsOnload(state As Boolean)
    Sub AddRuntime(SecondsRuntime As Single)

End Interface
