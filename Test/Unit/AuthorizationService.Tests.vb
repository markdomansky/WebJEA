Imports System
Imports Xunit
Imports Moq
Imports WebJEA

Namespace WebJEA_UnitTests_VBNET
    Public Class AuthorizationServiceTests

        Private Function CreateConfig(permittedGroups As List(Of String), commands As List(Of ConfigCmd)) As Mock(Of IConfigProvider)
            Dim mockConfig As New Mock(Of IConfigProvider)
            mockConfig.Setup(Function(c) c.PermittedGroups).Returns(permittedGroups)
            mockConfig.Setup(Function(c) c.Commands).Returns(commands)
            Return mockConfig
        End Function

        Private Function CreateGroupResolver(mappings As Dictionary(Of String, String)) As Mock(Of IGroupResolver)
            Dim mockResolver As New Mock(Of IGroupResolver)
            For Each kvp In mappings
                mockResolver.Setup(Function(g) g.GetSID(kvp.Key)).Returns(kvp.Value)
            Next
            Return mockResolver
        End Function

        Private Function CreateUserInfo(sids As List(Of String)) As UserInfo
            Return New UserInfo(sids, "TESTDOMAIN\testuser")
        End Function

        Private Function BuildService(config As IConfigProvider, resolver As IGroupResolver) As AuthorizationService
            Dim svc As New AuthorizationService()
            svc.InitGroups(config, resolver)
            Return svc
        End Function

#Region "IsGlobalUser"

        <Fact>
        Public Sub IsGlobalUser_UserInGlobalGroup_ReturnsTrue()
            Dim config = CreateConfig(
                New List(Of String) From {"Admins"},
                New List(Of ConfigCmd))
            Dim resolver = CreateGroupResolver(
                New Dictionary(Of String, String) From {{"Admins", "S-1-5-21-ADMIN"}})
            Dim svc = BuildService(config.Object, resolver.Object)

            Dim user = CreateUserInfo(New List(Of String) From {"S-1-5-21-ADMIN"})

            Assert.True(svc.IsGlobalUser(user))
        End Sub

        <Fact>
        Public Sub IsGlobalUser_UserNotInGlobalGroup_ReturnsFalse()
            Dim config = CreateConfig(
                New List(Of String) From {"Admins"},
                New List(Of ConfigCmd))
            Dim resolver = CreateGroupResolver(
                New Dictionary(Of String, String) From {{"Admins", "S-1-5-21-ADMIN"}})
            Dim svc = BuildService(config.Object, resolver.Object)

            Dim user = CreateUserInfo(New List(Of String) From {"S-1-5-21-OTHER"})

            Assert.False(svc.IsGlobalUser(user))
        End Sub

        <Fact>
        Public Sub IsGlobalUser_NoGlobalGroups_ReturnsFalse()
            Dim config = CreateConfig(
                New List(Of String),
                New List(Of ConfigCmd))
            Dim resolver = CreateGroupResolver(New Dictionary(Of String, String))
            Dim svc = BuildService(config.Object, resolver.Object)

            Dim user = CreateUserInfo(New List(Of String) From {"S-1-5-21-ADMIN"})

            Assert.False(svc.IsGlobalUser(user))
        End Sub

        <Fact>
        Public Sub IsGlobalUser_UserHasMultipleSIDs_MatchesAny()
            Dim config = CreateConfig(
                New List(Of String) From {"Admins"},
                New List(Of ConfigCmd))
            Dim resolver = CreateGroupResolver(
                New Dictionary(Of String, String) From {{"Admins", "S-1-5-21-ADMIN"}})
            Dim svc = BuildService(config.Object, resolver.Object)

            Dim user = CreateUserInfo(New List(Of String) From {"S-1-5-21-OTHER", "S-1-5-21-ADMIN"})

            Assert.True(svc.IsGlobalUser(user))
        End Sub

        <Fact>
        Public Sub IsGlobalUser_GroupResolverReturnsEmpty_ReturnsFalse()
            Dim config = CreateConfig(
                New List(Of String) From {"BadGroup"},
                New List(Of ConfigCmd))
            Dim resolver = CreateGroupResolver(
                New Dictionary(Of String, String) From {{"BadGroup", ""}})
            Dim svc = BuildService(config.Object, resolver.Object)

            Dim user = CreateUserInfo(New List(Of String) From {"S-1-5-21-ADMIN"})

            Assert.False(svc.IsGlobalUser(user))
        End Sub

#End Region

#Region "IsCommandAvailable"

        <Fact>
        Public Sub IsCommandAvailable_GlobalUser_AlwaysTrue()
            Dim cmd As New ConfigCmd()
            cmd.ID = "cmd1"
            cmd.PermittedGroups = New List(Of String) From {"SpecificGroup"}

            Dim config = CreateConfig(
                New List(Of String) From {"Admins"},
                New List(Of ConfigCmd) From {cmd})
            Dim resolver = CreateGroupResolver(
                New Dictionary(Of String, String) From {
                    {"Admins", "S-1-5-21-ADMIN"},
                    {"SpecificGroup", "S-1-5-21-SPECIFIC"}
                })
            Dim svc = BuildService(config.Object, resolver.Object)

            Dim user = CreateUserInfo(New List(Of String) From {"S-1-5-21-ADMIN"})

            Assert.True(svc.IsCommandAvailable(user, "cmd1"))
        End Sub

        <Fact>
        Public Sub IsCommandAvailable_UserInCommandGroup_ReturnsTrue()
            Dim cmd As New ConfigCmd()
            cmd.ID = "cmd1"
            cmd.PermittedGroups = New List(Of String) From {"CommandGroup"}

            Dim config = CreateConfig(
                New List(Of String),
                New List(Of ConfigCmd) From {cmd})
            Dim resolver = CreateGroupResolver(
                New Dictionary(Of String, String) From {{"CommandGroup", "S-1-5-21-CMDGRP"}})
            Dim svc = BuildService(config.Object, resolver.Object)

            Dim user = CreateUserInfo(New List(Of String) From {"S-1-5-21-CMDGRP"})

            Assert.True(svc.IsCommandAvailable(user, "cmd1"))
        End Sub

        <Fact>
        Public Sub IsCommandAvailable_UserNotInCommandGroup_ReturnsFalse()
            Dim cmd As New ConfigCmd()
            cmd.ID = "cmd1"
            cmd.PermittedGroups = New List(Of String) From {"CommandGroup"}

            Dim config = CreateConfig(
                New List(Of String),
                New List(Of ConfigCmd) From {cmd})
            Dim resolver = CreateGroupResolver(
                New Dictionary(Of String, String) From {{"CommandGroup", "S-1-5-21-CMDGRP"}})
            Dim svc = BuildService(config.Object, resolver.Object)

            Dim user = CreateUserInfo(New List(Of String) From {"S-1-5-21-OTHER"})

            Assert.False(svc.IsCommandAvailable(user, "cmd1"))
        End Sub

        <Fact>
        Public Sub IsCommandAvailable_WildcardGroup_ReturnsTrue()
            Dim cmd As New ConfigCmd()
            cmd.ID = "cmd1"
            cmd.PermittedGroups = New List(Of String) From {"*"}

            Dim config = CreateConfig(
                New List(Of String),
                New List(Of ConfigCmd) From {cmd})
            Dim resolver = CreateGroupResolver(
                New Dictionary(Of String, String) From {{"*", "*"}})
            Dim svc = BuildService(config.Object, resolver.Object)

            Dim user = CreateUserInfo(New List(Of String) From {"S-1-5-21-ANYONE"})

            Assert.True(svc.IsCommandAvailable(user, "cmd1"))
        End Sub

        <Fact>
        Public Sub IsCommandAvailable_UnknownCommand_ReturnsFalse()
            Dim config = CreateConfig(
                New List(Of String),
                New List(Of ConfigCmd))
            Dim resolver = CreateGroupResolver(New Dictionary(Of String, String))
            Dim svc = BuildService(config.Object, resolver.Object)

            Dim user = CreateUserInfo(New List(Of String) From {"S-1-5-21-USER"})

            Assert.False(svc.IsCommandAvailable(user, "nonexistent"))
        End Sub

#End Region

#Region "GetCommand"

        <Fact>
        Public Sub GetCommand_AuthorizedUser_ReturnsCommand()
            Dim cmd As New ConfigCmd()
            cmd.ID = "cmd1"
            cmd.DisplayName = "Test Command"
            cmd.PermittedGroups = New List(Of String) From {"Users"}

            Dim config = CreateConfig(
                New List(Of String),
                New List(Of ConfigCmd) From {cmd})
            Dim resolver = CreateGroupResolver(
                New Dictionary(Of String, String) From {{"Users", "S-1-5-21-USER"}})
            Dim svc = BuildService(config.Object, resolver.Object)

            Dim user = CreateUserInfo(New List(Of String) From {"S-1-5-21-USER"})

            Dim result = svc.GetCommand(user, "cmd1")

            Assert.NotNull(result)
            Assert.Equal("cmd1", result.ID)
        End Sub

        <Fact>
        Public Sub GetCommand_UnauthorizedUser_ReturnsNothing()
            Dim cmd As New ConfigCmd()
            cmd.ID = "cmd1"
            cmd.PermittedGroups = New List(Of String) From {"PrivilegedGroup"}

            Dim config = CreateConfig(
                New List(Of String),
                New List(Of ConfigCmd) From {cmd})
            Dim resolver = CreateGroupResolver(
                New Dictionary(Of String, String) From {{"PrivilegedGroup", "S-1-5-21-PRIV"}})
            Dim svc = BuildService(config.Object, resolver.Object)

            Dim user = CreateUserInfo(New List(Of String) From {"S-1-5-21-OTHER"})

            Dim result = svc.GetCommand(user, "cmd1")

            Assert.Null(result)
        End Sub

        <Fact>
        Public Sub GetCommand_NonexistentCommand_ReturnsNothing()
            Dim config = CreateConfig(
                New List(Of String) From {"Admins"},
                New List(Of ConfigCmd))
            Dim resolver = CreateGroupResolver(
                New Dictionary(Of String, String) From {{"Admins", "S-1-5-21-ADMIN"}})
            Dim svc = BuildService(config.Object, resolver.Object)

            Dim user = CreateUserInfo(New List(Of String) From {"S-1-5-21-ADMIN"})

            Dim result = svc.GetCommand(user, "nonexistent")

            Assert.Null(result)
        End Sub

        <Fact>
        Public Sub GetCommand_GlobalUser_ReturnsAnyCommand()
            Dim cmd As New ConfigCmd()
            cmd.ID = "restricted-cmd"
            cmd.PermittedGroups = New List(Of String) From {"SpecificGroup"}

            Dim config = CreateConfig(
                New List(Of String) From {"Admins"},
                New List(Of ConfigCmd) From {cmd})
            Dim resolver = CreateGroupResolver(
                New Dictionary(Of String, String) From {
                    {"Admins", "S-1-5-21-ADMIN"},
                    {"SpecificGroup", "S-1-5-21-SPECIFIC"}
                })
            Dim svc = BuildService(config.Object, resolver.Object)

            Dim user = CreateUserInfo(New List(Of String) From {"S-1-5-21-ADMIN"})

            Dim result = svc.GetCommand(user, "restricted-cmd")

            Assert.NotNull(result)
        End Sub

#End Region

#Region "GetMenu"

        <Fact>
        Public Sub GetMenu_GlobalUser_ReturnsAllCommands()
            Dim cmd1 As New ConfigCmd()
            cmd1.ID = "cmd1"
            cmd1.DisplayName = "Command 1"
            cmd1.PermittedGroups = New List(Of String) From {"Group1"}

            Dim cmd2 As New ConfigCmd()
            cmd2.ID = "cmd2"
            cmd2.DisplayName = "Command 2"
            cmd2.PermittedGroups = New List(Of String) From {"Group2"}

            Dim config = CreateConfig(
                New List(Of String) From {"Admins"},
                New List(Of ConfigCmd) From {cmd1, cmd2})
            Dim resolver = CreateGroupResolver(
                New Dictionary(Of String, String) From {
                    {"Admins", "S-1-5-21-ADMIN"},
                    {"Group1", "S-1-5-21-G1"},
                    {"Group2", "S-1-5-21-G2"}
                })
            Dim svc = BuildService(config.Object, resolver.Object)

            Dim user = CreateUserInfo(New List(Of String) From {"S-1-5-21-ADMIN"})

            Dim menu = svc.GetMenu(user)

            Assert.Equal(2, menu.Count)
        End Sub

        <Fact>
        Public Sub GetMenu_LimitedUser_ReturnsOnlyAuthorizedCommands()
            Dim cmd1 As New ConfigCmd()
            cmd1.ID = "cmd1"
            cmd1.DisplayName = "Command 1"
            cmd1.PermittedGroups = New List(Of String) From {"AllUsers"}

            Dim cmd2 As New ConfigCmd()
            cmd2.ID = "cmd2"
            cmd2.DisplayName = "Command 2"
            cmd2.PermittedGroups = New List(Of String) From {"AdminOnly"}

            Dim config = CreateConfig(
                New List(Of String),
                New List(Of ConfigCmd) From {cmd1, cmd2})
            Dim resolver = CreateGroupResolver(
                New Dictionary(Of String, String) From {
                    {"AllUsers", "S-1-5-21-ALL"},
                    {"AdminOnly", "S-1-5-21-ADMONLY"}
                })
            Dim svc = BuildService(config.Object, resolver.Object)

            Dim user = CreateUserInfo(New List(Of String) From {"S-1-5-21-ALL"})

            Dim menu = svc.GetMenu(user)

            Assert.Single(menu)
            Assert.Equal("cmd1", menu(0).ID)
        End Sub

        <Fact>
        Public Sub GetMenu_NoAccess_ReturnsEmptyList()
            Dim cmd As New ConfigCmd()
            cmd.ID = "cmd1"
            cmd.PermittedGroups = New List(Of String) From {"SpecialGroup"}

            Dim config = CreateConfig(
                New List(Of String),
                New List(Of ConfigCmd) From {cmd})
            Dim resolver = CreateGroupResolver(
                New Dictionary(Of String, String) From {{"SpecialGroup", "S-1-5-21-SPEC"}})
            Dim svc = BuildService(config.Object, resolver.Object)

            Dim user = CreateUserInfo(New List(Of String) From {"S-1-5-21-NOBODY"})

            Dim menu = svc.GetMenu(user)

            Assert.Empty(menu)
        End Sub

#End Region

#Region "GetMenuDataTable"

        <Fact>
        Public Sub GetMenuDataTable_ReturnsCorrectColumns()
            Dim cmd As New ConfigCmd()
            cmd.ID = "cmd1"
            cmd.DisplayName = "Command 1"
            cmd.Description = "Description 1"
            cmd.PermittedGroups = New List(Of String) From {"Users"}

            Dim config = CreateConfig(
                New List(Of String),
                New List(Of ConfigCmd) From {cmd})
            Dim resolver = CreateGroupResolver(
                New Dictionary(Of String, String) From {{"Users", "S-1-5-21-USER"}})
            Dim svc = BuildService(config.Object, resolver.Object)

            Dim user = CreateUserInfo(New List(Of String) From {"S-1-5-21-USER"})

            Dim dt = svc.GetMenuDataTable(user, "")

            Assert.True(dt.Columns.Contains("DisplayName"))
            Assert.True(dt.Columns.Contains("Description"))
            Assert.True(dt.Columns.Contains("Uri"))
            Assert.True(dt.Columns.Contains("CSS"))
        End Sub

        <Fact>
        Public Sub GetMenuDataTable_ActiveCommand_HasActiveCss()
            Dim cmd As New ConfigCmd()
            cmd.ID = "cmd1"
            cmd.DisplayName = "Command 1"
            cmd.Description = "Description 1"
            cmd.PermittedGroups = New List(Of String) From {"Users"}

            Dim config = CreateConfig(
                New List(Of String),
                New List(Of ConfigCmd) From {cmd})
            Dim resolver = CreateGroupResolver(
                New Dictionary(Of String, String) From {{"Users", "S-1-5-21-USER"}})
            Dim svc = BuildService(config.Object, resolver.Object)

            Dim user = CreateUserInfo(New List(Of String) From {"S-1-5-21-USER"})

            Dim dt = svc.GetMenuDataTable(user, "cmd1")

            Assert.Equal(1, dt.Rows.Count)
            Assert.Equal("active", dt.Rows(0)("CSS").ToString())
        End Sub

        <Fact>
        Public Sub GetMenuDataTable_InactiveCommand_HasEmptyCss()
            Dim cmd As New ConfigCmd()
            cmd.ID = "cmd1"
            cmd.DisplayName = "Command 1"
            cmd.Description = "Description 1"
            cmd.PermittedGroups = New List(Of String) From {"Users"}

            Dim config = CreateConfig(
                New List(Of String),
                New List(Of ConfigCmd) From {cmd})
            Dim resolver = CreateGroupResolver(
                New Dictionary(Of String, String) From {{"Users", "S-1-5-21-USER"}})
            Dim svc = BuildService(config.Object, resolver.Object)

            Dim user = CreateUserInfo(New List(Of String) From {"S-1-5-21-USER"})

            Dim dt = svc.GetMenuDataTable(user, "other-cmd")

            Assert.Equal(1, dt.Rows.Count)
            Assert.Equal("", dt.Rows(0)("CSS").ToString())
        End Sub

#End Region

    End Class
End Namespace
