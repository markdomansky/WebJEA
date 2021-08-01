Imports System.Web.SessionState
Imports System.Web
Imports System.Diagnostics

Public Class Global_asax
    Inherits System.Web.HttpApplication

    Sub Application_Start(ByVal sender As Object, ByVal e As EventArgs)
        ' Fires when the application is started

        ScriptManager.ScriptResourceMapping.AddDefinition("jquery", New ScriptResourceDefinition With {
            .Path = "~/scripts/jquery-3.6.0.min.js",
            .DebugPath = "~/scripts/jquery-3.6.0.js"
        })
    End Sub

    Sub Session_Start(ByVal sender As Object, ByVal e As EventArgs)
        ' Fires when the session is started
        Session("init") = 0 'forces session to start, so session id is maintained.
        Session("sessionid") = Session.SessionID

    End Sub

    Sub Application_BeginRequest(ByVal sender As Object, ByVal e As EventArgs)
        ' Fires at the beginning of each request
    End Sub

    Sub Application_AuthenticateRequest(ByVal sender As Object, ByVal e As EventArgs)
        ' Fires upon attempting to authenticate the use
    End Sub

    Sub Application_Error(ByVal sender As Object, ByVal e As EventArgs)
        ' Fires when an error occurs
        Dim ErrorDescription As String = Server.GetLastError.ToString

        'Creation of event log if it does not exist  
        Dim EventLogName As String = "Application"
        'If (Not EventLog.SourceExists(EventLogName)) Then
        '    EventLog.CreateEventSource(EventLogName, EventLogName)
        'End If

        ' Inserting into event log
        Dim Log As New EventLog()
        Log.Source = EventLogName
        Log.WriteEntry(ErrorDescription, EventLogEntryType.Error)
    End Sub

    Sub Session_End(ByVal sender As Object, ByVal e As EventArgs)
        ' Fires when the session ends
    End Sub

    Sub Application_End(ByVal sender As Object, ByVal e As EventArgs)
        ' Fires when the application ends
    End Sub

End Class