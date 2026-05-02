Public Class ConfigCmd

    Public prvID As String
    Public Property DisplayName As String
    Public Property LogParameters As TriState = TriState.UseDefault
    Public Property Synopsis As String
    Public Property Description As String
    Public Property OnloadScript As String
    Public Property Script As String
    Public Property PermittedGroups As New List(Of String)()

    Public Sub New()
    End Sub

    Public Property ID As String
        Get
            Return prvID
        End Get
        Set(value As String)
            prvID = value.ToLower()
        End Set
    End Property

    Public Function GetMenuItem() As MenuItem
        Dim mi As New MenuItem
        mi.ID = ID
        mi.DisplayName = If(DisplayName IsNot Nothing, DisplayName, ID)
        mi.Description = Description
        mi.Synopsis = Synopsis
        Return mi
    End Function

End Class
