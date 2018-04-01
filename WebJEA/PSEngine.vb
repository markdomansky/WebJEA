Imports System.Management.Automation
Public Class PSEngine
    Private ps As PowerShell

    Public LogParameters As Boolean = True
    Private prvScript As String
    Private prvParams As New Dictionary(Of String, Object)
    Private prvOutput As String
    Private prvRuntime As Single = 0
    Private prvOutputQ As New Queue(Of OutputData)

    Public Enum OutputType
        Unknown
        Output
        Info
        Warn
        Err
        Verbose
        Debug
    End Enum
    Public Structure OutputData
        Public OutputType As OutputType
        Public Content As String
    End Structure

    Public Sub New()
    End Sub

    Public ReadOnly Property Runtime As Single
        Get
            Return prvRuntime
        End Get
    End Property

    Public Property Script As String
        Get
            Return prvScript
        End Get
        Set(value As String)
            prvScript = value
        End Set
    End Property

    Public Property Parameters As Dictionary(Of String, Object)
        Get
            Return prvParams
        End Get
        Set(value As Dictionary(Of String, Object))
            prvParams = value
        End Set

    End Property

    Public Function getOutputData() As Queue(Of OutputData)
        Return prvOutputQ
    End Function
    'Public ReadOnly Property Output As Queue(Of OutputData)
    '    Get
    '        Return prvOutputQ
    '    End Get
    'End Property

    Public Sub Run()

        If Not IO.File.Exists(prvScript) Then
            'file didn't exist
            Dim item As OutputData
            item.OutputType = OutputType.Err
            item.Content = "File Does not Exist"
            prvOutputQ.Enqueue(item)
            Return
        End If

        ps = PowerShell.Create()

        'add stream handlers
        AddHandler ps.Streams.Information.DataAdded, AddressOf CacheOutputStreams
        AddHandler ps.Streams.Warning.DataAdded, AddressOf CacheOutputStreams
        AddHandler ps.Streams.Error.DataAdded, AddressOf CacheOutputStreams
        AddHandler ps.Streams.Verbose.DataAdded, AddressOf CacheOutputStreams
        AddHandler ps.Streams.Debug.DataAdded, AddressOf CacheOutputStreams

        prvOutput = ""
        'read the runspace in powershell
        '''''ps.Commands.AddScript(GetFileContent(prvScript))
        ps.Commands.AddCommand(prvScript)
        'ps.Runspace.SessionStateProxy.SetVariable("name", "svchost")

        If Not (prvParams Is Nothing) Then
            For Each pair In prvParams
                ps.Commands.AddParameter(pair.Key, pair.Value)
            Next
        End If

        ps.Commands.AddCommand("out-string")

        LogCommandExecuted()
        Dim timestart As DateTime = Now
        Dim results As New ObjectModel.Collection(Of PSObject)
        Try
            'record the timespan
            results = ps.Invoke()
        Catch ex As System.Management.Automation.ParameterBindingException
            Dim item As New OutputData
            item.Content = ex.ErrorRecord.FullyQualifiedErrorId & ": " & ex.ErrorRecord.Exception.Message & " - " & ex.ErrorRecord.ScriptStackTrace
            'item.Content = sender(e.Index).exception.message & " " & sender(e.Index).scriptstacktrace & vbCrLf & "    & " & sender(e.Index).categoryinfo.activity
            item.OutputType = OutputType.Err
            prvOutputQ.Enqueue(item)
        Catch ex As Exception
            'TODO: improve error output, try to emulate actual PS output
            Dim item As New OutputData
            item.Content = ex.Message
            'item.Content = sender(e.Index).exception.message & " " & sender(e.Index).scriptstacktrace & vbCrLf & "    & " & sender(e.Index).categoryinfo.activity
            item.OutputType = OutputType.Err
            dlog.Error("Error when executing script: " + ex.Message)
            prvOutputQ.Enqueue(item)
        End Try
        Dim timespan As TimeSpan = Now - timestart
        prvRuntime = timespan.TotalSeconds
        dlog.Info("Executed|" & prvScript & "|" & prvRuntime)

        If (results.Count > 0) Then
            For Each resobj As PSObject In results
                Dim item As OutputData
                item.OutputType = OutputType.Output
                item.Content = resobj.ToString()
                If item.Content <> "" Then
                    prvOutputQ.Enqueue(item)
                End If

            Next

            'prvOutput += GetStreamDetails(ps.Streams.Progress)
            'str += ps.Streams.information.readall()
            'str += ps.Streams.Verbose.ReadAll()
            'str += ps.Streams.[Error].ReadAll()
        End If

        ps = Nothing
    End Sub
    'get writehost output function
    'clear writehost buffer
    Private Sub CacheOutputStreams(sender As Object, e As DataAddedEventArgs)
        'dlog.Debug("CacheOutputStreams: " & sender & " - " & e)
        'works with information only right now
        Dim item As New OutputData
        Select Case sender.GetType().ToString() 'info, output, host
            Case "System.Management.Automation.PSDataCollection`1[System.Management.Automation.InformationRecord]"
                item.Content = sender(e.Index).ToString()
                item.OutputType = OutputType.Info
            Case "System.Management.Automation.PSDataCollection`1[System.Management.Automation.WarningRecord]"
                item.Content = sender(e.Index).ToString()
                item.OutputType = OutputType.Warn
            Case "System.Management.Automation.PSDataCollection`1[System.Management.Automation.ErrorRecord]"
                'Dim errobj As System.Management.Automation.ErrorRecord = sender(e.Index)
                item.Content = sender(e.Index).exception.message & vbCrLf & sender(e.Index).scriptstacktrace & vbCrLf & "    + CategoryInfo          : " & sender(e.Index).categoryinfo.ToString
                item.OutputType = OutputType.Err
            Case "System.Management.Automation.PSDataCollection`1[System.Management.Automation.VerboseRecord]"
                item.Content = sender(e.Index).ToString()
                item.OutputType = OutputType.Verbose
            Case "System.Management.Automation.PSDataCollection`1[System.Management.Automation.DebugRecord]"
                item.Content = sender(e.Index).ToString()
                item.OutputType = OutputType.Debug
            Case Else
                item.Content = sender(e.Index).ToString()
        End Select
        prvOutputQ.Enqueue(item)

    End Sub

    Private Sub LogCommandExecuted()

        Dim strbld As New StringBuilder(prvScript)
        If LogParameters Then
            For Each pair In prvParams
                strbld.Append(" -" & pair.Key)
                If pair.Value.GetType.Name = "String[]" Then
                    strbld.Append(" @(")
                    For Each item In pair.Value
                        strbld.Append(" '" & item.ToString & "',")
                    Next
                    strbld.Remove(strbld.Length - 1, 1)
                    strbld.Append(")")
                Else
                    strbld.Append(" '" & pair.Value & "'")
                End If
            Next
        End If

        dlog.Info("Executing|" & strbld.ToString & "|-")

    End Sub

    Public Sub AddParameter(Key As String, Value As Object)
        UpdateParameter(Key, Value)
    End Sub

    Public Sub RemoveParameter(Key As String)
        If prvParams.ContainsKey(Key) Then
            prvParams.Remove(Key)
        End If
    End Sub

    Public Sub UpdateParameter(Key As String, Value As Object)
        RemoveParameter(Key)
        prvParams.Add(Key, Value)
    End Sub

    Public Sub ClearParameters()
        prvParams.Clear()
    End Sub
    Public Sub ClearOutput()
        prvOutput = ""
    End Sub
End Class
