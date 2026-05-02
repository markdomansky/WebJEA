Imports System
Imports Xunit
Imports WebJEA

Namespace WebJEA_UnitTests_VBNET
    Public Class ConfigCmdTests

#Region "ID property - lowercasing"

        <Fact>
        Public Sub ID_SetUpperCase_StoredAsLowerCase()
            Dim cmd As New ConfigCmd()
            cmd.ID = "MY-COMMAND"

            Assert.Equal("my-command", cmd.ID)
        End Sub

        <Fact>
        Public Sub ID_SetMixedCase_StoredAsLowerCase()
            Dim cmd As New ConfigCmd()
            cmd.ID = "Get-Process"

            Assert.Equal("get-process", cmd.ID)
        End Sub

        <Fact>
        Public Sub ID_SetLowerCase_UnchangedAsLowerCase()
            Dim cmd As New ConfigCmd()
            cmd.ID = "already-lower"

            Assert.Equal("already-lower", cmd.ID)
        End Sub

#End Region

#Region "GetMenuItem"

        <Fact>
        Public Sub GetMenuItem_ReturnsMenuItemWithCorrectId()
            Dim cmd As New ConfigCmd()
            cmd.ID = "test-cmd"
            cmd.DisplayName = "Test Command"
            cmd.Description = "A test command"

            Dim mi = cmd.GetMenuItem()

            Assert.Equal("test-cmd", mi.ID)
            Assert.Equal("Test Command", mi.DisplayName)
            Assert.Equal("A test command", mi.Description)
        End Sub

        <Fact>
        Public Sub GetMenuItem_NullDisplayName_UsesIdAsDisplayName()
            Dim cmd As New ConfigCmd()
            cmd.ID = "fallback-cmd"
            cmd.DisplayName = Nothing

            Dim mi = cmd.GetMenuItem()

            Assert.Equal("fallback-cmd", mi.DisplayName)
        End Sub

        <Fact>
        Public Sub GetMenuItem_HasDisplayName_UsesDisplayName()
            Dim cmd As New ConfigCmd()
            cmd.ID = "cmd1"
            cmd.DisplayName = "Friendly Name"

            Dim mi = cmd.GetMenuItem()

            Assert.Equal("Friendly Name", mi.DisplayName)
        End Sub

        <Fact>
        Public Sub GetMenuItem_Synopsis_PropagatedToMenuItem()
            Dim cmd As New ConfigCmd()
            cmd.ID = "cmd1"
            cmd.Synopsis = "Short description"

            Dim mi = cmd.GetMenuItem()

            Assert.Equal("Short description", mi.Synopsis)
        End Sub

#End Region

#Region "Default property values"

        <Fact>
        Public Sub NewConfigCmd_PermittedGroups_IsEmptyList()
            Dim cmd As New ConfigCmd()

            Assert.NotNull(cmd.PermittedGroups)
            Assert.Empty(cmd.PermittedGroups)
        End Sub

        <Fact>
        Public Sub NewConfigCmd_LogParameters_UsesDefault()
            Dim cmd As New ConfigCmd()

            Assert.Equal(TriState.UseDefault, cmd.LogParameters)
        End Sub


#End Region

#Region "Property round-trip"

        <Fact>
        Public Sub Properties_RoundTrip()
            Dim cmd As New ConfigCmd()
            cmd.ID = "rt-cmd"
            cmd.DisplayName = "Round Trip"
            cmd.Synopsis = "Synopsis text"
            cmd.Description = "Description text"
            cmd.Script = "script.ps1"
            cmd.OnloadScript = "onload.ps1"
            cmd.LogParameters = TriState.True
            cmd.PermittedGroups.Add("Domain Admins")

            Assert.Equal("rt-cmd", cmd.ID)
            Assert.Equal("Round Trip", cmd.DisplayName)
            Assert.Equal("Synopsis text", cmd.Synopsis)
            Assert.Equal("Description text", cmd.Description)
            Assert.Equal("script.ps1", cmd.Script)
            Assert.Equal("onload.ps1", cmd.OnloadScript)
            Assert.Equal(TriState.True, cmd.LogParameters)
            Assert.Contains("Domain Admins", cmd.PermittedGroups)
        End Sub

#End Region

    End Class
End Namespace
