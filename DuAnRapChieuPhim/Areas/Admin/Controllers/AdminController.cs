using DuAnRapChieuPhim.Models;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Linq;
using System.Linq;
using System.Net.NetworkInformation;
using System.Web;
using PagedList;
using PagedList.Mvc;
using System.Web.Mvc;
using System.Web.UI;
using System.Security.Cryptography;
using System.Text;

namespace DuAnRapChieuPhim.Areas.Admin.Controllers
{
    public class AdminController : Controller
    {
        DbDataContext db = new DbDataContext();

        // GET: Admin/Admin
        public ActionResult Login()
        {
            return View();
        }
        public ActionResult Index()
        {
            var model = GetDoanhThuData();
            return View(model);
        }

        public ActionResult ThongKeDoanhThu()
        {
            var model = GetDoanhThuData();
            return View(model);
        }

        private DoanhThuViewModel GetDoanhThuData()
        {
            var doanhThuData = new DoanhThuViewModel
            {
                Labels = new List<string>(),
                TicketData = new List<decimal>(),
                ComboData = new List<decimal>()
            };

            // Lấy dữ liệu từ cơ sở dữ liệu và xử lý để điền vào model
            var doanhThuList = db.HoaDons.Where(hd => hd.NgayDat.Value.Month == DateTime.Now.Month && hd.NgayDat.Value.Year == DateTime.Now.Year).ToList();

            foreach (var doanhThu in doanhThuList)
            {
                doanhThuData.Labels.Add(doanhThu.NgayDat.Value.Date.ToString());
                doanhThuData.TicketData.Add(doanhThu.TienVe.Value);
                doanhThuData.ComboData.Add(doanhThu.TienCombo.Value);
            }

            return doanhThuData;
        }
        public ActionResult Movie(int? page)
        {
            ViewBag.TL = db.TheLoais.ToList();
            int pageSize = 5; // Số sản phẩm trên mỗi trang
            int pageNumber = (page ?? 1); // Trang mặc định là trang 1 nếu không có tham số page
            var phim = db.Phims.Where(u => !u.TrangThai.Contains("Ngưng chiếu")).ToPagedList(pageNumber, pageSize);   

            return View(phim);
        }
        public ActionResult Slider(int? page)
        {
            int pageSize = 5; // Số sản phẩm trên mỗi trang
            int pageNumber = (page ?? 1); // Trang mặc định là trang 1 nếu không có tham số page
            var phim = db.Sliders.ToPagedList(pageNumber, pageSize);

            return View(phim);
        }
        public ActionResult News(int? page)
        {
            int pageSize = 5; // Số sản phẩm trên mỗi trang
            int pageNumber = (page ?? 1); // Trang mặc định là trang 1 nếu không có tham số page
            var phim = db.Tintucs.ToPagedList(pageNumber, pageSize);

            return View(phim);
        }
        public ActionResult TheLoai(int? page)
        {

            int pageSize = 5; // Số sản phẩm trên mỗi trang
            int pageNumber = (page ?? 1); // Trang mặc định là trang 1 nếu không có tham số page
            var phim = db.TheLoais.ToPagedList(pageNumber, pageSize);

            return View(phim);
        }
        public ActionResult Food(int? page)
        {
            int pageSize = 5; // Số sản phẩm trên mỗi trang
            int pageNumber = (page ?? 1); // Trang mặc định là trang 1 nếu không có tham số page
            var doan = db.DoAns.Where(u => u.TrangThai.Contains("Đang bán")).ToPagedList(pageNumber, pageSize);

            return View(doan);
        }
        public ActionResult Chair(int? page)
        {
            int pageSize = 5; // Số sản phẩm trên mỗi trang
            int pageNumber = (page ?? 1); // Trang mặc định là trang 1 nếu không có tham số page
            var ghe = db.Ghes.ToPagedList(pageNumber, pageSize);

            return View(ghe);
        }
        public ActionResult Combo(int? page)
        {
            ViewBag.DoAn = db.DoAns.ToList();
            ViewBag.Nuoc = db.Nuocs.ToList();
            int pageSize = 5; // Số sản phẩm trên mỗi trang
            int pageNumber = (page ?? 1); // Trang mặc định là trang 1 nếu không có tham số page
            var ghe = db.Combos.Where(u => u.TrangThai.Contains("Đang bán")).ToPagedList(pageNumber, pageSize);

            return View(ghe);
        }
        public ActionResult Nuoc(int? page)
        {
            int pageSize = 5; // Số sản phẩm trên mỗi trang
            int pageNumber = (page ?? 1); // Trang mặc định là trang 1 nếu không có tham số page
            var ghe = db.Nuocs.Where(u=>u.TrangThai.Contains("Đang bán")).ToPagedList(pageNumber, pageSize);

            return View(ghe);
        }
        public ActionResult Bill(int? page)
        {
            int pageSize = 5; // Số sản phẩm trên mỗi trang
            int pageNumber = (page ?? 1); // Trang mặc định là trang 1 nếu không có tham số page
            var hoadon = db.HoaDons.Where(u => u.TrangThai.Contains("Đã thanh toán")).ToPagedList(pageNumber, pageSize);

            return View(hoadon);
        }
        private string HashSHA256(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }
        public ActionResult DetailsHD(long id)
        {
            var cthd = from chiTiet in db.ChiTietHoaDons
                       join lichChieu in db.LichChieus on chiTiet.MaLichChieu equals lichChieu.MaChieuPhim
                       join ghe in db.Ghes on chiTiet.MaGhe equals ghe.MaGhe
                       join phong in db.Phongs on ghe.MaPhong equals phong.MaPhong
                       join phim in db.Phims on lichChieu.MaPhim equals phim.MaPhim
                       where chiTiet.MaHoaDon == id
                       select new CTHD
                       {
                           chiTiet = chiTiet,
                           lichChieu = lichChieu,
                           ghe = ghe,
                           phong = phong,
                           phim = phim
                       };
            var cthdcb = from hdcb in db.CTHDComBos
                         join cb in db.Combos on hdcb.MaCB equals cb.MaCB
                         join nuoc in db.Nuocs on cb.MaNuoc equals nuoc.MaNuoc
                         join doan in db.DoAns on cb.MaDoAn equals doan.MaDoAn
                         where hdcb.MaHoaDon == id
                         select new CTHDCB
                         {
                             cthd = hdcb,
                             cb = cb,
                             nuoc = nuoc,
                             doan = doan
                         };
            var hd = db.HoaDons.SingleOrDefault(u => u.MaHoaDon == id);
            ViewBag.hd = hd;
            ViewBag.cb = cthdcb.ToList();
            ViewBag.ghe = cthd.ToList();
            string orderCode = $"HD{id}"; // Mã đơn hàng

            ViewBag.QRCode = QRCodeGenerator.GenerateQRCode(orderCode);
            return View();
        }
        [HttpPost]
        public ActionResult Login(string taikhoan, string password)
        {
            if (string.IsNullOrEmpty(taikhoan) || string.IsNullOrEmpty(password))
            {
                // Handle empty username or password
                return RedirectToAction("Login");
            }

            // Hash the password using a secure hashing algorithm before comparing it with the database
            string hashedPassword = HashSHA256(password);

            // Check if the user exists in the database
            var user = db.TaiKhoans
                .FirstOrDefault(u => u.Username == taikhoan && u.Password == hashedPassword);

            if (user != null)
            {
                // User authenticated, you can set authentication cookies or session variables here
                // For example, you can use ASP.NET Core Identity for more advanced authentication features
                return RedirectToAction("Index"); // Redirect to a success page
            }
            else
            {
                // User not found or password doesn't match
                // You may want to handle incorrect login attempts or display an error message
                return RedirectToAction("Login");
            }
        }
        public ActionResult Showtimes (int? page)
        {
            ViewBag.Phim = db.Phims.ToList();
            ViewBag.Phong = db.Phongs.ToList();
            int pageSize = 5; // Số sản phẩm trên mỗi trang
            int pageNumber = (page ?? 1); // Trang mặc định là trang 1 nếu không có tham số page
            var lichchieu = db.LichChieus.ToPagedList(pageNumber, pageSize);

            return View(lichchieu);
        }
        public ActionResult Room(int? page)
        {
            int pageSize = 5; // Số sản phẩm trên mỗi trang
            int pageNumber = (page ?? 1); // Trang mặc định là trang 1 nếu không có tham số page
            var phong  = db.Phongs.Where(u => u.TrangThai.Contains("Hoạt động")).ToPagedList(pageNumber, pageSize);

            return View(phong);
        }
        public ActionResult Account (int? page)
        {
            int pageSize = 5; // Số sản phẩm trên mỗi trang
            int pageNumber = (page ?? 1); // Trang mặc định là trang 1 nếu không có tham số page
            var taikhoan = db.TaiKhoans.ToPagedList(pageNumber, pageSize);

            return View(taikhoan);
        }
        public ActionResult Infor(int? page)
        {
            int pageSize = 5; // Số sản phẩm trên mỗi trang
            int pageNumber = (page ?? 1); // Trang mặc định là trang 1 nếu không có tham số page
            var thongtin  = db.ThongTinCaNhans.ToPagedList(pageNumber, pageSize);

            return View(thongtin);
        }

    }
}