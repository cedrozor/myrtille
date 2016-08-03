using System.Web.Optimization;

namespace Myrtille.Mvc
{
    public class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/default.css"));

            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.js",
                      "~/Scripts/respond.js"));

            bundles.Add(new ScriptBundle("~/bundles/myrtille").Include(
                      "~/Scripts/myrtille.js",
                      "~/Scripts/config.js",
                      "~/Scripts/dialog.js",
                      "~/Scripts/display.js",
                      "~/Scripts/display/canvas.js",
                      "~/Scripts/display/divs.js",
                      "~/Scripts/network.js",
                      "~/Scripts/network/buffer.js",
                      "~/Scripts/network/longpolling.js",
                      "~/Scripts/network/websocket.js",
                      "~/Scripts/network/xmlhttp.js",
                      "~/Scripts/user.js",
                      "~/Scripts/user/keyboard.js",
                      "~/Scripts/user/mouse.js",
                      "~/Scripts/user/touchscreen.js"));
        }
    }
}
