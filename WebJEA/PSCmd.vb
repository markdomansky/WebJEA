Imports System.DirectoryServices.AccountManagement

Public Class PSCmd

    Public DisplayName As String
    Public LogParameters As TriState = TriState.UseDefault
    Public prvID As String
    Public Synopsis As String
    Public Description As String
    Public OnloadScript As String
    Public OnloadScriptTimeout As Integer = 30
    Public Script As String
    Public ScriptTimeout As Integer = 30
    Public PermittedGroups As New List(Of String)()
    Public Parameters As New List(Of PSCmdParam)
    Public ParseScript As TriState = TriState.UseDefault
    Private prvGroupSIDs As New List(Of String)

    Public Sub New()

    End Sub

    Public Property ID As String
        Get
            Return prvID
        End Get
        Set(value As String)
            prvID = value.ToLower()
        End Set
    End Property

    Public Function IsCommandAvailable(uinfo As UserInfo) As Boolean

        If prvGroupSIDs.Contains("*") Then 'shortcut, if this group is for anyone, then return true
            Return True
        End If

        For Each usersid As String In uinfo.MemberOfSIDs
            If prvGroupSIDs.Contains(usersid) Then Return True
        Next

        Return False
    End Function

    Public Function GetMenuItem() As MenuItem
        Dim mi As New MenuItem
        mi.ID = ID
        mi.DisplayName = If(DisplayName IsNot Nothing, DisplayName, ID)
        mi.Description = Description
        Return mi
    End Function


    Public Sub InitGroups()
        'resolve groups to SIDs
        For Each group As String In PermittedGroups
            Dim grpsid As String = grpfinder.GetSID(group)
            If Not String.IsNullOrEmpty(grpsid) And Not prvGroupSIDs.Contains(grpsid) Then
                prvGroupSIDs.Add(grpsid)
            End If
        Next

    End Sub

    Public Sub InitScript(BasePath As String, DefaultparseScript As Boolean, DefaultLogParams As Boolean)

        dlog.Debug("PSCmd|Init " & prvID)

        If LogParameters = TriState.UseDefault Then
            LogParameters = DefaultLogParams
        End If

        'if onloadscript doesn't reference a full path, we'll build one
        If OnloadScript <> "" Then
            OnloadScript = RebuildPath(OnloadScript, BasePath)
        End If

        If Script <> "" Then
            Script = RebuildPath(Script, BasePath)

            'parse script
            If (DefaultparseScript = True And ParseScript = TriState.UseDefault) Or (ParseScript = True) Then
                Dim scriptparser As New PSScriptParser(Script)
                'This step merges the pscmd values underneath the current properties
                'if no value is specified in the current object, the scriptparser object will be used.
                If Synopsis = "" Then Synopsis = scriptparser.Synopsis
                If Description = "" Then Description = scriptparser.Description

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
