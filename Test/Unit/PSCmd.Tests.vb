Imports System
Imports Xunit
Imports WebJEA

Namespace WebJEA_UnitTests_VBNET
    Public Class PSCmdTests

#Region "Default property values"

        <Fact>
        Public Sub NewPSCmd_Parameters_IsEmptyList()
            Dim cmd As New PSCmd()

            Assert.NotNull(cmd.Parameters)
            Assert.Empty(cmd.Parameters)
        End Sub

#End Region

#Region "Init - path resolution"

        <Fact>
        Public Sub Init_AbsoluteScriptPath_RemainsAbsolute()
            Dim cmd As New PSCmd()
            cmd.Script = "C:\scripts\test.ps1"

            cmd.Init("C:\base", True)

            Assert.Equal("C:\scripts\test.ps1", cmd.Script)
        End Sub

        <Fact>
        Public Sub Init_RelativeScriptPath_PrependBasePath()
            Dim cmd As New PSCmd()
            cmd.Script = "test.ps1"

            cmd.Init("C:\base", True)

            Assert.Equal("C:\base\test.ps1", cmd.Script)
        End Sub

        <Fact>
        Public Sub Init_RootedScriptPath_PrependBasePath()
            Dim cmd As New PSCmd()
            cmd.Script = "\scripts\test.ps1"

            cmd.Init("C:\base", True)

            Assert.Equal("C:\base\scripts\test.ps1", cmd.Script)
        End Sub

        <Fact>
        Public Sub Init_EmptyScript_NoChange()
            Dim cmd As New PSCmd()
            cmd.Script = ""

            cmd.Init("C:\base", True)

            Assert.Equal("", cmd.Script)
        End Sub

        <Fact>
        Public Sub Init_LogParamsDefault_InheritsFromParam()
            Dim cmd As New PSCmd()
            cmd.Script = ""

            cmd.Init("C:\base", True)

            Assert.Equal(TriState.True, cmd.LogParameters)
        End Sub

        <Fact>
        Public Sub Init_LogParamsDefault_InheritsFalse()
            Dim cmd As New PSCmd()
            cmd.Script = ""

            cmd.Init("C:\base", False)

            Assert.Equal(TriState.False, cmd.LogParameters)
        End Sub

#End Region

#Region "Init - script parsing with real scripts"

        Private ReadOnly TestScriptsPath As String = IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\..\..\TestScripts\")

        <Fact>
        Public Sub Init_WithParsing_PopulatesParsedSynopsis()
            Dim cmd As New PSCmd()
            Dim scriptPath = IO.Path.GetFullPath(IO.Path.Combine(TestScriptsPath, "ValidScript.ps1"))
            cmd.Script = scriptPath

            cmd.Init("", True)

            Assert.Equal("This is a valid script.", cmd.ParsedSynopsis)
        End Sub

        <Fact>
        Public Sub Init_WithParsing_MergesParameters()
            Dim cmd As New PSCmd()
            Dim scriptPath = IO.Path.GetFullPath(IO.Path.Combine(TestScriptsPath, "ValidScript.ps1"))
            cmd.Script = scriptPath

            cmd.Init("", True)

            Assert.True(cmd.Parameters.Count >= 2)
        End Sub

#End Region

    End Class
End Namespace
