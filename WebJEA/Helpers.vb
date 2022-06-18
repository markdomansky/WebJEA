Imports Microsoft.VisualBasic
Imports Microsoft.VisualBasic.Devices

Module Helpers
    Public Function CoalesceString(ByVal ParamArray arguments As String()) As String
        Dim argument As String
        For Each argument In arguments
            If Not argument Is Nothing Then
                Return argument
            End If
        Next

        ' No argument was found that wasn't null.
        Return Nothing
    End Function

    Public Function GetFileContent(filename As String) As String
        'Just a convenience wrapper for reading a file
        If IO.File.Exists(filename) Then
            Dim fsobj As System.IO.FileStream
            fsobj = New System.IO.FileStream(filename, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.ReadWrite)
            Dim streamobj As New IO.StreamReader(fsobj)

            Dim contentstr As String = streamobj.ReadToEnd()
            streamobj.Close()
            streamobj = Nothing
            fsobj.Close()
            fsobj = Nothing
            Return contentstr
        End If

        Return Nothing

    End Function

    Public Function StringHash256(strin As String) As String

        'Const rounds As Integer = 5

        Dim uEncode As New UnicodeEncoding()
        Dim sha As New System.Security.Cryptography.SHA256Managed()

        'get byte array of input
        Dim bytin() As Byte = uEncode.GetBytes(strin)

        'round 1
        Dim hash() As Byte = sha.ComputeHash(bytin)
        ''round 2+
        'For round As Integer = 2 To rounds
        '    hash = sha.ComputeHash(hash)
        'Next
        'return
        Return ByteArrayToHexString(hash)

    End Function

    Private Function ByteArrayToHexString(ByVal bytes_Input As Byte()) As String
        Dim strTemp As New StringBuilder(bytes_Input.Length * 2)
        For Each b As Byte In bytes_Input
            strTemp.Append(b.ToString("X02"))
        Next
        Return strTemp.ToString()
    End Function

End Module