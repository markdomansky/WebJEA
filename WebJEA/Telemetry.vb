Imports System.Web.Hosting
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Web.Script.Serialization

Class Telemetry
    Private dlog As NLog.Logger = NLog.LogManager.GetCurrentClassLogger()

    Private Metrics As New Dictionary(Of String, Object)

    Public Sub Add(key As String, value As Object)
        If Metrics.Keys.Contains(key) Then
            Metrics(key) = value 'update
        Else
            Metrics.Add(key, value) 'add
        End If

    End Sub

    Public Sub Clear(key As String)
        Metrics.Clear()
    End Sub
    Public Sub Remove(key As String)
        If Metrics.Keys.Contains(key) Then
            Metrics.Remove(key)
        End If
    End Sub
    Public Sub SendTelemetry()
        dlog.Trace("SendTelemetry")
        'sends whatever telemetry we have
        Dim cts As New CancellationTokenSource
        HostingEnvironment.QueueBackgroundWorkItem(Sub() AddMetricsAndSendTelemetry())

    End Sub

    Public Sub AddIDs(DomainSid As String, DomainDNSRoot As String, ScriptID As String, UserID As String, Optional Permitted As Boolean = True)

        Dim oid As String = StringHash256(DomainSid & ";" & DomainDNSRoot.ToUpper())
#If DEBUG Then
        Add("orgid", "DEV")
#Else
        Add("orgid", oid)
#End If
        Add("scriptid", StringHash256(oid & ";" & ScriptID.ToUpper()))
        Add("userid", StringHash256(oid & ";" & UserID.ToUpper()))
        Add("permitted", Permitted)

    End Sub

    Public Sub AddIsOnload(state As Boolean)
        Add("IsOnload", state)
    End Sub

    Public Sub AddRuntime(SecondsRuntime As Single)

        Add("runtimesec", (Math.Ceiling(SecondsRuntime * 10D) / 10D).ToString()) 'round up to 1 decimal

    End Sub

    Private Sub AddMetricsAndSendTelemetry()
        AddSystemMetrics()
        AddTimeMetrics()
        SubmitToAWSQueue(Metrics)

    End Sub

    Private Sub SubmitToAWSQueue(dictobj As Dictionary(Of String, Object))
        Dim msg As String = ToJSON(dictobj)

        SubmitToAWSQueue(msg)
    End Sub

    Private Sub SubmitToAWSQueue(msg As String)
        If globalSettings(globalKeys.aws_enabled) = False Then Return 'disable in code

        Try
            'Dim cred As Amazon.Runtime.AWSCredentials = New Amazon.Runtime.AnonymousAWSCredentials
            dlog.Info("Sending Telemetry: " + msg)
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

    Private Function ToJSON(obj As Object) As String

        Dim serializer As New JavaScriptSerializer()
        serializer.RecursionLimit = 2
        Return serializer.Serialize(obj)

    End Function

    Private Sub AddTimeMetrics()

        Dim wints As DateTime = DateTime.UtcNow
        Dim ts As Integer = (wints - New DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds
        Dim version As String = "3" 'version of string format

        Add("wints", wints.ToString("yyyy-MM-dd hh:mm:ss"))
        Add("unixts", ts)
        Add("msgversion", version)

    End Sub


    Private Sub AddSystemMetrics()

        Add("CPUCount", Environment.ProcessorCount) 'cpu count
        Dim mgmtobjs As System.Management.ManagementObjectCollection = New System.Management.ManagementObjectSearcher("Select MaxClockSpeed from Win32_Processor").Get()
        For Each mgmtobj In mgmtobjs
            Add("CPUMhz", mgmtobj("maxclockspeed"))
        Next
        Add("OS", My.Computer.Info.OSFullName) 'os details
        Add("RAM", Math.Round(My.Computer.Info.TotalPhysicalMemory / 1024 / 1024 / 1024, 1)) 'GB ram


    End Sub

End Class
