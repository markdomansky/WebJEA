Public Class MenuItem
    Public ID As String
    Public DisplayName As String
    Public Description As String

    Public Function Uri() As String
        Return "?cmdid=" & ID
    End Function
End Class
