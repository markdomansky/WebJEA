Public Class MenuItem
    Public Property ID As String
    Public Property DisplayName As String
    Public Property Description As String
    Public Property Synopsis As String

    Public Function Uri() As String
        Return "command.aspx?cmdid=" & ID
    End Function
End Class
