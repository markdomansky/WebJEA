Public Class _default
    Inherits System.Web.UI.Page
    Dim cmdid As String

    'TODO: 7- Add ParameterSet support?


    'advanced functions should be able to retrieve the get-help and parameter data, then permit overriding

    'cache is the same format, and might contain a bit more, but it also includes the stuff we've calculated from other inputs (say by looking at parameters from advanced functions)
    '-probably has a lifetime in it, like 30 minutes or an hour
    'will need to have credential specified in app pool

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        'instantiate NLog with the config from nlog.config
        dlog = NLog.LogManager.GetCurrentClassLogger()
        dlog.Trace("Page: Start")

        objTelemetry.Add("sessionid", StringHash256(Session.SessionID)) 'to correlate one user's activities
        objTelemetry.Add("requestid", StringHash256(Guid.NewGuid().ToString())) 'to correlate multiple telemetry from the same page request

        uinfo = New UserInfo

        Dim psweb = New PSWebHelper

        'read the webjea config
        Dim configid As String = "configfile"
        dlog.Debug("Looking for web.config/applicationSettings/WebJEA.My.MySettings/settings=" & configid)
        Dim configstr As String = GetFileContent(WebJEA.My.Settings(configid))
        Try
            cfg = JsonConvert.DeserializeObject(Of WebJEA.Config)(configstr)
            objTelemetry.Add("CommandCount", cfg.Commands.Count)
            objTelemetry.Add("PermGlobalCount", cfg.PermittedGroups.Count)
        Catch
            Throw New Exception("Could not read config file")
        End Try

        'TODO: 9 - Improve JSON read process.  The current system is a hack, but it does work.

        'TODO: 5 - consider, using cached config, and then check for changes, and reload if appropriate

        'parse group info
        Try
            cfg.InitGroups()
        Catch
            Throw New Exception("Could not initialize groups")
        End Try

        dlog.Trace("IsGlobalUser: " & cfg.IsGlobalUser(uinfo))
        objTelemetry.Add("IsGlobalUser", cfg.IsGlobalUser(uinfo))

        'determine which cmds the user has access to
        Dim menuitems As List(Of MenuItem) = cfg.GetMenu(uinfo)

        'check to see if the user requests a specific command
        cmdid = ReadGetPost("cmdid", cfg.DefaultCommandId) 'doesn't matter if they have access here
        'if the user requested a page they don't have access to, send them to defaultid
        'user should always have access to defaultid.
        'If you don't want them to have access to defaultid, restrict access using ntfs perms
        If Not cfg.IsCommandAvailable(uinfo, cmdid) Then
            dlog.Warn("User " & uinfo.UserName & " requested page they don't have access to " & cmdid & ". redirecting to " & cfg.DefaultCommandId)
            cmdid = cfg.DefaultCommandId
        End If
        cfg.Init(cmdid) 'json's deserialize doesn't can't call new with parameters, so we do all the stuff we should do during the deserialize process.

        'build display page
        lblTitle.Text = cfg.Title

        'build menu
        lvMenu.DataSource = cfg.GetMenuDataTable(uinfo, cmdid)
        lvMenu.DataBind()

        'add version to display
        Try
            lblVersion.Text = " v" + System.Reflection.Assembly.GetExecutingAssembly.GetName().Version.ToString(3)
        Catch ex As Exception
        End Try


        'if the user has access to the requested command, build the display, otherwise display nothing
        Dim cmd As PSCmd = cfg.GetCommand(uinfo, cmdid)

        If cmd Is Nothing Then
            objTelemetry.AddIDs(uinfo.DomainSID, uinfo.DomainDNSRoot, cmdid, uinfo.UserName, Permitted:=False)

            divCmdBody.InnerText = "You do not have access to this command."
            dlog.Error("User " & uinfo.UserName & " requested cmdid " & cmdid & " that does not exist (or they don't have access to)")
        Else
            objTelemetry.AddIDs(uinfo.DomainSID, uinfo.DomainDNSRoot, cmd.ID, uinfo.UserName)
            objTelemetry.Add("PermCount", cmd.PermittedGroups.Count)
            objTelemetry.Add("ParamCount", cmd.Parameters.Count)

            'build display
            lblCmdTitle.Text = cmd.DisplayName
            If cmd.Synopsis <> "" Then
                lblCmdSynopsis.Text = cmd.Synopsis
                lblCmdDescription.Text = cmd.Description
            Else
                lblCmdSynopsis.Text = cmd.Description
            End If
            If cmd.Description = "" Or cmd.Synopsis = "" Then
                btnMore.Visible = False
            End If

            Dim pscontrols As List(Of HtmlControl) = psweb.NewControl(Page, cmd.Parameters)

            'psweb.AddControls(pscontrols, frmMain, btnRun)
            psweb.AddControls(pscontrols, frmMain, divParameters)


            If String.IsNullOrEmpty(cmd.OnloadScript) Then
                'hide the onload section
                panelOnload.Attributes("class") = panelOnload.Attributes("class") & " collapse"
            Else 'run the script and display it
                Dim ps As New PSEngine
                ps.Script = cmd.OnloadScript
                ps.LogParameters = cmd.LogParameters
                ps.Run()
                objTelemetry.AddRuntime(ps.Runtime, isOnload:=True)

                consoleOnload.InnerHtml = psweb.ConvertToHTML(ps.getOutputData)
                ps = Nothing

            End If

            If (String.IsNullOrEmpty(cmd.Script)) Then
                panelInput.Visible = False
            End If

            'hide output until submit
            panelOutput.Attributes("class") = panelOutput.Attributes("class") & " collapse"


        End If


        dlog.Trace("Page: End")

    End Sub



    Protected Sub btnRun_Click(sender As Object, e As EventArgs) Handles btnRun.Click

        uinfo = New UserInfo
        'dlog.Trace("Timeout: " & HttpContext.Current.Server.ScriptTimeout)
        'HttpContext.Current.Server.ScriptTimeout = 6
        'dlog.Trace("Timeout: " & HttpContext.Current.Server.ScriptTimeout)

        'display the output panel now and set focus
        panelOutput.Attributes("class") = panelOutput.Attributes("class").Replace("collapse", "")
        ClientScript.RegisterStartupScript(Page.GetType(), "hash", "location.hash='#panelOutput';", True)

        'TODO: verify user has access to this command

        Dim psweb As New PSWebHelper
        Dim ps As New PSEngine
        Dim cmd As PSCmd

        'verify the user has access to the cmd they want to use
        If Not cfg.IsCommandAvailable(uinfo, cmdid) Then
            dlog.Warn("User " & uinfo.UserName & " tried submitting a page they don't have access to " & cmdid & ".")
            consoleOutput.Text = "You do not have access to the page you requested."
            Return
        End If
        'get the script config
        cmd = cfg.GetCommand(uinfo, cmdid)

        'TODO: validate if there is a script to run, fail if not
        ps.Script = cmd.Script
        ps.LogParameters = cmd.LogParameters
        ps.Parameters = psweb.getParameters(cmd, Page)
        objTelemetry.Add("ParamUsed", ps.Parameters.Count)

        ps.Run()
        objTelemetry.AddRuntime(ps.Runtime)

        consoleOutput.Text = psweb.ConvertToHTML(ps.getOutputData)
        ps = Nothing

    End Sub


    Private Function ReadGetPost(param As String, DefaultValue As String) As String
        'check both GET and POST for parameter, if not found, return defaultvalue
        'prefer post over get for security

        If Request.Form(param) IsNot Nothing Then
            Return Request.Form(param)
        ElseIf Request.QueryString(param) IsNot Nothing Then
            Return Request.QueryString(param)
        Else
            Return DefaultValue
        End If

    End Function

    Private Sub _default_Init(sender As Object, e As EventArgs) Handles Me.Init
        ViewStateUserKey = Session.SessionID
    End Sub

    Private Sub _default_LoadComplete(sender As Object, e As EventArgs) Handles Me.LoadComplete
        If cfg.SendTelemetry Then
            objTelemetry.SendTelemetry()
        End If
    End Sub
End Class