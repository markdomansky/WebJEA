<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="default.aspx.vb" Inherits="WebJEA._default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <meta http-equiv="X-UA-Compatible" content="IE=edge" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />

    <link rel="icon" href="favicon.ico" />
    <title>WebJEA</title>

    <link href="content/bootstrap.min.css" rel="stylesheet" />
    <!-- IE10 viewport hack for Surface/desktop Windows 8 bug -->
    <link href="content/ie10-viewport-bug-workaround.css" rel="stylesheet" />

    <link href="content/themes/base/jquery-ui.css" rel="stylesheet" />
    <link href="content/jquery-ui-timepicker-addon.css" rel="stylesheet" />

    <link href="sidebar.css" rel="stylesheet" />
    <%--<link href="loader.css" rel="stylesheet" />--%>
    <link href="main.css" rel="stylesheet" />
    <link href="psoutput.css" rel="stylesheet" />
</head>
<body>
    <!-- webjea specific scripts -->
    <script src="validation.js"></script>
    <form id="frmMain" runat="server">

        <div class="container-fluid ">
            <div class="row no-gutters">
                <div class="col-md-3">
                    <div class="nav-side-menu">
                        <asp:Label ID="lblTitle" runat="server" Text="lblTitle" CssClass="brand"></asp:Label>
                        <%--navbar-brand--%>

                        <div type="button" class="navbar-toggler toggle-btn " data-toggle="collapse" data-target="#menu-content" aria-label="Toggle Menu"><div class="hamburger">&nbsp;</div></div>

                        <div id="menu-list" class="menu-list ">
                            <div id="menu-content" class="menu-content collapse out" >
                                <asp:ListView ID="lvMenu" runat="server" class="">
                                    <LayoutTemplate>
                                        <ul ">
                                            <asp:PlaceHolder ID="itemPlaceholder" runat="server" />
<%--                                            <li id="footer" class="menulink footer">Powered by <a href="http://webjea.com" target="_blank">WebJEA</a> <asp:Literal runat="server" ID="lblVersion"></asp:Literal>.</li>--%>
                                        </ul>
                                    </LayoutTemplate>
                                    <ItemTemplate>
                                        <li class="menulink <%#Eval("CSS") %>"><a href="<%#Eval("Uri") %>"><%#Eval("DisplayName") %></a></li>
                                    </ItemTemplate>
                                </asp:ListView>
                                <div id="footer" class="footer">Powered by <a href="http://webjea.com" target="_blank">WebJEA</a><asp:Literal runat="server" ID="lblVersion"></asp:Literal>.</div>
                            </div>
                        </div>
                    </div>
                </div>

                <%--col-sm-offset-1--%> 
                <div class="col-md-9 main" runat="server" id="divCmdBody" clientmode="static">
                    <div class="page-header">
                        <h3>
                            <asp:Label ID="lblCmdTitle" runat="server" Text="lblCmdTitle"></asp:Label>
                        </h3>
                    </div>
                    <div id="Synopsis" class="description">
                        <asp:Label ID="lblCmdSynopsis" runat="server" Text="lblCmdSynopsis"></asp:Label>
                        <a runat="server" href="#collapseDescription" class="btn btn-sm btn-light" data-toggle="collapse" aria-expanded="false" aria-controls="collapseDescription" id="btnMore">more</a></div>
                    <div id="collapseDescription" class="collapse">
                        <div class="well">
                            <asp:Label ID="lblCmdDescription" runat="server" Text="lblCmdDescription"></asp:Label></div>
                    </div>
                    <div>&nbsp;</div>
                    <div id="panelOnload" class="" runat="server">
                        <div class="card psouter">
                            <div id="consoleOnload" runat="server" class="ps">consoleOnLoad</div>
                        </div>
                    </div>
                    <div class="card " id="panelInput" runat="server">
                        <div class="card-body">
                            <div id="divParameters" runat="server"></div>
                            <asp:Button CssClass="btn btn-primary" ID="btnRun" runat="server" Text="Submit" OnClientClick="return disableOnPostback();" UseSubmitBehavior="True" /><span id="imgLoader"><span id="imgLoaderSVG" ></span></span>
                        </div>

                    </div>

                    <div class="collapse" id="panelOutput" runat="server">
                        <div class="card psouter">
                            <asp:Label ID="consoleOutput" runat="server" Text="consoleOutput" CssClass="ps"></asp:Label></div>
                    </div>

                </div>
            </div>
        </div>

    </form>

    <!-- jQuery (necessary for Bootstrap's JavaScript plugins) -->
    <script src="scripts/jquery-3.3.1.min.js"></script>
    <script src="scripts/jquery-ui-1.12.1.min.js"></script>
    <script src="scripts/jquery-ui-sliderAccess.js"></script>
    <script src="scripts/jquery-ui-timepicker-addon.min.js"></script>
    <!-- Include all compiled plugins (below), or include individual files as needed -->
    <script src="scripts/popper.min.js"></script>
    <script src="scripts/bootstrap.min.js"></script>
    <!-- IE10 viewport hack for Surface/desktop Windows 8 bug -->
    <script src="scripts/ie10-viewport-bug-workaround.js"></script>

    <!-- webjea startup -->
    <script src="startup.js"></script>
</body>
</html>
