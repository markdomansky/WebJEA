Imports System
Imports Xunit
Imports WebJEA

Namespace WebJEA_UnitTests_VBNET
    Public Class MenuItemTests

        <Fact>
        Public Sub Uri_ReturnsQueryStringWithId()
            Dim mi As New MenuItem()
            mi.ID = "test-cmd"

            Assert.Equal("command.aspx?cmdid=test-cmd", mi.Uri())
        End Sub

        <Fact>
        Public Sub Uri_EmptyId_ReturnsQueryStringWithEmptyValue()
            Dim mi As New MenuItem()
            mi.ID = ""

            Assert.Equal("command.aspx?cmdid=", mi.Uri())
        End Sub

        <Fact>
        Public Sub Properties_CanBeSetAndRetrieved()
            Dim mi As New MenuItem()
            mi.ID = "my-id"
            mi.DisplayName = "My Display Name"
            mi.Description = "My Description"

            Assert.Equal("my-id", mi.ID)
            Assert.Equal("My Display Name", mi.DisplayName)
            Assert.Equal("My Description", mi.Description)
        End Sub

        <Fact>
        Public Sub Uri_SpecialCharactersInId_AreNotEncoded()
            Dim mi As New MenuItem()
            mi.ID = "cmd with spaces"

            Assert.Equal("command.aspx?cmdid=cmd with spaces", mi.Uri())
        End Sub

    End Class
End Namespace
