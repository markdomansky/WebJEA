Public Interface IAuthorizationService

    Sub InitGroups(config As IConfigProvider, grpfinder As IGroupResolver)
    Function IsGlobalUser(uinfo As UserInfo) As Boolean
    Function IsCommandAvailable(uinfo As UserInfo, commandId As String) As Boolean
    Function GetCommand(uinfo As UserInfo, commandId As String) As ConfigCmd
    Function GetMenu(uinfo As UserInfo) As List(Of MenuItem)
    Function GetMenuDataTable(uinfo As UserInfo, activeID As String) As DataTable

End Interface
