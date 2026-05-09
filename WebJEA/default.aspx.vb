Public Class _default
    Inherits System.Web.UI.Page

    Private dlog As NLog.Logger = NLog.LogManager.GetCurrentClassLogger()
    Private uinfo As New UserInfo(Page.User)
    Private objTelemetry As ITelemetryService = New Telemetry
    Private cmdSvc As New CommandService

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        dlog.Trace("Page: Start")

        objTelemetry.Add("sessionid", StringHash256(Session.SessionID))
        objTelemetry.Add("requestid", StringHash256(Guid.NewGuid().ToString()))

        'read the webjea config
        Dim configid As String = "configfile"
        dlog.Debug("Looking for web.config/applicationSettings/WebJEA.My.MySettings/settings=" & configid)
        cmdSvc.LoadConfig(WebJEA.My.Settings(configid), New GroupFinder)
        objTelemetry.Add("CommandCount", cmdSvc.Config.Commands.Count)
        objTelemetry.Add("PermGlobalCount", cmdSvc.Config.PermittedGroups.Count)

        'set html language from config
        htmlRoot.Attributes("lang") = cmdSvc.Config.HtmlLanguage

        dlog.Trace("IsGlobalUser: " & cmdSvc.Auth.IsGlobalUser(uinfo))
        objTelemetry.Add("IsGlobalUser", cmdSvc.Auth.IsGlobalUser(uinfo))

        'if cmdid is present in querystring or form, redirect to command.aspx preserving all data
        Dim cmdid As String = ReadGetPost("cmdid", "")
        If Not String.IsNullOrEmpty(cmdid) Then
            Dim redirectUrl As String = "command.aspx"
            If Request.QueryString.Count > 0 Then
                redirectUrl &= "?" & Request.QueryString.ToString()
            Else
                redirectUrl &= "?cmdid=" & Server.UrlEncode(cmdid)
            End If
            Response.Redirect(redirectUrl, True)
            Return
        End If

        'build display page
        Page.Title = String.Format("{0} - WebJEA", cmdSvc.Config.Title)
        lblTitleDash.Text = cmdSvc.Config.Title

        'add version to display
        Try
            lblVersionDash.Text = "v" + System.Reflection.Assembly.GetExecutingAssembly.GetName().Version.ToString(4)
            objTelemetry.Add("appedition", "CE")
            objTelemetry.Add("appversion", System.Reflection.Assembly.GetExecutingAssembly.GetName().Version.ToString(4))
        Catch ex As Exception
        End Try

        'show dashboard - command tiles for all commands available to the user
        Dim menuItems As List(Of MenuItem) = cmdSvc.Auth.GetMenu(uinfo)
        'If DashboardHtml is provided in config, display it above the tiles in its own container.
        'BalanceHtmlFragment ensures unclosed tags are closed and excess closing tags are discarded
        'so the fragment cannot leak structure into the card grid below.
        If Not String.IsNullOrEmpty(cmdSvc.Config.DashboardHtml) Then
            divDashboardHtml.InnerHtml = "<div class=""card border-0 rounded-0 w-100 dashboard-html""><div class=""card-body"">" & BalanceHtmlFragment(cmdSvc.Config.DashboardHtml) & "</div></div>"
        End If
        Dim sbTiles As New System.Text.StringBuilder
        For Each mi As MenuItem In menuItems
            sbTiles.Append("<div class=""tile"">")
            sbTiles.Append("<a class=""card h-100 text-decoration-none tile-card"" href=""")
            sbTiles.Append(Server.HtmlEncode(mi.Uri()))
            sbTiles.Append(""">")
            sbTiles.Append("<div class=""card-body"">")
            sbTiles.Append("<p class=""card-title fw-semibold"">")
            sbTiles.Append(Server.HtmlEncode(mi.DisplayName))
            sbTiles.Append("</p>")
            'Include synopsis (may contain HTML) below title
            If Not String.IsNullOrEmpty(mi.Synopsis) Then
                sbTiles.Append("<p class=""card-synopsis"">")
                sbTiles.Append(mi.Synopsis)
                sbTiles.Append("</p>")
            End If
            If Not String.IsNullOrEmpty(mi.Description) Then
                sbTiles.Append("<p class=""card-text"">")
                sbTiles.Append(Server.HtmlEncode(mi.Description))
                sbTiles.Append("</p>")
            End If
            sbTiles.Append("</div></a></div>")
        Next
        divTileView.InnerHtml = sbTiles.ToString()
        objTelemetry.AddIDs(uinfo.DomainSID, uinfo.DomainDNSRoot, "", uinfo.UserName)

        dlog.Trace("Page: End")

    End Sub


    ' Closes any unclosed HTML tags and discards excess closing tags so that
    ' an arbitrary HTML snippet cannot break the surrounding page structure.
    Private Function BalanceHtmlFragment(html As String) As String
        Dim voidTags As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase) From {
            "area", "base", "br", "col", "embed", "hr", "img", "input",
            "link", "meta", "param", "source", "track", "wbr"
        }
        Dim stack As New Stack(Of String)()
        Dim result As New System.Text.StringBuilder()
        Dim tagRx As New System.Text.RegularExpressions.Regex(
            "<(/?)([a-zA-Z][a-zA-Z0-9]*)([^>]*)(/?)>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase Or
            System.Text.RegularExpressions.RegexOptions.Compiled)
        Dim pos As Integer = 0
        For Each m As System.Text.RegularExpressions.Match In tagRx.Matches(html)
            result.Append(html.Substring(pos, m.Index - pos))
            pos = m.Index + m.Length
            Dim isClosing As Boolean = m.Groups(1).Value = "/"
            Dim tagName As String = m.Groups(2).Value.ToLowerInvariant()
            Dim isSelfClosing As Boolean = m.Groups(4).Value = "/" OrElse voidTags.Contains(tagName)
            If isClosing Then
                If stack.Count > 0 AndAlso stack.Peek() = tagName Then
                    stack.Pop()
                    result.Append(m.Value)
                ElseIf stack.Contains(tagName) Then
                    ' Close intermediate open tags and then this one
                    Do While stack.Count > 0
                        Dim top As String = stack.Pop()
                        result.Append("</" & top & ">")
                        If top = tagName Then Exit Do
                    Loop
                End If
                ' else: excess closing tag with no matching open – discard it
            ElseIf isSelfClosing Then
                result.Append(m.Value)
            Else
                stack.Push(tagName)
                result.Append(m.Value)
            End If
        Next
        If pos < html.Length Then result.Append(html.Substring(pos))
        ' Close any tags still open at the end of the fragment
        Do While stack.Count > 0
            result.Append("</" & stack.Pop() & ">")
        Loop
        Return result.ToString()
    End Function

    Private Function ReadGetPost(param As String, DefaultValue As String) As String
        If Page.Request.Form(param) IsNot Nothing Then
            Return Page.Request.Form(param)
        ElseIf Page.Request.QueryString(param) IsNot Nothing Then
            Return Page.Request.QueryString(param)
        Else
            Return DefaultValue
        End If
    End Function

    Private Sub _default_Init(sender As Object, e As EventArgs) Handles Me.Init
        ViewStateUserKey = Session.SessionID
    End Sub

    Private Sub _default_LoadComplete(sender As Object, e As EventArgs) Handles Me.LoadComplete
        If cmdSvc.Config.SendTelemetry Then
            objTelemetry.SendTelemetry()
        End If
    End Sub
End Class