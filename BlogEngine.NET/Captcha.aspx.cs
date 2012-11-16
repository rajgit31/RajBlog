using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Drawing;
using System.Drawing.Drawing2D;

public partial class Captcha : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        CreateCheckCodeImage(GenCode(4));
    }

    private string GenCode(int num)
    {
        string[] source = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
        string code = "";
        Random rd = new Random();
        int i;
        for (i = 0; i < num; i++)
        {
            code += source[rd.Next(0, source.Length)];
        }
        return code;

    }

    private void CreateCheckCodeImage(string checkCode)
    {
        if (checkCode.Trim() == "" || checkCode == null)
            return;
        Session["AlphaCaptchaCode"] = checkCode;
        System.Drawing.Bitmap image = new System.Drawing.Bitmap((int)(checkCode.Length * 19), 22);
        Graphics g = Graphics.FromImage(image);
        try
        {

            Random random = new Random();

            g.Clear(Color.White);

            int i;
            for (i = 0; i < 25; i++)
            {
                int x1 = random.Next(image.Width);
                int x2 = random.Next(image.Width);
                int y1 = random.Next(image.Height);
                int y2 = random.Next(image.Height);
                g.DrawLine(new Pen(Color.Silver), x1, y1, x2, y2);
            }

            Font font = new System.Drawing.Font("Arial", 14, (System.Drawing.FontStyle.Bold));
            System.Drawing.Drawing2D.LinearGradientBrush brush = new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(0, 0, image.Width, image.Height), Color.Blue, Color.DarkRed, 1.2F, true);
            g.DrawString(checkCode, font, brush, 4, 1);

            g.DrawRectangle(new Pen(Color.Silver), 0, 0, image.Width - 1, image.Height - 1);
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            image.Save(ms, System.Drawing.Imaging.ImageFormat.Gif);
            Response.ClearContent();
            Response.ContentType = "image/jpg";
            Response.BinaryWrite(ms.ToArray());

        }
        catch
        {
            g.Dispose();
            image.Dispose();
        }

    }
}