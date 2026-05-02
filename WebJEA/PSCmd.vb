Public Class PSCmd

    Private dlog As NLog.Logger = NLog.LogManager.GetCurrentClassLogger()

    Public Property Script As String
    Public Property LogParameters As TriState = TriState.UseDefault
    Public Property Parameters As New List(Of PSCmdParam)

    Private _parsedSynopsis As String = ""
    Private _parsedDescription As String = ""

    Public ReadOnly Property ParsedSynopsis As String
        Get
            Return _parsedSynopsis
        End Get
    End Property

    Public ReadOnly Property ParsedDescription As String
        Get
            Return _parsedDescription
        End Get
    End Property

    Public Sub New()

    End Sub

    Public Sub Init(BasePath As String, DefaultLogParams As Boolean)

        dlog.Debug("PSCmd|Init")

        If LogParameters = TriState.UseDefault Then
            LogParameters = DefaultLogParams
        End If

        If Script <> "" Then
            Script = RebuildPath(Script, BasePath)

            Dim scriptparser As New PSScriptParser(Script)
            _parsedSynopsis = scriptparser.Synopsis
            _parsedDescription = scriptparser.Description

            Dim newParams As List(Of PSCmdParam) = scriptparser.GetParameters

            'merge existing parameters into newparams
            For Each parsedParam As PSCmdParam In newParams
                Dim findMatchedParam As PSCmdParam = Nothing
                'this looks for an existing parameter to merge with
                If Not (Parameters Is Nothing) Then
                    For Each parentParam As PSCmdParam In Parameters
                        If parsedParam.Name.ToUpper = parentParam.Name.ToUpper Then
                            findMatchedParam = parentParam
                        End If
                    Next
                End If

                If Not (findMatchedParam Is Nothing) Then
                    'matched an existing parameter, prefer settings from Parameters over newParams
                    'TODO: remove this logic because we're going to stop honoring parameters stored in the config
                    parsedParam.MergeOver(findMatchedParam)
                End If

            Next

            'now check for params missing from newParams that are in Parameters
            If Not (Parameters Is Nothing) Then
                For Each parentParam As PSCmdParam In Parameters
                    Dim findMatchedParam As PSCmdParam = Nothing
                    For Each parsedParam As PSCmdParam In newParams
                        If parsedParam.Name.ToUpper = parentParam.Name.ToUpper Then
                            findMatchedParam = parentParam
                        End If
                    Next

                    If findMatchedParam Is Nothing Then
                        'did not find a match, add as new
                        newParams.Add(parentParam)
                    End If
                Next
            End If

            'then replace parameters with newParams
            Parameters = newParams

        End If

    End Sub

    Private Function RebuildPath(scriptPath As String, BasePath As String) As String

        If Not String.IsNullOrEmpty(scriptPath) Then
            If scriptPath <> IO.Path.GetFullPath(scriptPath) Then
                If IO.Path.IsPathRooted(scriptPath) Then
                    scriptPath = BasePath & scriptPath
                Else
                    scriptPath = BasePath & "\" & scriptPath
                End If
            End If
        End If

        Return scriptPath
    End Function

End Class
