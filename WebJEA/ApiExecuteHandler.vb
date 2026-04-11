Imports System.Web
Imports System.Management.Automation
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Public Class ApiExecuteHandler
    Implements IHttpHandler

    Private dlog As NLog.Logger = NLog.LogManager.GetCurrentClassLogger()

    Public ReadOnly Property IsReusable As Boolean Implements IHttpHandler.IsReusable
        Get
            Return False
        End Get
    End Property

    Public Sub ProcessRequest(context As HttpContext) Implements IHttpHandler.ProcessRequest
        context.Response.ContentType = "application/json"

        Try
            If context.Request.HttpMethod <> "POST" Then
                WriteResponse(context, 405, "Method Not Allowed. Use POST.")
                Return
            End If

            Dim contentType As String = If(context.Request.ContentType, "")
            If Not contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) Then
                WriteResponse(context, 415, "Unsupported Media Type. Use application/json.")
                Return
            End If

#If Not DEBUG Then
            If Not context.Request.IsAuthenticated Then
                context.Response.StatusCode = 401
                WriteResponse(context, 401, "Authentication required.")
                Return
            End If
#End If

            Dim body As String
            Using reader As New IO.StreamReader(context.Request.InputStream, context.Request.ContentEncoding)
                body = reader.ReadToEnd()
            End Using

            Dim requestObj As JObject
            Try
                requestObj = JObject.Parse(body)
            Catch ex As JsonReaderException
                WriteResponse(context, 400, "Invalid JSON in request body.")
                Return
            End Try

            Dim cmdid As String = If(requestObj.Value(Of String)("cmdid"), "")
            If String.IsNullOrWhiteSpace(cmdid) Then
                WriteResponse(context, 400, "cmdid is required.")
                Return
            End If

            ' Sanitize cmdid to alphanumeric, hyphens, underscores, and dots only
            If Not System.Text.RegularExpressions.Regex.IsMatch(cmdid, "^[a-zA-Z0-9_\-\.]+$") Then
                WriteResponse(context, 400, "Invalid cmdid format.")
                Return
            End If

            Dim requestParams As JObject = Nothing
            Dim paramToken As JToken = Nothing
            If requestObj.TryGetValue("parameters", paramToken) Then
                If paramToken.Type = JTokenType.Object Then
                    requestParams = CType(paramToken, JObject)
                ElseIf paramToken.Type <> JTokenType.Null Then
                    WriteResponse(context, 400, "parameters must be a JSON object.")
                    Return
                End If
            End If

            ' Load config (same pattern as default.aspx.vb)
            Dim grpfinder As New GroupFinder
            Dim cfg As Config

            Dim configstr As String = GetFileContent(My.Settings("configfile"))
            Try
                cfg = JsonConvert.DeserializeObject(Of Config)(configstr)
            Catch ex As Exception
                dlog.Error("API: Could not read config file: " & ex.Message)
                WriteResponse(context, 500, "Internal server error: configuration failure.")
                Return
            End Try

            Try
                cfg.InitGroups(grpfinder)
            Catch ex As Exception
                dlog.Error("API: Could not initialize groups: " & ex.Message)
                WriteResponse(context, 500, "Internal server error: group initialization failure.")
                Return
            End Try

            ' Build UserInfo from the authenticated Windows principal
            Dim uinfo As New UserInfo(context.User)

            ' Check authorization
            If Not cfg.IsCommandAvailable(uinfo, cmdid) Then
                dlog.Warn("API: User " & uinfo.UserName & " denied access to cmdid " & cmdid)
                WriteResponse(context, 403, "Access denied. Command not available or insufficient permissions.")
                Return
            End If

            cfg.Init(cmdid)
            Dim cmd As PSCmd = cfg.GetCommand(uinfo, cmdid)

            If cmd Is Nothing Then
                dlog.Error("API: Command " & cmdid & " not found after authorization check")
                WriteResponse(context, 403, "Command not found.")
                Return
            End If

            ' Validate and build parameters
            Dim psParams As New Dictionary(Of String, Object)
            Dim validationErrors As New List(Of String)

            If cmd.Parameters IsNot Nothing Then
                For Each param As PSCmdParam In cmd.Parameters
                    ' Handle WEBJEA* internal parameters
                    If param.Name.ToUpper().StartsWith("WEBJEA") Then
                        If param.Name.ToUpper() = "WEBJEAUSERNAME" Then
                            psParams.Add(param.Name, uinfo.UserName)
                        ElseIf param.Name.ToUpper() = "WEBJEAHOSTNAME" Then
                            psParams.Add(param.Name, context.Request.UserHostName)
                        Else
                            dlog.Warn("API: Parameter '" & param.Name & "' is not a recognized internal parameter.")
                        End If
                        Continue For
                    End If

                    Dim rawValue As JToken = Nothing
                    Dim hasValue As Boolean = False
                    If requestParams IsNot Nothing Then
                        ' Perform a case-insensitive lookup so incoming JSON parameter names are not case sensitive
                        hasValue = requestParams.TryGetValue(param.Name, StringComparison.OrdinalIgnoreCase, rawValue) AndAlso rawValue.Type <> JTokenType.Null
                    End If

                    ' Check mandatory
                    If param.IsMandatory AndAlso Not hasValue Then
                        validationErrors.Add("Parameter '" & param.Name & "' is required.")
                        Continue For
                    End If

                    If Not hasValue Then Continue For

                    ' Convert value based on param type
                    Dim convertedValue As Object = Nothing
                    Try
                        convertedValue = ConvertParameterValue(param, rawValue)
                    Catch ex As Exception
                        validationErrors.Add("Parameter '" & param.Name & "': " & ex.Message)
                        Continue For
                    End Try

                    ' Validate against rules
                    Dim paramErrors As List(Of String) = ValidateParameter(param, convertedValue)
                    If paramErrors.Count > 0 Then
                        validationErrors.AddRange(paramErrors)
                        Continue For
                    End If

                    psParams.Add(param.Name, convertedValue)
                Next
            End If

            If validationErrors.Count > 0 Then
                WriteResponse(context, 400, "Parameter validation failed.", Nothing, validationErrors)
                Return
            End If

            ' Execute the command
            Dim ps As New PSEngine
            ps.Script = cmd.Script
            ps.LogParameters = cmd.LogParameters
            ps.PipeToOutString = False
            ps.Parameters = psParams

            'Set WebJEA context for the PowerShell runspace
            ps.WebJEAUserName = uinfo.UserName
            ps.WebJEAHostName = context.Request.UserHostName

            ps.Run()

            ' Build messages from streams (info, verbose, warning, error, debug — in order)
            Dim messages As New JArray
            Dim streamData As Queue(Of PSEngine.OutputData) = ps.getOutputData()
            While streamData.Count > 0
                Dim item As PSEngine.OutputData = streamData.Dequeue()
                ' Skip output-type items, those go to the output field
                If item.OutputType = PSEngine.OutputType.Output Then Continue While
                Dim msgObj As New JObject
                msgObj("stream") = item.OutputType.ToString().ToLower()
                msgObj("message") = item.Content
                messages.Add(msgObj)
            End While

            ' Serialize output objects
            Dim outputArray As New JArray
            For Each psObj As PSObject In ps.GetOutputObjects()
                outputArray.Add(ConvertPSObjectToJToken(psObj))
            Next

            Dim output As JToken
            If outputArray.Count = 1 Then
                output = outputArray(0)
            ElseIf outputArray.Count = 0 Then
                output = JValue.CreateNull()
            Else
                output = outputArray
            End If

            Dim statusCode As Integer = If(ps.HasErrors, 206, 200)
            Dim statusMessage As String = If(ps.HasErrors, "Completed with errors in stream.", "OK")

            dlog.Info("API: Executed|" & cmdid & "|User=" & uinfo.UserName & "|Status=" & statusCode & "|Runtime=" & ps.Runtime)

            WriteResponse(context, statusCode, statusMessage, output, Nothing, messages)
            ps = Nothing

        Catch ex As Exception
            dlog.Error("API: Unhandled exception: " & ex.ToString())
            WriteResponse(context, 500, "Internal server error.")
        End Try
    End Sub

    Private Function ConvertParameterValue(param As PSCmdParam, rawValue As JToken) As Object
        If param.IsMultiValued Then
            ' Expect an array
            If rawValue.Type = JTokenType.Array Then
                Dim arr As JArray = CType(rawValue, JArray)
                Dim strList As New List(Of String)
                For Each item As JToken In arr
                    strList.Add(item.ToString())
                Next
                Return strList.ToArray()
            Else
                ' Single value provided, wrap in array
                Return New String() {rawValue.ToString()}
            End If
        End If

        Select Case param.ParamType
            Case PSCmdParam.ParameterType.PSBoolean
                If rawValue.Type = JTokenType.Boolean Then
                    Return rawValue.Value(Of Boolean)()
                End If
                Dim strVal As String = rawValue.ToString().ToLower()
                If strVal = "true" Or strVal = "1" Then Return True
                If strVal = "false" Or strVal = "0" Then Return False
                Throw New ArgumentException("Invalid boolean value.")

            Case PSCmdParam.ParameterType.PSInt
                If rawValue.Type = JTokenType.Integer Then
                    Return rawValue.Value(Of Integer)()
                End If
                Dim intVal As Integer
                If Integer.TryParse(rawValue.ToString(), intVal) Then Return intVal
                Throw New ArgumentException("Invalid integer value.")

            Case PSCmdParam.ParameterType.PSFloat
                If rawValue.Type = JTokenType.Float OrElse rawValue.Type = JTokenType.Integer Then
                    Return rawValue.Value(Of Double)()
                End If
                Dim dblVal As Double
                If Double.TryParse(rawValue.ToString(), dblVal) Then Return dblVal
                Throw New ArgumentException("Invalid numeric value.")

            Case PSCmdParam.ParameterType.PSDate
                Dim dtVal As DateTime
                If DateTime.TryParse(rawValue.ToString(), dtVal) Then Return dtVal
                Throw New ArgumentException("Invalid date value.")

            Case Else
                Return rawValue.ToString()
        End Select
    End Function

    Private Function ValidateParameter(param As PSCmdParam, value As Object) As List(Of String)
        Dim errors As New List(Of String)

        For Each valObj As PSCmdParamVal In param.ValidationObjects
            Select Case valObj.Type
                Case PSCmdParamVal.ValType.SetCol
                    If param.IsMultiValued AndAlso TypeOf value Is String() Then
                        For Each item As String In DirectCast(value, String())
                            If Not valObj.Options.Contains(item) Then
                                errors.Add("Parameter '" & param.Name & "': value '" & item & "' is not in the allowed set.")
                            End If
                        Next
                    Else
                        If Not valObj.Options.Contains(value.ToString()) Then
                            errors.Add("Parameter '" & param.Name & "': value '" & value.ToString() & "' is not in the allowed set.")
                        End If
                    End If

                Case PSCmdParamVal.ValType.Length
                    Dim strVal As String = value.ToString()
                    If strVal.Length < valObj.LowerLimit OrElse strVal.Length > valObj.UpperLimit Then
                        errors.Add("Parameter '" & param.Name & "': length must be between " & valObj.LowerLimit & " and " & valObj.UpperLimit & ".")
                    End If

                Case PSCmdParamVal.ValType.Range
                    Dim numVal As Double
                    If Double.TryParse(value.ToString(), numVal) Then
                        If numVal < valObj.LowerLimit OrElse numVal > valObj.UpperLimit Then
                            errors.Add("Parameter '" & param.Name & "': value must be between " & valObj.LowerLimit & " and " & valObj.UpperLimit & ".")
                        End If
                    End If

                Case PSCmdParamVal.ValType.Pattern
                    If Not System.Text.RegularExpressions.Regex.IsMatch(value.ToString(), valObj.Pattern) Then
                        errors.Add("Parameter '" & param.Name & "': value does not match the required pattern.")
                    End If

                Case PSCmdParamVal.ValType.Count
                    If param.IsMultiValued AndAlso TypeOf value Is String() Then
                        Dim arr As String() = DirectCast(value, String())
                        If arr.Length < valObj.LowerLimit OrElse arr.Length > valObj.UpperLimit Then
                            errors.Add("Parameter '" & param.Name & "': item count must be between " & valObj.LowerLimit & " and " & valObj.UpperLimit & ".")
                        End If
                    End If
            End Select
        Next

        Return errors
    End Function

    Private Function ConvertPSObjectToJToken(psObj As PSObject) As JToken
        If psObj Is Nothing Then Return JValue.CreateNull()

        Dim baseObj As Object = psObj.BaseObject

        ' Primitives and strings - serialize directly
        If TypeOf baseObj Is String OrElse TypeOf baseObj Is Boolean OrElse
           TypeOf baseObj Is Integer OrElse TypeOf baseObj Is Long OrElse
           TypeOf baseObj Is Double OrElse TypeOf baseObj Is Single OrElse
           TypeOf baseObj Is Decimal OrElse TypeOf baseObj Is DateTime OrElse
           TypeOf baseObj Is Byte OrElse TypeOf baseObj Is Short Then
            Return JToken.FromObject(baseObj)
        End If

        ' Hashtable
        If TypeOf baseObj Is Collections.Hashtable Then
            Dim ht As Collections.Hashtable = DirectCast(baseObj, Collections.Hashtable)
            Dim jobj As New JObject
            For Each key As Object In ht.Keys
                jobj(key.ToString()) = JToken.FromObject(If(ht(key), ""))
            Next
            Return jobj
        End If

        ' Arrays and collections
        If TypeOf baseObj Is Collections.IEnumerable AndAlso Not TypeOf baseObj Is String Then
            Dim jarr As New JArray
            For Each item In DirectCast(baseObj, Collections.IEnumerable)
                If TypeOf item Is PSObject Then
                    jarr.Add(ConvertPSObjectToJToken(DirectCast(item, PSObject)))
                Else
                    jarr.Add(JToken.FromObject(If(item, "")))
                End If
            Next
            Return jarr
        End If

        ' Complex objects with properties
        If psObj.Properties IsNot Nothing AndAlso psObj.Properties.Any() Then
            Dim jobj As New JObject
            For Each prop As PSPropertyInfo In psObj.Properties
                Try
                    Dim propVal As Object = prop.Value
                    If propVal Is Nothing Then
                        jobj(prop.Name) = JValue.CreateNull()
                    ElseIf TypeOf propVal Is PSObject Then
                        jobj(prop.Name) = ConvertPSObjectToJToken(DirectCast(propVal, PSObject))
                    Else
                        jobj(prop.Name) = JToken.FromObject(propVal)
                    End If
                Catch
                    jobj(prop.Name) = JValue.CreateNull()
                End Try
            Next
            Return jobj
        End If

        ' Fallback
        Return New JValue(psObj.ToString())
    End Function

    Private Sub WriteResponse(context As HttpContext, statusCode As Integer, statusMessage As String,
                              Optional output As JToken = Nothing,
                              Optional validationErrors As List(Of String) = Nothing,
                              Optional messages As JArray = Nothing)
        context.Response.StatusCode = statusCode

        Dim responseObj As New JObject
        responseObj("status") = statusCode
        responseObj("statusmessage") = statusMessage

        If output IsNot Nothing Then
            responseObj("output") = output
        Else
            responseObj("output") = JValue.CreateNull()
        End If

        If messages IsNot Nothing Then
            responseObj("messages") = messages
        ElseIf validationErrors IsNot Nothing Then
            Dim msgArr As New JArray
            For Each err As String In validationErrors
                Dim msgObj As New JObject
                msgObj("stream") = "error"
                msgObj("message") = err
                msgArr.Add(msgObj)
            Next
            responseObj("messages") = msgArr
        Else
            responseObj("messages") = New JArray()
        End If

        context.Response.Write(responseObj.ToString(Formatting.None))
    End Sub

End Class
