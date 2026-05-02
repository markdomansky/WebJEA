Imports Microsoft.VisualBasic

Public Module Helpers
    Public Function CoalesceString(ByVal ParamArray arguments As String()) As String
        Dim argument As String
        For Each argument In arguments
            If Not argument Is Nothing Then
                Return argument
            End If
        Next

        Return Nothing
    End Function

    Public Function GetFileContent(filename As String) As String
        If IO.File.Exists(filename) Then
            Using fsobj As New System.IO.FileStream(filename, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.ReadWrite)
                Using streamobj As New IO.StreamReader(fsobj)
                    Return streamobj.ReadToEnd()
                End Using
            End Using
        End If

        Return Nothing

    End Function

    Public Function StringHash256(strin As String) As String

        Dim uEncode As New UnicodeEncoding()

        Dim bytin() As Byte = uEncode.GetBytes(strin)

        Using sha As System.Security.Cryptography.SHA256 = System.Security.Cryptography.SHA256.Create()
            Dim hash() As Byte = sha.ComputeHash(bytin)
            Return ByteArrayToHexString(hash)
        End Using

    End Function

    Private Function ByteArrayToHexString(ByVal bytes_Input As Byte()) As String
        Dim strTemp As New StringBuilder(bytes_Input.Length * 2)
        For Each b As Byte In bytes_Input
            strTemp.Append(b.ToString("X02"))
        Next
        Return strTemp.ToString()
    End Function

End Module