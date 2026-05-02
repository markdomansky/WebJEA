<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="command.aspx.vb" Inherits="WebJEA._command" %>

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

    <link href="/resources/command.css" rel="stylesheet" />
    <link href="/resources/main.css" rel="stylesheet" />
    <link href="/resources/psoutput.css" rel="stylesheet" />
</head>
<body class="bg-light">
    <script src="/resources/validation.js"></script>
    <form id="frmMain" runat="server">
    <div id="app" runat="server" clientidmode="static" class="mode-cmd">

        <!-- ── MOBILE TOPBAR (shown below lg in command mode) ── -->
        <div id="mobile-topbar">
            <button class="btn btn-outline-secondary btn-sm" type="button" onclick="toggleSidebar()" aria-label="Toggle navigation">
                <span aria-hidden="true">&#9776;</span>
            </button>
            <asp:Label ID="lblTitle" runat="server" Text="lblTitle" CssClass="site-name"></asp:Label>
        </div>

        <!-- ── LAYOUT WRAPPER ── -->
        <div id="layout">
            <div id="overlay" onclick="closeSidebar()"></div>

            <!-- ── SIDEBAR ── -->
            <div id="sidebar">
                <!-- Brand -->
                <div id="sidebar-brand">
                    <asp:Label ID="lblTitleDesktop" runat="server" Text="lblTitle"></asp:Label>
                </div>

                <!-- Sub-nav with Dashboard link -->
                <div id="sidebar-subnav">
                    <a class="nav-item-link" href="default.aspx">Dashboard<span class="nav-tip">Return to the dashboard</span></a>
                </div>

                <!-- Scrollable menu -->
                <div id="nav-list">
                    <asp:ListView ID="lvMenu" runat="server">
                        <LayoutTemplate>
                            <asp:PlaceHolder ID="itemPlaceholder" runat="server" />
                        </LayoutTemplate>
                        <ItemTemplate>
                            <a class="nav-item-link <%#Eval("CSS") %>" href="<%#Eval("Uri") %>"><%#Eval("DisplayName") %><span class="nav-tip"><%#Eval("Description") %></span></a>
                        </ItemTemplate>
                    </asp:ListView>
                </div>

                <!-- Sidebar footer -->
                <div id="sidebar-footer">
                    <div class="footer">Powered by&nbsp;<a href="http://www.webjea.com" target="_blank" class="WebJEALink">WebJEA</a>&nbsp;<asp:Literal runat="server" ID="lblVersion"></asp:Literal>.</div>
                </div>
            </div>

            <!-- ── MAIN CONTENT ── -->
            <div id="main">
                <div id="scroll-area">
                    <div role="main" runat="server" id="divCmdBody" clientidmode="static">
                        <div class="page-header">
                            <h1 id="lblCmdTitle" runat="server" class="h3 fw-bold"></h1>
                        </div>

                        <div class="accordion" id="SynopsisAndDescription" runat="server" clientidmode="static">
                            <div class="accordion-item">
                                <h2 class="accordion-header">
                                    <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#flush-collapseOne" aria-expanded="false" aria-controls="flush-collapseOne">
                                        <asp:Literal ID="lblCmdSynopsis" runat="server" Mode="PassThrough"></asp:Literal>
                                    </button>
                                </h2>
                                <div id="flush-collapseOne" class="accordion-collapse collapse" data-bs-parent="#SynopsisAndDescription">
                                    <div class="accordion-body">
                                        <asp:Literal ID="lblCmdDescription" runat="server" Mode="PassThrough"></asp:Literal>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <div class="card card-body mb-3" id="divSingleInfo" runat="server" clientidmode="static">
                            <asp:Literal ID="lblSingleInfo" runat="server" Mode="PassThrough"></asp:Literal>
                        </div>

                        <div id="panelOnload" class="" runat="server">
                            <div class="card psouter">
                                <div id="consoleOnload" runat="server" class="ps">consoleOnLoad</div>
                            </div>
                        </div>

                        <div class="card" id="panelInput" runat="server">
                            <div class="card-body">
                                <div id="divParameters" runat="server"></div>
                                <asp:Button CssClass="btn btn-primary" ID="btnRun" runat="server" Text="Submit" OnClientClick="return webjeaSubmit();" UseSubmitBehavior="False" /><span id="imgLoader"><span id="imgLoaderSVG"></span></span>
                            </div>
                        </div>

                        <div class="collapse" id="panelOutput" runat="server">
                            <div class="card psouter">
                                <asp:Label ID="consoleOutput" runat="server" Text="consoleOutput" CssClass="ps"></asp:Label>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

        </div><!-- /layout -->

    </div><!-- /app -->
    </form>

    <!-- jQuery (necessary for Bootstrap's JavaScript plugins) -->
    <script src="/scripts/jquery-<%=ConfigurationManager.AppSettings("jQueryVersion")%>.js"></script>
    <script src="/scripts/jquery-ui-<%=ConfigurationManager.AppSettings("jQueryUIVersion")%>.min.js"></script>
    <script src="/scripts/jquery-ui-sliderAccess.js"></script>
    <script src="/scripts/jquery-ui-timepicker-addon.min.js"></script>
    <script src="/scripts/bootstrap.bundle.min.js"></script>

    <!-- webjea output parser -->
    <script src="/resources/purify.min.js"></script>
    <script src="/resources/PSWebParser.js"></script>

    <!-- webjea startup -->
    <script src="/resources/startup.js"></script>
    <script src="/resources/command-init.js" data-cmdid="<%=cmdid%>"></script>
</body>
</html>
