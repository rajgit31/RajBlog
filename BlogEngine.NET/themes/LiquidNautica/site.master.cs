using System;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;

public partial class LiquidNauticaSite : System.Web.UI.MasterPage
{
  protected void Page_Load(object sender, EventArgs e)
  {
		if (Page.User.Identity.IsAuthenticated)
		{
			aLogin.InnerHtml = String.Format("<span>{0}</span>", Resources.labels.logoff );
			aLogin.HRef = BlogEngine.Core.Utils.RelativeWebRoot + "login.aspx?logoff";
		}
		else
		{
			aLogin.HRef = BlogEngine.Core.Utils.RelativeWebRoot + "login.aspx";
            aLogin.InnerHtml = String.Format("<span>{0}</span>", Resources.labels.login);
		}
  }

}
