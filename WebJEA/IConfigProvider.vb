Public Interface IConfigProvider

    Property Title As String
    Property LogParameters As Boolean
    ReadOnly Property Commands As List(Of ConfigCmd)
    Property BasePath As String
    Property SendTelemetry As Boolean
    Property HtmlLanguage As String
    Property DashboardHtml As String
    Property ShowVerbose As Boolean
    Property PermittedGroups As List(Of String)

End Interface
