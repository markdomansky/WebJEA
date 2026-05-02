Imports Newtonsoft.Json

Public Class CommandService
    Private dlog As NLog.Logger = NLog.LogManager.GetCurrentClassLogger()
    Private _config As IConfigProvider
    Private _auth As IAuthorizationService = New AuthorizationService
    Private _scriptCmds As New Dictionary(Of String, PSCmd)
    Private _onloadCmds As New Dictionary(Of String, PSCmd)

    Public ReadOnly Property Config As IConfigProvider
        Get
            Return _config
        End Get
    End Property

    Public ReadOnly Property Auth As IAuthorizationService
        Get
            Return _auth
        End Get
    End Property

    Public Sub LoadConfig(configFilePath As String, grpfinder As IGroupResolver)
        Dim configstr As String = GetFileContent(configFilePath)
        Try
            _config = JsonConvert.DeserializeObject(Of Config)(configstr)
        Catch ex As Exception
            Throw New Exception("Could not read config file", ex)
        End Try

        Try
            _auth.InitGroups(_config, grpfinder)
        Catch ex As Exception
            Throw New Exception("Could not initialize groups", ex)
        End Try
    End Sub

    Public Function ResolveCommandId(uinfo As UserInfo, requestedId As String) As String
        If _auth.IsCommandAvailable(uinfo, requestedId) Then
            Return requestedId
        End If
        dlog.Warn("User " & uinfo.UserName & " requested page they don't have access to " & requestedId & ". Showing dashboard.")
        Return ""
    End Function

    Public Function GetCommand(uinfo As UserInfo, cmdid As String) As ConfigCmd
        Return _auth.GetCommand(uinfo, cmdid)
    End Function

    Public Function GetScriptCmd(cmdid As String) As PSCmd
        If _scriptCmds.ContainsKey(cmdid) Then
            Return _scriptCmds(cmdid)
        End If

        Dim configCmd As ConfigCmd = Nothing
        For Each cmd As ConfigCmd In _config.Commands
            If cmd.ID = cmdid Then
                configCmd = cmd
                Exit For
            End If
        Next

        If configCmd Is Nothing OrElse String.IsNullOrEmpty(configCmd.Script) Then
            Return Nothing
        End If

        Dim pscmd As New PSCmd()
        pscmd.Script = configCmd.Script
        pscmd.LogParameters = configCmd.LogParameters
        pscmd.Init(_config.BasePath, _config.LogParameters)

        If String.IsNullOrEmpty(configCmd.Synopsis) Then
            configCmd.Synopsis = pscmd.ParsedSynopsis
        End If
        If String.IsNullOrEmpty(configCmd.Description) Then
            configCmd.Description = pscmd.ParsedDescription
        End If

        _scriptCmds(cmdid) = pscmd
        Return pscmd
    End Function

    Public Function GetOnloadCmd(cmdid As String) As PSCmd
        If _onloadCmds.ContainsKey(cmdid) Then
            Return _onloadCmds(cmdid)
        End If

        Dim configCmd As ConfigCmd = Nothing
        For Each cmd As ConfigCmd In _config.Commands
            If cmd.ID = cmdid Then
                configCmd = cmd
                Exit For
            End If
        Next

        If configCmd Is Nothing OrElse String.IsNullOrEmpty(configCmd.OnloadScript) Then
            Return Nothing
        End If

        Dim pscmd As New PSCmd()
        pscmd.Script = configCmd.OnloadScript
        pscmd.LogParameters = configCmd.LogParameters
        pscmd.Init(_config.BasePath, _config.LogParameters)

        _onloadCmds(cmdid) = pscmd
        Return pscmd
    End Function

End Class
