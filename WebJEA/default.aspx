<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="default.aspx.vb" Inherits="WebJEA._default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml" id="htmlRoot" runat="server">
<head runat="server">
    <meta charset="utf-8" />
    <meta http-equiv="X-UA-Compatible" content="IE=edge" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />

    <!-- favicon info -->
    <link rel="apple-touch-icon" sizes="180x180" href="resources/apple-touch-icon.png" />
    <link rel="icon" type="image/png" sizes="32x32" href="resources/favicon-32x32.png" />
    <link rel="icon" type="image/png" sizes="16x16" href="resources/favicon-16x16.png" />
    <link rel="manifest" href="resources/site.webmanifest" />

    <title>WebJEA</title>

    <link href="content/bootstrap.min.css" rel="stylesheet" />

    <link href="content/themes/base/jquery-ui.min.css" rel="stylesheet" />
    <link href="content/jquery-ui-timepicker-addon.min.css" rel="stylesheet" />

    <link href="resources/sidebar.css" rel="stylesheet" />
    <link href="resources/main.css" rel="stylesheet" />
    <link href="resources/psoutput.css" rel="stylesheet" />
</head>
<body class="bg-light d-flex flex-column vh-100">
    <script src="resources/validation.js"></script>
    <form id="frmMain" runat="server">

        <!-- ── MOBILE TOPBAR (hidden lg+) ── -->
        <nav class="navbar navbar-dark bg-dark d-flex d-lg-none flex-shrink-0 px-3">
            <asp:Label ID="lblTitle" runat="server" Text="lblTitle" CssClass="navbar-brand fw-bold mb-0"></asp:Label>
            <button class="btn btn-outline-secondary btn-sm" type="button"
                data-bs-toggle="offcanvas" data-bs-target="#sidebarOffcanvas" aria-label="Toggle navigation">
                <span aria-hidden="true">&#9776;</span>
            </button>
        </nav>

        <!-- ── WRAPPER ── -->
        <div class="d-flex flex-row flex-grow-1 overflow-hidden">

            <!-- ── DESKTOP SIDEBAR (hidden below lg) ── -->
            <nav id="desktopSidebar" class="d-none d-lg-flex flex-column flex-shrink-0 overflow-auto sidebar-dark">
                <!-- Brand -->
                <div class="sidebar-brand">
                    <asp:Label ID="lblTitleDesktop" runat="server" Text="lblTitle" CssClass="brand"></asp:Label>
                </div>

                <!-- Menu -->
                <div class="flex-grow-1 sidebar-menu-list">
                    <asp:ListView ID="lvMenu" runat="server">
                        <LayoutTemplate>
                            <ul class="sidebar-menu">
                                <asp:PlaceHolder ID="itemPlaceholder" runat="server" />
                            </ul>
                        </LayoutTemplate>
                        <ItemTemplate>
                            <li class="menulink <%#Eval("CSS") %>"><a href="<%#Eval("Uri") %>"><%#Eval("DisplayName") %></a></li>
                        </ItemTemplate>
                    </asp:ListView>
                </div>

                <!-- Sidebar footer -->
                <div class="sidebar-footer">
                    <%-- Future: user info display will go here --%>
                    <div id="footer" class="footer">Powered by <a href="http://www.webjea.com" target="_blank" class="WebJEALink">WebJEA</a><asp:Literal runat="server" ID="lblVersion"></asp:Literal>.</div>
                </div>
            </nav>

            <!-- ── MAIN CONTENT ── -->
            <main class="flex-grow-1 overflow-auto p-4" role="main" runat="server" id="divCmdBody" clientidmode="static">
                <div class="page-header">
                    <h1 id="lblCmdTitle" runat="server" class="h3 fw-bold"></h1>
                </div>

                <div class="accordion" id="SynopsisAndDescription">
                    <div class="accordion-item">
                        <h2 class="accordion-header">
                            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#flush-collapseOne" aria-expanded="false" aria-controls="flush-collapseOne">
                                <asp:Label ID="lblCmdSynopsis" runat="server" Text="lblCmdSynopsis"></asp:Label>
                            </button>
                        </h2>
                        <div id="flush-collapseOne" class="accordion-collapse collapse" data-bs-parent="#SynopsisAndDescription">
                            <div class="accordion-body">
                                <asp:Label ID="lblCmdDescription" runat="server" Text="lblCmdDescription"></asp:Label>
                            </div>
                        </div>
                    </div>
                </div>

                <div id="panelOnload" class="" runat="server">
                    <div class="card psouter">
                        <div id="consoleOnload" runat="server" class="ps">consoleOnLoad</div>
                    </div>
                </div>

                <div class="card" id="panelInput" runat="server">
                    <div class="card-body">
                        <div id="divParameters" runat="server"></div>
                        <asp:Button CssClass="btn btn-primary" ID="btnRun" runat="server" Text="Submit" OnClientClick="return disableOnPostback();" UseSubmitBehavior="True" /><span id="imgLoader"><span id="imgLoaderSVG"></span></span>
                    </div>
                </div>

                <div class="collapse" id="panelOutput" runat="server">
                    <div class="card psouter">
                        <asp:Label ID="consoleOutput" runat="server" Text="consoleOutput" CssClass="ps"></asp:Label>
                    </div>
                </div>
            </main>

        </div><!-- /.wrapper -->

    </form>

    <!-- ── OFFCANVAS SIDEBAR (mobile only) ── -->
    <div class="offcanvas offcanvas-start bg-dark text-white" tabindex="-1" id="sidebarOffcanvas">
        <div class="offcanvas-header border-bottom border-secondary">
            <h5 class="offcanvas-title text-white fw-bold mb-0" id="offcanvasTitle"></h5>
            <button type="button" class="btn-close btn-close-white" data-bs-dismiss="offcanvas" aria-label="Close"></button>
        </div>
        <div class="offcanvas-body px-0 py-2 d-flex flex-column">
            <div id="offcanvasMenuTarget" class="flex-grow-1 sidebar-menu-list"></div>
            <div class="px-3 py-3 border-top border-secondary mt-auto">
                <div id="offcanvasFooter" class="footer"></div>
            </div>
        </div>
    </div>

    <!-- jQuery (necessary for Bootstrap's JavaScript plugins) -->
    <script src="<%= ResolveUrl("~/scripts/jquery-" & ConfigurationManager.AppSettings("jQueryVersion") & ".js") %>"></script>
    <script src="<%= ResolveUrl("~/scripts/jquery-ui-" & ConfigurationManager.AppSettings("jQueryUIVersion") & ".min.js") %>"></script>
    <script src="scripts/jquery-ui-sliderAccess.js"></script>
    <script src="scripts/jquery-ui-timepicker-addon.min.js"></script>
    <script src="scripts/bootstrap.bundle.min.js"></script>

    <!-- webjea startup -->
    <script src="resources/startup.js"></script>
</body>
</html>
