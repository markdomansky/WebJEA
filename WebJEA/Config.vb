Public Class Config

    Public Title As String
    Public LogParameters As Boolean = True
    Public Commands As List(Of PSCmd)
    Public ParseScript As TriState = TriState.True
    Private prvDefaultCommandId As String
    Public BasePath As String
    Public SendTelemetry As Boolean = True
    Public PermittedGroups As New List(Of String)()
    Private prvGroupSIDs As New List(Of String)

    Public Sub New()
        'future check check in-memory dtstamp
        'future check config.clixml dtstamp lastmodified
        'future if different, reload

        'read My.Settings.configfile

    End Sub

    Public Sub InitGroups()

        'resolve groups to SIDs that will always have access
        For Each group As String In PermittedGroups
            Dim grpsid As String = grpfinder.GetSID(group)
            If Not String.IsNullOrEmpty(grpsid) And Not prvGroupSIDs.Contains(grpsid) Then
                prvGroupSIDs.Add(grpsid)
            End If
        Next

        'just init the one command we're going to use
        For Each cmd As PSCmd In Commands
            'init all group data
            cmd.InitGroups()
        Next

    End Sub
    Public Sub Init(Optional cmdid As String = "")
        'just init the one command we're going to use
        If String.IsNullOrEmpty(cmdid) Then
            cmdid = prvDefaultCommandId
        End If
        For Each cmd As PSCmd In Commands

            If cmd.ID = cmdid Then
                cmd.InitScript(BasePath, ParseScript, LogParameters)
            End If
        Next

    End Sub
    Public Property DefaultCommandId As String
        Get
            Return prvDefaultCommandId
        End Get
        Set(value As String)
            prvDefaultCommandId = value.ToLower()
        End Set
    End Property


    Public Function GetMenu(uinfo As UserInfo) As List(Of MenuItem)

        Dim menuitems As New List(Of MenuItem)
        Dim globaluser As Boolean = IsGlobalUser(uinfo)
        dlog.Trace("GetMenu: IsGlobalUser: " & globaluser.ToString())

        For Each cmd As PSCmd In Commands
            dlog.Trace("Building Menu ")
            If cmd.IsCommandAvailable(uinfo) Or globaluser Then
                menuitems.Add(cmd.GetMenuItem)
            End If
        Next

        Return menuitems

    End Function

    Public Function GetMenuDataTable(uinfo As UserInfo, activeID As String) As DataTable

        'Dim ds As New DataSet
        Dim dt As New DataTable
        Dim dr As DataRow

        'build row schema
        Dim DispName As New DataColumn("DisplayName", Type.GetType("System.String"))
        Dim Description As New DataColumn("Description", Type.GetType("System.String"))
        Dim Uri As New DataColumn("Uri", Type.GetType("System.String"))
        Dim CSS As New DataColumn("CSS", Type.GetType("System.String"))
        dt.Columns.Add(DispName)
        dt.Columns.Add(Description)
        dt.Columns.Add(Uri)
        dt.Columns.Add(CSS)

        'get the menu set
        Dim menu As List(Of MenuItem) = GetMenu(uinfo)
        'build the data table
        For Each mi As MenuItem In menu
            dr = dt.NewRow()
            dr("DisplayName") = mi.DisplayName
            dr("Description") = mi.Description
            dr("Uri") = mi.Uri
            dr("CSS") = ""
            If activeID = mi.ID Then dr("CSS") = "active"

            dt.Rows.Add(dr)
        Next

        'ds.Tables.Add(dt)
        Return dt

    End Function

    Public Function IsGlobalUser(uinfo As UserInfo) As Boolean

#If DEBUG Then
        If PermittedGroups.Contains("*") Then
            Return True
        End If
#End If

        'if user is part of global groups, return true
        For Each usersid As String In uinfo.MemberOfSIDs
            If prvGroupSIDs.Contains(usersid) Then Return True
        Next

        'nope
        Return False
    End Function

    Public Function IsCommandAvailable(uinfo As UserInfo, ID As String) As Boolean

        If (IsGlobalUser(uinfo)) Then Return True

        'otherwise check if they have access via the command
        For Each Command As PSCmd In Commands
            If Command.ID = ID Then
                Return Command.IsCommandAvailable(uinfo)
            End If
        Next

        'no match
        Return False

    End Function

    Public Function GetCommand(uinfo As UserInfo, ID As String) As PSCmd

        Dim foundCmd As PSCmd

        For Each Command As PSCmd In Commands
            If Command.ID = ID Then
                foundCmd = Command
            End If
        Next

        If (IsGlobalUser(uinfo) Or foundCmd.IsCommandAvailable(uinfo)) Then
            Return foundCmd
        End If

        'no match
        Return Nothing
    End Function

End Class
