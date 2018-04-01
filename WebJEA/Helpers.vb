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
        dlog.Trace("GetFileContent: " & filename)
        If IO.File.Exists(filename) Then
            Dim fileobj As New System.IO.StreamReader(filename)

            Dim contentstr As String = fileobj.ReadToEnd
            fileobj.Close()
            fileobj = Nothing
            Return contentstr
        End If

        Return Nothing

    End Function

    Public Sub SendUsage(DomainSid As String, DomainDNSRoot As String, ScriptID As String, UserID As String, ParamCount As Integer, isOnload As Boolean, SecondsRuntime As Single)
        If globalSettings(globalKeys.aws_enabled) = False Then Return 'disable in code

        Dim wints As DateTime = DateTime.UtcNow
        Dim ts As Integer = (wints - New DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds
        Dim version As String = "1" 'version of string format
        Dim oid As String = UsageHash(DomainSid & ";" & DomainDNSRoot.ToUpper())
        Dim sid As String = UsageHash(oid & ";" & ScriptID.ToUpper())
        Dim uid As String = UsageHash(oid & ";" & UserID.ToUpper())
        Dim isOnloadInt As Integer = Math.Abs(Convert.ToInt16(isOnload)) '0=false, 1=true
        Dim secondsRuntimeStr As String = (Math.Ceiling(SecondsRuntime * 10D) / 10D).ToString()
        Dim msg As String = wints.ToString("yyyy-MM-dd hh:mm:ss") & "," & ts & "," & version & "," & oid & "," & sid & "," & uid & "," & ParamCount & "," & isOnloadInt & "," & secondsRuntimeStr


        Try
            'Dim cred As Amazon.Runtime.AWSCredentials = New Amazon.Runtime.AnonymousAWSCredentials
            Dim cred As Amazon.Runtime.AWSCredentials = New Amazon.Runtime.BasicAWSCredentials(globalSettings(globalKeys.aws_key), globalSettings(globalKeys.aws_keysec))

            Dim conf As New Amazon.SQS.AmazonSQSConfig
            conf.Timeout = New TimeSpan(0, 0, 5)
            conf.ServiceURL = globalSettings(globalKeys.aws_serviceUrl)

            Dim client As New Amazon.SQS.AmazonSQSClient(cred, conf)

            Dim req As New Amazon.SQS.Model.SendMessageRequest
            req.QueueUrl = globalSettings(globalKeys.aws_queueUrl)
            req.MessageBody = msg

            Dim resp As Amazon.SQS.Model.SendMessageResponse = client.SendMessage(req)
        Catch
            'if it errors, do nothing
        End Try



    End Sub

    Public Function UsageHash(strin As String) As String

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