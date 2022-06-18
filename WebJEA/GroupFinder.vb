Imports System.DirectoryServices.AccountManagement

Public Class GroupFinder
    Private dlog As NLog.Logger = NLog.LogManager.GetCurrentClassLogger()

    Private prvPC As New Dictionary(Of String, PrincipalContext)

    Public Sub New()
    End Sub

    Public Function GetSID(input As String) As String

        dlog.Trace("GroupFinder: GetSID: Cached Contexts: " & prvPC.Count.ToString)

        Dim groupname As String = ""
        Dim groupcontext As String = ""
        dlog.Trace("GroupFinder: GetSID: Input: " & input)
        If input = "*" Then 'allow all users
            dlog.Trace("GroupFinder: GetSID: * All Authd Users")
            Return "*"
        ElseIf input.Contains("\") Then 'process as domain\sam
            groupname = input.Split("\")(1)
            groupcontext = input.Split("\")(0).ToUpper
        ElseIf input.Contains("@") Then 'process as upn
            groupname = input.Split("@")(0)
            groupcontext = input.Split("@")(1).ToUpper
        Else
            'no groupcontext
            groupname = input
        End If

        If groupcontext = "." Then 'context doesn't seem to reliably support ".", so we convert to machinename for clarity.
            groupcontext = System.Environment.MachineName
        End If
        '#If DEBUG Then
        '        Return "*"
        '#End If

        'create a context for the domain/machine in the group
        Dim pc As PrincipalContext = Nothing
        If prvPC.ContainsKey(groupcontext) Then 'sid already found in cache
            dlog.Trace("GroupFinder: GetSID: Found cached Context: " & groupcontext)
            pc = prvPC(groupcontext)
        Else
            'try connecting to domain
            If groupcontext = "" Then
                dlog.Trace("GroupFinder: GetSID: Adding Default Domain Context")
                Try
                    pc = New PrincipalContext(ContextType.Domain)
                Catch ex As Exception
                    dlog.Trace("GroupFinder: GetSID: Failed to resolve as Default Domain Context ()")
                End Try
            Else
                Try
                    pc = New PrincipalContext(ContextType.Domain, groupcontext)
                    dlog.Trace("GroupFinder: GetSID: Adding Domain Context: " & groupcontext)
                Catch ex As Exception
                    dlog.Trace("GroupFinder: GetSID: Failed to resolve as Domain Context (" & groupcontext & ")")
                End Try
                If pc Is Nothing Then
                    Try
                        pc = New PrincipalContext(ContextType.Machine, groupcontext)
                        dlog.Trace("GroupFinder: GetSID: Adding Machine Context: " & groupcontext)
                    Catch ex As Exception
                        dlog.Trace("GroupFinder: GetSID: Failed to resolve as Machine Context (" & groupcontext & ")")
                    End Try
                End If

            End If
            If Not IsNothing(pc) Then
                prvPC.Add(groupcontext, pc)
            End If

        End If

        If Not IsNothing(pc) Then
            Try
                Dim grp As GroupPrincipal = GroupPrincipal.FindByIdentity(pc, groupname)
                If (grp IsNot Nothing) Then
                    dlog.Trace("GroupFinder: GetSID: Found Group SID: " & groupname & ": " & grp.Sid.ToString)
                    Return grp.Sid.ToString
                End If
            Catch ex As Exception
                'dlog.Error("GroupFinder: GetSID: Error Trying as Group. (" & groupname & ")")
            End Try

            Try
                Dim usr As UserPrincipal = UserPrincipal.FindByIdentity(pc, groupname)
                If (usr IsNot Nothing) Then
                    dlog.Trace("GroupFinder: GetSID: Found User SID: " & groupname & ": " & usr.Sid.ToString)
                    Return usr.Sid.ToString
                End If
            Catch ex As Exception
                'dlog.Error("GroupFinder: GetSID: Error Trying as User. (" & groupname & ")")
            End Try
        End If

        dlog.Error("GroupFinder: GetSID: No SID Matched.")
        Return ""

    End Function



End Class
