Public Class OutputRenderer
    Private dlog As NLog.Logger = NLog.LogManager.GetCurrentClassLogger()

    Public Function ConvertToHTML(OutputData As Queue(Of PSEngine.OutputData)) As String
        Const NLOGPREFIX As String = "WEBJEA:"
        Dim outputstr As String = ""

        For Each line As PSEngine.OutputData In OutputData

            If line.Content.StartsWith(NLOGPREFIX) Then
                dlog.Info(line.Content.Substring(NLOGPREFIX.Length).Trim())
            Else

                Select Case line.OutputType
                    Case PSEngine.OutputType.Debug
                        outputstr += EncodeOutput("DEBUG: " & line.Content, "psdebug")
                    Case PSEngine.OutputType.Err
                        outputstr += EncodeOutput(line.Content, "pserror")
                    Case PSEngine.OutputType.Warn
                        outputstr += EncodeOutput("WARNING: " & line.Content, "pswarning")
                    Case PSEngine.OutputType.Info
                        outputstr += EncodeOutput(line.Content, "psoutput")
                    Case PSEngine.OutputType.Verbose
                        outputstr += EncodeOutput("VERBOSE: " & line.Content, "psverbose")
                    Case PSEngine.OutputType.Output
                        outputstr += EncodeOutput(line.Content, "psoutput")
                    Case Else
                        outputstr += EncodeOutput(line.Content, "psoutput")
                End Select
            End If

        Next

        Return outputstr

    End Function

    Private Function EncodeOutput(input As String, baseclass As String) As String

        Dim output As String = input
        output = HttpContext.Current.Server.HtmlEncode(output)

        output = EncodeOutputTags(output)

        output = "<span class=""" & baseclass & """>" & output & "</span><br/>"
        Return output
    End Function

    Friend Function EncodeOutputTags(ByVal input As String) As String
        Dim rexopt As RegexOptions = RegexOptions.IgnoreCase + RegexOptions.Multiline
        Const rexA As String = "\[\[a\|(.+?)\|(.+?)\]\]"
        Const repA As String = "<a href='$1'>$2</a>"
        Dim rgxA As New Regex(rexA, rexopt)
        Const rexSpan As String = "\[\[span\|(.+?)\|(.+?)\]\]"
        Const repSpan As String = "<span Class='$1'>$2</span>"
        Dim rgxSpan As New Regex(rexSpan, rexopt)
        Const rexImg As String = "\[\[img\|(.*?)\|(.+?)\]\]"
        Const repImg As String = "<img class='$1' src='$2' />"
        Dim rgxImg As New Regex(rexImg, rexopt)

        Dim idx As Int32 = input.LastIndexOf("[[")
        While idx > -1
            input = rgxA.Replace(input, repA, 1, idx)
            input = rgxSpan.Replace(input, repSpan, 1, idx)
            input = rgxImg.Replace(input, repImg, 1, idx)
            If (idx > 0) Then
                idx = input.LastIndexOf("[[", idx - 1)
            Else
                idx = -1
            End If

        End While

        Return input
    End Function

End Class
