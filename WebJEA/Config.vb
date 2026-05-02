Public Class Config
    Implements IConfigProvider
    Private dlog As NLog.Logger = NLog.LogManager.GetCurrentClassLogger()

    Public Property Title As String Implements IConfigProvider.Title
    Public Property LogParameters As Boolean = True Implements IConfigProvider.LogParameters
    Public Property Commands As List(Of ConfigCmd) Implements IConfigProvider.Commands
    Public Property BasePath As String Implements IConfigProvider.BasePath
    Public Property SendTelemetry As Boolean = True Implements IConfigProvider.SendTelemetry
    Public Property HtmlLanguage As String = "en-US" Implements IConfigProvider.HtmlLanguage
    Public Property DashboardHtml As String Implements IConfigProvider.DashboardHtml
    Public Property ShowVerbose As Boolean = True Implements IConfigProvider.ShowVerbose
    Public Property PermittedGroups As List(Of String) = New List(Of String)() Implements IConfigProvider.PermittedGroups

    Public Sub New()

    End Sub

End Class
