using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(DuAnRapChieuPhim.Startup))]
namespace DuAnRapChieuPhim
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
