<%@ Control Language="C#" AutoEventWireup="true" CodeFile="edit.ascx.cs" Inherits="widgets_TwitterFeed_edit" %>
<%@ Reference Control="~/widgets/TwitterFeed/widget.ascx" %>
<asp:Label for="<%=twitterAccountName%>" ID="twitterAccountNameLabel" runat="server" Text="Twitter Account Name"></asp:Label><br />
<asp:TextBox ID="twitterAccountName" runat="server"></asp:TextBox>
<asp:RequiredFieldValidator ID="twitterAccountNameValidator" ControlToValidate="twitterAccountName" runat="server" ErrorMessage="Required" Display="dynamic"></asp:RequiredFieldValidator>
<br /><br />
<asp:Label for="<%=itemCount%>" ID="itemCountLabel" runat="server" Text="Number of Items"></asp:Label><br />
<asp:TextBox ID="itemCount" runat="server"></asp:TextBox>
<asp:CompareValidator ID="itemCountValidator" Type="Integer" Operator="dataTypeCheck" ControlToValidate="itemCount" runat="server" ErrorMessage="Numeric value required" Display="dynamic"></asp:CompareValidator>
<br /><br />
<asp:Label for="<%=followMeText%>" ID="followMeTextLabel" runat="server" Text="Follow Me Text"></asp:Label><br />
<asp:TextBox ID="followMeText" runat="server"></asp:TextBox>
<asp:RequiredFieldValidator ID="followMeTextValidator" ControlToValidate="followMeText" runat="server" ErrorMessage="Required" Display="dynamic"></asp:RequiredFieldValidator>
<br /><br />
<asp:Label for="<%=pollingInterval%>" ID="pollingIntevalLabel" runat="server" Text="Polling Interval (minutes)"></asp:Label><br />
<asp:TextBox ID="pollingInterval" runat="server"></asp:TextBox>
<asp:CompareValidator ID="pollingIntervalValidator" Type="Integer" Operator="dataTypeCheck" ControlToValidate="pollingInterval" runat="server" ErrorMessage="Numeric value required" Display="dynamic"></asp:CompareValidator>
<br /><br />
<asp:Label for="<%=dateFormat%>" ID="dateFormatLabel" runat="server" Text="Date Format (do not modify unless you understand date format strings)"></asp:Label><br />
<asp:TextBox ID="dateFormat" runat="server"></asp:TextBox>
<asp:RequiredFieldValidator ID="dateFormatValidator" ControlToValidate="dateFormat" runat="server" ErrorMessage="Required" Display="dynamic"></asp:RequiredFieldValidator>
<br /><br />
<asp:CheckBox ID="showRelativeDate" runat="server" Text="Use Relative Date (e.g. ‘3 hours ago’)" />




