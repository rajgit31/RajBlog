<%@ Control Language="C#" AutoEventWireup="true" CodeFile="widget.ascx.cs" Inherits="widgets_TwitterSearch_widget" %>
<asp:Repeater ID="twitterItems" runat="server" OnItemDataBound="ItemDataBoundHandler">
    <ItemTemplate>
        <table runat="server" cellspacing="-1" cellpadding="-1" border="0" style="width:100%">
            <tr style="vertical-align:top">
                <td style="width:52px"><asp:Image ID="itemImage" runat="server"></asp:Image></td>
                <td><asp:Label runat="server" ID="itemText" Text=""></asp:Label></td>
            </tr>
            <tr>
                <td colspan="2">
                    <asp:Label runat="server" ID="itemUser" Text="" style="color:Gray;font-size:smaller"></asp:Label>
                    <asp:Label runat="server" ID="itemDate" Text="" style="color:Gray;font-size:smaller"></asp:Label>
                </td>
            </tr>
        </table>
        <h4></h4>
    </ItemTemplate>
</asp:Repeater>
<asp:HyperLink ID="twitterLink" runat="server">Follow me on Twitter</asp:HyperLink>
