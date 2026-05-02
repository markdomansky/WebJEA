Imports System
Imports System.IO
Imports Xunit
Imports Moq
Imports WebJEA

Namespace WebJEA_UnitTests_VBNET
    Public Class CommandServiceTests

        Private ReadOnly TestScriptsPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\..\..\TestScripts\")

#Region "LoadConfig - JSON deserialization"

        <Fact>
        Public Sub LoadConfig_ValidConfigFile_LoadsTitle()
            Dim svc As New CommandService()
            Dim configPath = Path.GetFullPath(Path.Combine(TestScriptsPath, "config.json"))
            Dim mockResolver As New Mock(Of IGroupResolver)
            mockResolver.Setup(Function(g) g.GetSID(It.IsAny(Of String)())).Returns("")

            svc.LoadConfig(configPath, mockResolver.Object)

            Assert.NotNull(svc.Config)
            Assert.NotNull(svc.Config.Title)
        End Sub

        <Fact>
        Public Sub LoadConfig_ValidConfigFile_LoadsCommands()
            Dim svc As New CommandService()
            Dim configPath = Path.GetFullPath(Path.Combine(TestScriptsPath, "config.json"))
            Dim mockResolver As New Mock(Of IGroupResolver)
            mockResolver.Setup(Function(g) g.GetSID(It.IsAny(Of String)())).Returns("")

            svc.LoadConfig(configPath, mockResolver.Object)

            Assert.NotNull(svc.Config.Commands)
            Assert.NotEmpty(svc.Config.Commands)
        End Sub

        <Fact>
        Public Sub LoadConfig_InvalidFilePath_Throws()
            Dim svc As New CommandService()
            Dim mockResolver As New Mock(Of IGroupResolver)

            Assert.Throws(Of Exception)(Sub() svc.LoadConfig("C:\nonexistent\bad.json", mockResolver.Object))
        End Sub

        <Fact>
        Public Sub LoadConfig_InitializesAuthService()
            Dim svc As New CommandService()
            Dim configPath = Path.GetFullPath(Path.Combine(TestScriptsPath, "config.json"))
            Dim mockResolver As New Mock(Of IGroupResolver)
            mockResolver.Setup(Function(g) g.GetSID(It.IsAny(Of String)())).Returns("")

            svc.LoadConfig(configPath, mockResolver.Object)

            Assert.NotNull(svc.Auth)
        End Sub

#End Region

#Region "ResolveCommandId"

        <Fact>
        Public Sub ResolveCommandId_AuthorizedUser_ReturnsRequestedId()
            Dim svc As New CommandService()
            Dim configPath = Path.GetFullPath(Path.Combine(TestScriptsPath, "config.json"))
            Dim mockResolver As New Mock(Of IGroupResolver)
            mockResolver.Setup(Function(g) g.GetSID("*")).Returns("*")
            mockResolver.Setup(Function(g) g.GetSID(It.IsNotIn("*"))).Returns("")

            svc.LoadConfig(configPath, mockResolver.Object)

            Dim user As New UserInfo(New List(Of String) From {"S-1-5-21-DUMMY"}, "DOMAIN\user")

            Dim result = svc.ResolveCommandId(user, svc.Config.Commands(0).ID)
            Assert.Equal(svc.Config.Commands(0).ID, result)
        End Sub

        <Fact>
        Public Sub ResolveCommandId_UnauthorizedUser_ReturnsEmptyString()
            Dim svc As New CommandService()
            Dim configPath = Path.GetFullPath(Path.Combine(TestScriptsPath, "config.json"))
            Dim mockResolver As New Mock(Of IGroupResolver)
            mockResolver.Setup(Function(g) g.GetSID(It.IsAny(Of String)())).Returns("")

            svc.LoadConfig(configPath, mockResolver.Object)

            Dim user As New UserInfo(New List(Of String) From {"S-1-5-21-NOBODY"}, "DOMAIN\nobody")

            Dim result = svc.ResolveCommandId(user, "nonexistent-cmd")
            Assert.Equal("", result)
        End Sub

#End Region

#Region "GetCommand"

        <Fact>
        Public Sub GetCommand_AuthorizedUser_ReturnsConfigCmd()
            Dim svc As New CommandService()
            Dim configPath = Path.GetFullPath(Path.Combine(TestScriptsPath, "config.json"))
            Dim mockResolver As New Mock(Of IGroupResolver)
            mockResolver.Setup(Function(g) g.GetSID(It.IsAny(Of String)())).Returns("S-1-5-21-MATCH")

            svc.LoadConfig(configPath, mockResolver.Object)

            Dim user As New UserInfo(New List(Of String) From {"S-1-5-21-MATCH"}, "DOMAIN\user")

            Dim cmd = svc.GetCommand(user, svc.Config.Commands(0).ID)
            Assert.NotNull(cmd)
            Assert.IsType(Of ConfigCmd)(cmd)
        End Sub

        <Fact>
        Public Sub GetCommand_UnauthorizedUser_ReturnsNothing()
            Dim svc As New CommandService()
            Dim configPath = Path.GetFullPath(Path.Combine(TestScriptsPath, "config.json"))
            Dim mockResolver As New Mock(Of IGroupResolver)
            mockResolver.Setup(Function(g) g.GetSID(It.IsAny(Of String)())).Returns("")

            svc.LoadConfig(configPath, mockResolver.Object)

            Dim user As New UserInfo(New List(Of String) From {"S-1-5-21-NOBODY"}, "DOMAIN\nobody")

            Dim firstCmdId = svc.Config.Commands(0).ID
            Dim cmd = svc.GetCommand(user, firstCmdId)
            Assert.Null(cmd)
        End Sub

#End Region

#Region "GetScriptCmd"

        <Fact>
        Public Sub GetScriptCmd_CommandWithScript_ReturnsNonNull()
            Dim svc As New CommandService()
            Dim configPath = Path.GetFullPath(Path.Combine(TestScriptsPath, "config.json"))
            Dim mockResolver As New Mock(Of IGroupResolver)
            mockResolver.Setup(Function(g) g.GetSID(It.IsAny(Of String)())).Returns("")

            svc.LoadConfig(configPath, mockResolver.Object)

            Dim scriptCmd = svc.GetScriptCmd("validate")

            Assert.NotNull(scriptCmd)
            Assert.False(String.IsNullOrEmpty(scriptCmd.Script))
        End Sub

        <Fact>
        Public Sub GetScriptCmd_CommandWithoutScript_ReturnsNothing()
            Dim svc As New CommandService()
            Dim configPath = Path.GetFullPath(Path.Combine(TestScriptsPath, "config.json"))
            Dim mockResolver As New Mock(Of IGroupResolver)
            mockResolver.Setup(Function(g) g.GetSID(It.IsAny(Of String)())).Returns("")

            svc.LoadConfig(configPath, mockResolver.Object)

            Dim scriptCmd = svc.GetScriptCmd("t3")

            Assert.Null(scriptCmd)
        End Sub

        <Fact>
        Public Sub GetScriptCmd_UnknownCommand_ReturnsNothing()
            Dim svc As New CommandService()
            Dim configPath = Path.GetFullPath(Path.Combine(TestScriptsPath, "config.json"))
            Dim mockResolver As New Mock(Of IGroupResolver)
            mockResolver.Setup(Function(g) g.GetSID(It.IsAny(Of String)())).Returns("")

            svc.LoadConfig(configPath, mockResolver.Object)

            Dim scriptCmd = svc.GetScriptCmd("nonexistent-cmd")

            Assert.Null(scriptCmd)
        End Sub

        <Fact>
        Public Sub GetScriptCmd_CalledTwice_ReturnsSameInstance()
            Dim svc As New CommandService()
            Dim configPath = Path.GetFullPath(Path.Combine(TestScriptsPath, "config.json"))
            Dim mockResolver As New Mock(Of IGroupResolver)
            mockResolver.Setup(Function(g) g.GetSID(It.IsAny(Of String)())).Returns("")

            svc.LoadConfig(configPath, mockResolver.Object)

            Dim first = svc.GetScriptCmd("validate")
            Dim second = svc.GetScriptCmd("validate")

            Assert.Same(first, second)
        End Sub

#End Region

#Region "GetOnloadCmd"

        <Fact>
        Public Sub GetOnloadCmd_CommandWithOnloadScript_ReturnsNonNull()
            Dim svc As New CommandService()
            Dim configPath = Path.GetFullPath(Path.Combine(TestScriptsPath, "config.json"))
            Dim mockResolver As New Mock(Of IGroupResolver)
            mockResolver.Setup(Function(g) g.GetSID(It.IsAny(Of String)())).Returns("")

            svc.LoadConfig(configPath, mockResolver.Object)

            Dim onloadCmd = svc.GetOnloadCmd("validate")

            Assert.NotNull(onloadCmd)
            Assert.False(String.IsNullOrEmpty(onloadCmd.Script))
        End Sub

        <Fact>
        Public Sub GetOnloadCmd_CommandWithoutOnloadScript_ReturnsNothing()
            Dim svc As New CommandService()
            Dim configPath = Path.GetFullPath(Path.Combine(TestScriptsPath, "config.json"))
            Dim mockResolver As New Mock(Of IGroupResolver)
            mockResolver.Setup(Function(g) g.GetSID(It.IsAny(Of String)())).Returns("")

            svc.LoadConfig(configPath, mockResolver.Object)

            Dim onloadCmd = svc.GetOnloadCmd("t0")

            Assert.Null(onloadCmd)
        End Sub

        <Fact>
        Public Sub GetOnloadCmd_ScriptIsOnloadScriptPath()
            Dim svc As New CommandService()
            Dim configPath = Path.GetFullPath(Path.Combine(TestScriptsPath, "config.json"))
            Dim mockResolver As New Mock(Of IGroupResolver)
            mockResolver.Setup(Function(g) g.GetSID(It.IsAny(Of String)())).Returns("")

            svc.LoadConfig(configPath, mockResolver.Object)

            Dim onloadCmd = svc.GetOnloadCmd("validate")

            Assert.NotNull(onloadCmd)
            Assert.Contains("validate-onload", onloadCmd.Script)
        End Sub

#End Region

    End Class
End Namespace
