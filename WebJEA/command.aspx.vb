Public Class _command
    Inherits System.Web.UI.Page
    Protected cmdid As String

    Private dlog As NLog.Logger = NLog.LogManager.GetCurrentClassLogger()
    Private uinfo As New UserInfo(Page.User)
    Private objTelemetry As ITelemetryService = New Telemetry
    Private cmdSvc As New CommandService
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        dlog.Trace("Page: Start")

        objTelemetry.Add("sessionid", StringHash256(Session.SessionID))
        objTelemetry.Add("requestid", StringHash256(Guid.NewGuid().ToString()))

        Dim psweb As New ControlBuilder

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

        'check to see if the user requests a specific command (empty = redirect to dashboard)
        cmdid = ReadGetPost("cmdid", "")
        If Not String.IsNullOrEmpty(cmdid) Then
            cmdid = cmdSvc.ResolveCommandId(uinfo, cmdid)
        End If

        'if no command requested, redirect back to dashboard
        If String.IsNullOrEmpty(cmdid) Then
            Response.Redirect("default.aspx", True)
            Return
        End If

        'build display page
        lblTitle.Text = cmdSvc.Config.Title
        lblTitleDesktop.Text = cmdSvc.Config.Title

        'build menu
        lvMenu.DataSource = cmdSvc.Auth.GetMenuDataTable(uinfo, cmdid)
        lvMenu.DataBind()

        'add version to display
        Try
            lblVersion.Text = "v" + System.Reflection.Assembly.GetExecutingAssembly.GetName().Version.ToString(4)
            objTelemetry.Add("appedition", "CE")
            objTelemetry.Add("appversion", System.Reflection.Assembly.GetExecutingAssembly.GetName().Version.ToString(4))
        Catch ex As Exception
        End Try

        'show command view
        Dim cmd As ConfigCmd = cmdSvc.GetCommand(uinfo, cmdid)

        If cmd Is Nothing Then
            objTelemetry.AddIDs(uinfo.DomainSID, uinfo.DomainDNSRoot, cmdid, uinfo.UserName, Permitted:=False)
            divCmdBody.InnerText = "You do not have access to this command."
            dlog.Error("User " & uinfo.UserName & " requested cmdid " & cmdid & " that does not exist (or they don't have access to)")
        Else
            Dim scriptCmd As PSCmd = cmdSvc.GetScriptCmd(cmdid)
            Dim onloadCmd As PSCmd = cmdSvc.GetOnloadCmd(cmdid)

            objTelemetry.AddIDs(uinfo.DomainSID, uinfo.DomainDNSRoot, cmd.ID, uinfo.UserName)
            objTelemetry.Add("PermCount", cmd.PermittedGroups.Count)
            objTelemetry.Add("ParamCount", If(scriptCmd IsNot Nothing, scriptCmd.Parameters.Count, 0))

            'build display
            Page.Title = String.Format("{0} - {1} - WebJEA", cmd.DisplayName, cmdSvc.Config.Title)
            lblCmdTitle.InnerText = cmd.DisplayName
            Dim hasSynopsis As Boolean = Not String.IsNullOrEmpty(cmd.Synopsis)
            Dim hasDescription As Boolean = Not String.IsNullOrEmpty(cmd.Description)
            If hasSynopsis AndAlso hasDescription Then
                ' Both present: show accordion
                SynopsisAndDescription.Visible = True
                divSingleInfo.Visible = False
                lblCmdSynopsis.Text = cmd.Synopsis
                lblCmdDescription.Text = cmd.Description
            ElseIf hasSynopsis OrElse hasDescription Then
                ' Only one present: show simple div, hide accordion
                SynopsisAndDescription.Visible = False
                divSingleInfo.Visible = True
                lblSingleInfo.Text = If(hasSynopsis, cmd.Synopsis, cmd.Description)
            Else
                ' Neither present: hide both
                SynopsisAndDescription.Visible = False
                divSingleInfo.Visible = False
            End If

            Dim pscontrols As List(Of HtmlControl) = psweb.NewControl(Page, If(scriptCmd IsNot Nothing, scriptCmd.Parameters, New List(Of PSCmdParam)()))

            'Add verbose control for global users (members of top-level permitted groups)
            If cmdSvc.Auth.IsGlobalUser(uinfo) Then
                pscontrols.Add(psweb.NewVerboseControl(Page))
            End If

            psweb.AddControls(pscontrols, frmMain, divParameters)


            If onloadCmd Is Nothing Then
                'hide the onload section
                panelOnload.Attributes("class") = panelOnload.Attributes("class") & " collapse"
            Else
                panelOnload.Attributes("data-has-onload") = "true"
            End If

            If scriptCmd Is Nothing Then
                panelInput.Visible = False
            End If

            'hide output until submit
            panelOutput.Attributes("class") = panelOutput.Attributes("class") & " collapse"

        End If

        dlog.Trace("Page: End")

    End Sub


    Private Function ReadGetPost(param As String, DefaultValue As String) As String
        If Page.Request.Form(param) IsNot Nothing Then
            Return Page.Request.Form(param)
        ElseIf Page.Request.QueryString(param) IsNot Nothing Then
            Return Page.Request.QueryString(param)
        Else
            Return DefaultValue
        End If
    End Function

    Private Sub _command_Init(sender As Object, e As EventArgs) Handles Me.Init
        ViewStateUserKey = Session.SessionID
    End Sub

    Private Sub _command_LoadComplete(sender As Object, e As EventArgs) Handles Me.LoadComplete
        If cmdSvc.Config.SendTelemetry Then
            objTelemetry.SendTelemetry()
        End If
    End Sub
End Class
