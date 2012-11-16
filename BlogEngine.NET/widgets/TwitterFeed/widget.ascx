<%@ Control Language="C#" AutoEventWireup="true" CodeFile="widget.ascx.cs" Inherits="widgets_TwitterFeed_widget" %>
<asp:Repeater ID="twitterItems" runat="server" OnItemDataBound="ItemDataBoundHandler">
    <ItemTemplate>
        <div runat="server" style="margin-bottom:10px">
            <asp:Label runat="server" ID="itemText" Text=""></asp:Label><br />
            <asp:Label runat="server" ID="itemDate" Text="" style="color:Gray;font-size:smaller;font-style:italic"></asp:Label>
            <asp:Label runat="server" ID="itemVia" Text="" style="color:Gray;font-size:smaller;font-style:italic"></asp:Label>
        </div>
    </ItemTemplate>
</asp:Repeater>
<asp:HyperLink ID="twitterLink" runat="server" style="margin-top:5px">Follow me on Twitter</asp:HyperLink>
