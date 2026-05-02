Imports System
Imports Xunit
Imports WebJEA

Namespace WebJEA_UnitTests_VBNET
    Public Class ConfigTests

#Region "Default property values"

        <Fact>
        Public Sub NewConfig_LogParameters_DefaultTrue()
            Dim config As New Config()
            Assert.True(config.LogParameters)
        End Sub

        <Fact>
        Public Sub NewConfig_SendTelemetry_DefaultTrue()
            Dim config As New Config()
            Assert.True(config.SendTelemetry)
        End Sub

        <Fact>
        Public Sub NewConfig_HtmlLanguage_DefaultEnUS()
            Dim config As New Config()
            Assert.Equal("en-US", config.HtmlLanguage)
        End Sub

        <Fact>
        Public Sub NewConfig_ShowVerbose_DefaultTrue()
            Dim config As New Config()
            Assert.True(config.ShowVerbose)
        End Sub

        <Fact>
        Public Sub NewConfig_PermittedGroups_IsEmptyList()
            Dim config As New Config()
            Assert.NotNull(config.PermittedGroups)
            Assert.Empty(config.PermittedGroups)
        End Sub

#End Region

#Region "IConfigProvider implementation"

        <Fact>
        Public Sub Config_ImplementsIConfigProvider()
            Dim config As New Config()
            Assert.IsAssignableFrom(Of IConfigProvider)(config)
        End Sub

        <Fact>
        Public Sub Config_PropertiesRoundTrip()
            Dim config As New Config()
            config.Title = "Test Title"
            config.LogParameters = False
            config.BasePath = "C:\test"
            config.SendTelemetry = False
            config.HtmlLanguage = "fr-FR"
            config.ShowVerbose = False

            Assert.Equal("Test Title", config.Title)
            Assert.False(config.LogParameters)
            Assert.Equal("C:\test", config.BasePath)
            Assert.False(config.SendTelemetry)
            Assert.Equal("fr-FR", config.HtmlLanguage)
            Assert.False(config.ShowVerbose)
        End Sub

#End Region

    End Class
End Namespace
