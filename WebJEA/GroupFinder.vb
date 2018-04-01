Imports System.DirectoryServices.AccountManagement

Public Class GroupFinder

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

        '#If DEBUG Then
        '        Return "*"
        '#End If

        'create a context for the domain/machine in the group
        Dim pc As PrincipalContext
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
                End Try
            Else
                Try
                    pc = New PrincipalContext(ContextType.Domain, groupcontext)
                    dlog.Trace("GroupFinder: GetSID: Adding Domain Context: " & groupcontext)
                Catch ex As Exception
                End Try
                If pc Is Nothing Then
                    Try
                        pc = New PrincipalContext(ContextType.Machine, groupcontext)
                        dlog.Trace("GroupFinder: GetSID: Adding Machine Context: " & groupcontext)
                    Catch ex As Exception
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
                dlog.Trace("GroupFinder: GetSID: Found Group SID: " & groupname & ": " & grp.Sid.ToString)
                Return grp.Sid.ToString
            Catch ex As Exception
            End Try

            Try
                Dim usr As UserPrincipal = UserPrincipal.FindByIdentity(pc, groupname)
                dlog.Trace("GroupFinder: GetSID: Found User SID: " & groupname & ": " & usr.Sid.ToString)
                Return usr.Sid.ToString
            Catch ex As Exception
            End Try
        End If


        Return ""

    End Function



End Class
