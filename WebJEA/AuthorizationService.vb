Public Class AuthorizationService
    Implements IAuthorizationService

    Private dlog As NLog.Logger = NLog.LogManager.GetCurrentClassLogger()
    Private _config As IConfigProvider
    Private _globalGroupSIDs As New List(Of String)
    Private _commandGroupSIDs As New Dictionary(Of String, List(Of String))

    Public Sub InitGroups(config As IConfigProvider, grpfinder As IGroupResolver) Implements IAuthorizationService.InitGroups
        _config = config

        For Each group As String In config.PermittedGroups
            Dim grpsid As String = grpfinder.GetSID(group)
            If Not String.IsNullOrEmpty(grpsid) And Not _globalGroupSIDs.Contains(grpsid) Then
                _globalGroupSIDs.Add(grpsid)
            End If
        Next

        For Each cmd As ConfigCmd In config.Commands
            Dim sids As New List(Of String)
            For Each group As String In cmd.PermittedGroups
                Dim grpsid As String = grpfinder.GetSID(group)
                If Not String.IsNullOrEmpty(grpsid) And Not sids.Contains(grpsid) Then
                    sids.Add(grpsid)
                End If
            Next
            _commandGroupSIDs(cmd.ID) = sids
        Next
    End Sub

    Public Function IsGlobalUser(uinfo As UserInfo) As Boolean Implements IAuthorizationService.IsGlobalUser

#If DEBUG Then
        If _config.PermittedGroups.Contains("*") Then
            Return True
        End If
#End If

        For Each usersid As String In uinfo.MemberOfSIDs
            If _globalGroupSIDs.Contains(usersid) Then Return True
        Next

        Return False
    End Function

    Public Function IsCommandAvailable(uinfo As UserInfo, commandId As String) As Boolean Implements IAuthorizationService.IsCommandAvailable
        If IsGlobalUser(uinfo) Then Return True

        Dim sids As List(Of String) = Nothing
        If _commandGroupSIDs.TryGetValue(commandId, sids) Then
            If sids.Contains("*") Then Return True
            For Each usersid As String In uinfo.MemberOfSIDs
                If sids.Contains(usersid) Then Return True
            Next
        End If

        Return False
    End Function

    Public Function GetCommand(uinfo As UserInfo, commandId As String) As ConfigCmd Implements IAuthorizationService.GetCommand
        Dim foundCmd As ConfigCmd = Nothing

        For Each cmd As ConfigCmd In _config.Commands
            If cmd.ID = commandId Then
                foundCmd = cmd
            End If
        Next

        If foundCmd IsNot Nothing Then
            If IsGlobalUser(uinfo) Or IsCommandAvailable(uinfo, commandId) Then
                Return foundCmd
            End If
        End If

        Return Nothing
    End Function

    Public Function GetMenu(uinfo As UserInfo) As List(Of MenuItem) Implements IAuthorizationService.GetMenu
        Dim menuitems As New List(Of MenuItem)
        Dim globaluser As Boolean = IsGlobalUser(uinfo)
        dlog.Trace("GetMenu: IsGlobalUser: " & globaluser.ToString())

        dlog.Trace("Building Menu")
        For Each cmd As ConfigCmd In _config.Commands
            If IsCommandAvailable(uinfo, cmd.ID) Or globaluser Then
                menuitems.Add(cmd.GetMenuItem)
            End If
        Next

        Return menuitems
    End Function

    Public Function GetMenuDataTable(uinfo As UserInfo, activeID As String) As DataTable Implements IAuthorizationService.GetMenuDataTable
        Dim dt As New DataTable
        Dim dr As DataRow

        Dim DispName As New DataColumn("DisplayName", Type.GetType("System.String"))
        Dim Description As New DataColumn("Description", Type.GetType("System.String"))
        Dim Uri As New DataColumn("Uri", Type.GetType("System.String"))
        Dim CSS As New DataColumn("CSS", Type.GetType("System.String"))
        dt.Columns.Add(DispName)
        dt.Columns.Add(Description)
        dt.Columns.Add(Uri)
        dt.Columns.Add(CSS)

        Dim menu As List(Of MenuItem) = GetMenu(uinfo)
        For Each mi As MenuItem In menu
            dr = dt.NewRow()
            dr("DisplayName") = mi.DisplayName
            dr("Description") = mi.Description
            dr("Uri") = mi.Uri
            dr("CSS") = ""
            If activeID = mi.ID Then dr("CSS") = "active"
            dt.Rows.Add(dr)
        Next

        Return dt
    End Function

End Class
