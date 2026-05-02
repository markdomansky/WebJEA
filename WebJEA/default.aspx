<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="default.aspx.vb" Inherits="WebJEA._default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml" id="htmlRoot" runat="server">
<head runat="server">
    <meta charset="utf-8" />
    <meta http-equiv="X-UA-Compatible" content="IE=edge" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />

    <!-- favicon info -->
    <link rel="apple-touch-icon" sizes="180x180" href="/resources/apple-touch-icon.png" />
    <link rel="icon" type="image/png" sizes="32x32" href="/resources/favicon-32x32.png" />
    <link rel="icon" type="image/png" sizes="16x16" href="/resources/favicon-16x16.png" />
    <link rel="manifest" href="/resources/site.webmanifest" />

    <title>WebJEA</title>

    <link href="/content/bootstrap.min.css" rel="stylesheet" />

    <link href="/content/themes/base/jquery-ui.min.css" rel="stylesheet" />
    <link href="/content/jquery-ui-timepicker-addon.min.css" rel="stylesheet" />

    <link href="/resources/default.css" rel="stylesheet" />
    <link href="/resources/main.css" rel="stylesheet" />
    <link href="/resources/psoutput.css" rel="stylesheet" />
</head>
<body class="bg-light mode-dashboard">
    <script src="/resources/validation.js"></script>
    <form id="frmMain" runat="server">
    <div id="app" runat="server" clientidmode="static" class="mode-dashboard">

        <!-- ── DASHBOARD HEADER ── -->
        <h1 id="dashboard-header">
            <asp:Label ID="lblTitleDash" runat="server" Text="" CssClass="dashboard-title"></asp:Label>
        </h1>
        <main>
        <!-- ── MAIN CONTENT ── -->
        <div id="divTileView" runat="server" clientidmode="static" class="tile-grid"></div>
        </main>
        <!-- ── DASHBOARD FOOTER ── -->
        <div id="dashboard-footer">
            Powered by&nbsp;<a href="http://www.webjea.com" target="_blank" class="WebJEALink">WebJEA</a>&nbsp;<asp:Literal runat="server" ID="lblVersionDash"></asp:Literal>.
        </div>

    </div><!-- /app -->
    </form>

    <!-- jQuery (necessary for Bootstrap's JavaScript plugins) -->
    <script src="/scripts/jquery-<%=ConfigurationManager.AppSettings("jQueryVersion")%>.js"></script>
    <script src="/scripts/jquery-ui-<%=ConfigurationManager.AppSettings("jQueryUIVersion")%>.min.js"></script>
    <script src="/scripts/jquery-ui-sliderAccess.js"></script>
    <script src="/scripts/jquery-ui-timepicker-addon.min.js"></script>
    <script src="/scripts/bootstrap.bundle.min.js"></script>

    <!-- webjea startup -->
    <script src="/resources/startup.js"></script>
</body>
</html>
