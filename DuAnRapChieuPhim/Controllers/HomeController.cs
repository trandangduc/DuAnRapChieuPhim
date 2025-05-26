using DuAnRapChieuPhim.Models;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using PagedList;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using System.IO;
using ZXing.QrCode.Internal;

namespace DuAnRapChieuPhim.Controllers
{

    public class HomeController : Controller
    {
        DbDataContext db = new DbDataContext();

        public ActionResult Login(string succes,string err)
        {
            ViewData["succes"] = null;
            ViewData["err"] = null;
            if (succes != null)
            {
                ViewData["succes"] = succes;
            }
            if (err != null)
            {
                ViewData["err"] = err;
            }
            return View();

        }
        [HttpPost]
        public ActionResult Login(FormCollection formCollection)
        {
            string emailOrPhone = formCollection["loginInput"];
            string password = HashSHA256(formCollection["passwordInput"]) ;

            // Kiểm tra thông tin đăng nhập trong cơ sở dữ liệu
            var user = db.ThongTinCaNhans
                .FirstOrDefault(u => (u.Email == emailOrPhone || u.SDT == emailOrPhone) && u.Matkhau == password);

            if (user != null)
            {
                Session["KH"] = user;
                if (Session["PreviousPage"] != null)
                {
                    var previousPage = Session["PreviousPage"].ToString();
                    Session.Remove("PreviousPage"); // Xóa thông tin trang trước đó sau khi sử dụng
                    return Redirect(previousPage);
                }

                // Nếu không có trang trước đó, chuyển hướng về trang mặc định
                return RedirectToAction("Index", "Home");
            }
            else
            {
                // Đăng nhập không thành công, trả về thông báo lỗi
                return RedirectToAction("Login",new { err = "Thông tin đăng nhập không đúng" });
            }
        }
        [HttpPost]
        public JsonResult CheckUserExistence(string inputType, string inputInfo)
        {
            // Kiểm tra sự tồn tại của người dùng trong bảng ThongTinCaNhan
            bool userExists = CheckUserExists(inputType, inputInfo);

            return Json(new { exist = userExists });
        }

        // Hàm kiểm tra sự tồn tại của người dùng
        private bool CheckUserExists(string inputType, string inputInfo)
        {

                // Kiểm tra sự tồn tại của người dùng với số điện thoại hoặc email
                if (inputType.ToLower() == "phone")
                {
                    // Kiểm tra số điện thoại
                    return db.ThongTinCaNhans.Any(u => u.SDT == inputInfo);
                }
                else if (inputType.ToLower() == "email")
                {
                    // Kiểm tra email
                    return db.ThongTinCaNhans.Any(u => u.Email == inputInfo);
                }

                // Trường hợp không xác định loại thông tin, trả về false
                return false;
            
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(string currentPassword, string newPassword)
        {
            var nguoidung = Session["KH"] as ThongTinCaNhan;
            int userId = nguoidung.MaKH;

            var user = db.ThongTinCaNhans.FirstOrDefault(u => u.MaKH == userId);

            // Kiểm tra mật khẩu hiện tại
            if (user != null && user.Matkhau == HashSHA256(currentPassword))
            {
                user.Matkhau = HashSHA256(newPassword);
                db.SubmitChanges();
                return RedirectToAction("EditTT", new {id = nguoidung.MaKH ,succes = "Đổi mật khẩu thành công." });
            }
            else
            {
                return RedirectToAction("EditTT", new { id = nguoidung.MaKH, err = "Mật khẩu hiện tại không đúng." });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(string ten, string sdt,string email)
        {
            var nguoidung = Session["KH"] as ThongTinCaNhan;
            int userId = nguoidung.MaKH;

            var user = db.ThongTinCaNhans.FirstOrDefault(u => u.MaKH == userId);

            // Kiểm tra mật khẩu hiện tại
            if (user != null)
            {
                user.Ten = ten;
                user.SDT = sdt;
                user.Email = email;
                db.SubmitChanges();
                return RedirectToAction("EditTT", new { id = nguoidung.MaKH, succes = "Đổi thông tin thành công." });
            }
            else
            {
                return RedirectToAction("EditTT", new { id = nguoidung.MaKH, err = "Đổi thông tin không thành công." });
            }
        }
        [SavePreviousPage]
        public ActionResult Ve(int? page)
        {
            int pageSize = 9; // Số sản phẩm trên mỗi trang
            int pageNumber = (page ?? 1); // Trang mặc định là trang 1 nếu không có tham số page
            ThongTinCaNhan nguoidung = Session["KH"] as ThongTinCaNhan;
            var ve = db.HoaDons.Where(u => u.MaKH==nguoidung.MaKH && u.TrangThai.Contains("Đã thanh toán")).ToPagedList(pageNumber, pageSize);
            return View(ve);
        }
        [SavePreviousPage]
        public ActionResult MovieShow(int? page)
        {
            int pageSize = 9; // Số sản phẩm trên mỗi trang
            int pageNumber = (page ?? 1); // Trang mặc định là trang 1 nếu không có tham số page
            ViewBag.TheloaiList = db.TheLoais.ToList();
            var phim = db.Phims.Where(u=>u.TrangThai.Contains("Đang chiếu")).ToPagedList(pageNumber, pageSize);
            return View(phim);
        }
        [SavePreviousPage]
        public ActionResult MovieSoone(int? page)
        {
            int pageSize = 9; // Số sản phẩm trên mỗi trang
            int pageNumber = (page ?? 1); // Trang mặc định là trang 1 nếu không có tham số page
            ViewBag.TheloaiList = db.TheLoais.ToList();
            var phim = db.Phims.Where(u => u.TrangThai.Contains("Sắp chiếu")).ToPagedList(pageNumber, pageSize);
            return View(phim);
        }
        [HttpPost]
        public ActionResult DK(FormCollection form)
        {
            string hoTen = form["HoTen"];
            string soDienThoai = form["SoDienThoai"];
            string email = form["Email"];
            string matKhau = form["MatKhau"];

            if (db.ThongTinCaNhans.Any(x => x.SDT == soDienThoai))
            {
                return RedirectToAction("Login", new { err = "Số điện thoại đã tồn tại" });
            }

            // Kiểm tra xem email đã tồn tại trong cơ sở dữ liệu hay chưa
            if (db.ThongTinCaNhans.Any(x => x.Email == email))
            {
                return RedirectToAction("Login", new { err = "Email đã tồn tại" });
            }
            // Kiểm tra và xử lý dữ liệu nếu cần
            if (!string.IsNullOrEmpty(hoTen) && !string.IsNullOrEmpty(soDienThoai) && !string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(matKhau))
            {
                // Mã hóa mật khẩu bằng SHA-256
                string hashedPassword = HashSHA256(matKhau);

                var thongTinCaNhan = new ThongTinCaNhan
                {
                    Ten = hoTen,
                    SDT = soDienThoai,
                    Email = email,
                    Matkhau = hashedPassword, // Lưu mật khẩu đã mã hóa
                                              // Gán giá trị cho các thuộc tính khác nếu cần
                };

                db.ThongTinCaNhans.InsertOnSubmit(thongTinCaNhan);
                db.SubmitChanges();

                return RedirectToAction("Login", new { succes = "Đăng ký thành công" }); // Chuyển hướng sau khi đăng ký thành công
            }

            // Nếu dữ liệu không hợp lệ, quay lại view đăng ký
            return RedirectToAction("Login", new { err = "Vui lòng nhập đầy đủ thông tin" });
        }
        [AcceptVerbs("GET", "POST")]
        public JsonResult ForgotPassword(string inputType, string inputInfo)
        {
            // Kiểm tra sự tồn tại của người dùng trong bảng ThongTinCaNhan
            var user = GetUser(inputType, inputInfo);

            if (user != null)
            {
                // Tạo mật khẩu mới
                string newPassword = GenerateRandomPassword();

                // Cập nhật mật khẩu mới cho người dùng trong cơ sở dữ liệu
                UpdatePassword(user, newPassword);

                // Gửi mật khẩu mới đến email của người dùng
                SendNewPasswordByEmail(user.Email, newPassword);

                return Json(new { success = true, message = "Mật khẩu mới đã được gửi đến email của bạn." });
            }

            return Json(new { success = false, message = "Không tìm thấy người dùng." });
        }
        private ThongTinCaNhan GetUser(string inputType, string inputInfo)
        {
                // Kiểm tra sự tồn tại của người dùng với số điện thoại hoặc email
                if (inputType.ToLower() == "phone")
                {
                    // Kiểm tra số điện thoại
                    return db.ThongTinCaNhans.FirstOrDefault(u => u.SDT == inputInfo);
                }
                else if (inputType.ToLower() == "email")
                {
                    // Kiểm tra email
                    return db.ThongTinCaNhans.FirstOrDefault(u => u.Email == inputInfo);
                }

                // Trường hợp không xác định loại thông tin, trả về null
                return null;
            
        }

        // Hàm gửi mật khẩu mới đến email của người dùng
      
        public ActionResult EditTT(int id,string succes,string err)
        {
            ViewData["succes"] = null;
            ViewData["err"] = null;
            if (succes != null)
            {
                ViewData["succes"] = succes;
            }
            if (err != null)
            {
                ViewData["err"] = err;
            }
            var tt = db.ThongTinCaNhans.SingleOrDefault(u=>u.MaKH==id);
            return View(tt);
        }
        public ActionResult Logout()
        {
            Session.Remove("KH");

            return RedirectToAction("Index");

        }
        private string GenerateRandomPassword()
        {
            // Bảng ký tự bạn muốn sử dụng để tạo mật khẩu
            string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()-_=+";

            // Độ dài của mật khẩu
            int passwordLength = 8;

            // Sử dụng StringBuilder để hiệu quả khi thêm ký tự
            StringBuilder password = new StringBuilder();

            // Random số để chọn ký tự từ bảng
            Random random = new Random();

            // Tạo mật khẩu ngẫu nhiên
            for (int i = 0; i < passwordLength; i++)
            {
                int index = random.Next(chars.Length);
                password.Append(chars[index]);
            }

            return password.ToString();
        }

        private void UpdatePassword(ThongTinCaNhan user, string newPassword)
        {

                // Hash mật khẩu mới
                string hashedPassword = HashSHA256(newPassword);

                // Cập nhật mật khẩu mới đã được hash
                user.Matkhau = hashedPassword;
                db.SubmitChanges();
            
        }
        // Hàm mã hóa mật khẩu bằng SHA-256
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
        public ActionResult ThanhToanQuaThe(string selectedCombos)
        {

            var nguoidung = Session["KH"] as ThongTinCaNhan;
            List<int> listGhe = Session["ListGhe"] as List<int>;
            List<ComboDat> comboList = JsonConvert.DeserializeObject<List<ComboDat>>(selectedCombos);
            decimal tienve = 0;
            var danhSachGheTrongList = db.Ghes.Where(g => listGhe.Contains(g.MaGhe)).ToList();

            foreach (var sp in danhSachGheTrongList)
            {
                tienve += sp.GiaTien.Value;
            }
            decimal tiencb = 0;

            foreach (var combo in comboList)
            {

                // Giả sử "MaCB" và "SoLuong" đều có trong mỗi dictionary
                int maCB = combo.MaCB;
                int soLuong = combo.SoLuong;
                // Kiểm tra xem MaCB có tồn tại trong bảng cb không
                var cb = db.Combos.FirstOrDefault(c => c.MaCB == maCB);

                if (cb != null)
                {
                    // Nếu MaCB tồn tại, tính tổng tiền và cộng vào biến tổng
                    decimal giaTien = cb.Gia.Value;
                    decimal thanhTien = giaTien * soLuong;
                    tiencb += thanhTien;

                }
            }
            Session["ListCB"] = comboList;
            // Bây giờ yourList chứa thông tin của các combo và tongTien là tổng tiền cần thanh toán

            HoaDon dh = new HoaDon
            {
                MaHoaDon = DateTime.Now.Ticks,
                HoTen = nguoidung.Ten,
                SoDienThoai = nguoidung.SDT,
                Email = nguoidung.Email,
                MaKH = nguoidung.MaKH,
                TrangThai = "Chưa thanh toán",
                TienVe = tienve,
                TienCombo = tiencb,
                TongTien = tienve + tiencb,
                NgayDat = DateTime.Now.Date
            };
            db.HoaDons.InsertOnSubmit(dh);
            db.SubmitChanges();
            var urlPayment = "";
            Session["MaDH"] = dh.MaHoaDon;

            string vnp_Returnurl = ConfigurationManager.AppSettings["vnp_Returnurl"]; //URL nhan ket qua tra ve 
            string vnp_Url = ConfigurationManager.AppSettings["vnp_Url"]; //URL thanh toan cua VNPAY 
            string vnp_TmnCode = ConfigurationManager.AppSettings["vnp_TmnCode"]; //Ma định danh merchant kết nối (Terminal Id)
            string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"]; //Secret Key


            VnPayLibrary vnpay = new VnPayLibrary();
            long amount = (long)dh.TongTien;
            vnpay.AddRequestData("vnp_Version", VnPayLibrary.VERSION);
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnpay.AddRequestData("vnp_Amount", (amount * 100).ToString()); //Số tiền thanh toán. Số tiền không mang các ký tự phân tách thập phân, phần nghìn, ký tự tiền tệ. Để gửi số tiền thanh toán là 100,000 VND (một trăm nghìn VNĐ) thì merchant cần nhân thêm 100 lần (khử phần thập phân), sau đó gửi sang VNPAY là: 10000000
            long madonhang = dh.MaHoaDon;
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress());

            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang:" + madonhang);
            vnpay.AddRequestData("vnp_OrderType", "other"); //default value: other

            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
            vnpay.AddRequestData("vnp_TxnRef", madonhang.ToString()); // Mã tham chiếu của giao dịch tại hệ thống của merchant. Mã này là duy nhất dùng để phân biệt các đơn hàng gửi sang VNPAY. Không được trùng lặp trong ngày

            //Add Params of 2.1.0 Version
            //Billing

            urlPayment = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
            return Redirect(urlPayment);
        }
        public ActionResult Return()
        {
            if (Request.QueryString.Count > 0)
            {
                string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"]; //Chuoi bi mat
                var vnpayData = Request.QueryString;
                VnPayLibrary vnpay = new VnPayLibrary();

                foreach (string s in vnpayData)
                {
                    if (!string.IsNullOrEmpty(s) && s.StartsWith("vnp_"))
                    {
                        vnpay.AddResponseData(s, vnpayData[s]);
                    }
                }
                string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
                string vnp_TransactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");
                String vnp_SecureHash = Request.QueryString["vnp_SecureHash"];

                bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);
                if (checkSignature)
                {
                    if (vnp_ResponseCode == "00" && vnp_TransactionStatus == "00")
                    {
                        List<int> listGhe = Session["ListGhe"] as List<int>;
                        List<ComboDat> listcb = Session["ListCB"] as List<ComboDat>;
                        long madh = (long)Session["MaDH"];
                        int macp = (int)Session["MaCP"];
                        foreach (var cb in listcb)
                        {
                            var combo = db.Combos.SingleOrDefault(u=>u.MaCB==cb.MaCB);
                            CTHDComBo cthdcb = new CTHDComBo
                            {
                                MaCB = cb.MaCB,
                                SoLuong = cb.SoLuong,
                                GiaTien = combo.Gia.Value,
                                MaHoaDon = madh
                            };
                            db.CTHDComBos.InsertOnSubmit(cthdcb);
                        }
                        foreach (var ghe in listGhe)
                        {
                            var lc = db.LichChieus.SingleOrDefault(u=>u.MaChieuPhim==macp);
                            var g = db.Ghes.SingleOrDefault(u => u.MaPhong == lc.Phong && u.MaGhe==ghe);
                            ChiTietHoaDon cthd = new ChiTietHoaDon
                            {
                                ThoiGianDat = DateTime.Now.Date,
                                ThoiGianChieu = lc.NgayChieu,
                                TongTien = g.GiaTien,
                                MaHoaDon = madh,
                                MaLichChieu = macp,
                                MaGhe = ghe,
                            };
                            db.ChiTietHoaDons.InsertOnSubmit(cthd);
                        }
                        var dh = db.HoaDons.SingleOrDefault(u=>u.MaHoaDon==madh);
                        dh.TrangThai = "Đã thanh toán";
                        db.SubmitChanges();
                        
                    }
                    else
                    {

                        ViewBag.InnerText = "Có lỗi xảy ra trong quá trình xử lý.Mã lỗi: " + vnp_ResponseCode;
                    }
                }
            }
            return View();
        }
        public ActionResult ThongTin()
        {
            var nguoidung = Session["KH"] as ThongTinCaNhan;
            var tt = db.ThongTinCaNhans.SingleOrDefault(u=>u.MaKH==nguoidung.MaKH);
            return View(tt);
        }
        public ActionResult Combo(float totalGiaTien, string selectedSeats)
        {
            ViewBag.TienGhe = totalGiaTien;
            List<int> seatList = JsonConvert.DeserializeObject<List<int>>(selectedSeats);
            List<string> seatNames = new List<string>();
            foreach (int seatId in seatList)
            {
                // Tìm ghế trong bảng Ghe dựa trên mã ghế
                Ghe seat = db.Ghes.FirstOrDefault(g => g.MaGhe == seatId);

                // Nếu tìm thấy ghế, thêm tên ghế vào danh sách
                if (seat != null)
                {
                    seatNames.Add(seat.TenGhe);
                }
            }
            Session["ListGhe"] = seatList;
            // Lưu danh sách tên ghế vào ViewBag để sử dụng trong View
            ViewBag.SeatNames = seatNames;
            var cb= db.Combos.ToList();
            return View(cb);
        }
        [SavePreviousPage]
        public ActionResult Payment()
        {
            if (Request.QueryString.Count > 0)
            {
                string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"]; //Chuoi bi mat
                var vnpayData = Request.QueryString;
                VnPayLibrary vnpay = new VnPayLibrary();

                foreach (string s in vnpayData)
                {
                    if (!string.IsNullOrEmpty(s) && s.StartsWith("vnp_"))
                    {
                        vnpay.AddResponseData(s, vnpayData[s]);
                    }
                }
                string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
                string vnp_TransactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");
                String vnp_SecureHash = Request.QueryString["vnp_SecureHash"];

                bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);
                if (checkSignature)
                {
                    if (vnp_ResponseCode == "00" && vnp_TransactionStatus == "00")
                    {
                        List<int> listGhe = Session["ListGhe"] as List<int>;
                        List<ComboDat> listcb = Session["ListCB"] as List<ComboDat>;
                        long madh = (long)Session["MaDH"];
                        int macp = (int)Session["MaCP"];
                        foreach (var cb in listcb)
                        {
                            var combo = db.Combos.SingleOrDefault(u => u.MaCB == cb.MaCB);
                            CTHDComBo cthdcb = new CTHDComBo
                            {
                                MaCB = cb.MaCB,
                                SoLuong = cb.SoLuong,
                                GiaTien = combo.Gia.Value,
                                MaHoaDon = madh
                            };
                            db.CTHDComBos.InsertOnSubmit(cthdcb);
                            db.SubmitChanges();
                        }
                        foreach (var ghe in listGhe)
                        {
                            var lc = db.LichChieus.SingleOrDefault(u => u.MaChieuPhim == macp);
                            var g = db.Ghes.SingleOrDefault(u => u.MaPhong == lc.Phong && u.MaGhe == ghe);
                            ChiTietHoaDon cthd = new ChiTietHoaDon
                            {
                                ThoiGianDat = DateTime.Now.Date,
                                ThoiGianChieu = lc.NgayChieu,
                                TongTien = g.GiaTien,
                                MaHoaDon = madh,
                                MaLichChieu = macp,
                                MaGhe = ghe,
                            };
                            db.ChiTietHoaDons.InsertOnSubmit(cthd);
                            db.SubmitChanges();
                        }
                        var dh = db.HoaDons.SingleOrDefault(u => u.MaHoaDon == madh);
                        dh.TrangThai = "Đã thanh toán";
                        db.SubmitChanges();
                        SendEmail(madh);
                    }
                    else
                    {

                        ViewBag.InnerText = "Có lỗi xảy ra trong quá trình xử lý.Mã lỗi: " + vnp_ResponseCode;
                    }
                }
            }
            return View();
        }
        [SavePreviousPage]
        public ActionResult ChonGhe(int id,int MaChieuPhim)
        {
            var lc = db.LichChieus.SingleOrDefault(u => u.MaChieuPhim == MaChieuPhim);
            var cthd = db.ChiTietHoaDons.Where(u => u.MaLichChieu == lc.MaChieuPhim && u.ThoiGianChieu.Value.Date == lc.NgayChieu.Value.Date);
            var ghe = db.Ghes.Where(u => u.MaPhong == lc.Phong);

            foreach (var chiTiet in cthd)
            {
                var gheTrongChiTiet = ghe.FirstOrDefault(g => g.MaGhe == chiTiet.MaGhe);

                if (gheTrongChiTiet != null)
                {
                    gheTrongChiTiet.TrangThai = "Đã đặt";
                }
            }
            Session["MaCP"] = MaChieuPhim;
            // Cập nhật trạng thái "Trống" cho những ghế không trùng khớp
            foreach (var gheKhongTrongChiTiet in ghe.Where(g => !cthd.Any(c => c.MaGhe == g.MaGhe)))
            {
                gheKhongTrongChiTiet.TrangThai = "Trống";
            }
            // Cập nhật thay đổi vào cơ sở dữ liệu
            db.SubmitChanges();

            return View(ghe);
        }
        [SavePreviousPage]
        [HttpGet]
        public ActionResult TimKiem(string searchQuery, string trangThai, int? theLoaiId, int? page)
        {
            int pageSize = 9;
            int pageNumber = (page ?? 1);

            var query = db.Phims.AsQueryable(); // Bắt đầu với tất cả các phim

            // Thêm điều kiện tìm kiếm theo tên phim, đạo diễn, diễn viên và trạng thái
            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(p =>
                    p.TenPhim.Contains(searchQuery) ||
                    p.DaoDien.Contains(searchQuery) ||
                    p.DienVien.Contains(searchQuery));
            }

            if (!string.IsNullOrEmpty(trangThai))
            {
                query = query.Where(p => p.TrangThai.Contains(trangThai));
            }

            // Thêm điều kiện lọc theo thể loại
            if (theLoaiId.HasValue && theLoaiId > 0)
            {
                var tenTheLoai = db.TheLoais
                    .Where(u => u.MaTheLoai == theLoaiId)
                    .Select(u => u.TenTheLoai)
                    .FirstOrDefault();

                query = query.Where(p => p.TheLoai.Contains(tenTheLoai));
            }

            // Thực hiện phân trang và trả về kết quả
            var ketQuaTimKiem = query.ToPagedList(pageNumber, pageSize);

            ViewBag.TT = trangThai;
            ViewBag.TheloaiList = db.TheLoais.ToList();

            return View(ketQuaTimKiem);
        }

        [HttpPost]
        public ActionResult ThemBinhLuan(string noiDung, int maPhim)
        {
            var user = Session["KH"] as ThongTinCaNhan;
            // Lấy thông tin ngày giờ hiện tại
            DateTime thoiGian = DateTime.Now;

            // Lấy thông tin mã khách hàng từ user đăng nhập
            int maKH = user.MaKH;

            // Tạo đối tượng BinhLuan
            BinhLuan binhLuan = new BinhLuan
            {
                ThoiGian = thoiGian,
                NoiDung = noiDung,
                MaPhim = maPhim,
                MaKH = maKH
            };

            // Thêm đối tượng BinhLuan vào cơ sở dữ liệu
            db.BinhLuans.InsertOnSubmit(binhLuan);
            db.SubmitChanges();

            // Chuyển hướng đến action hiển thị chi tiết phim hoặc trang chủ
            return RedirectToAction("Detail", new { id = maPhim });
        }

        public JsonResult GetDetails(int phimId, int? ngayChieu)
        {
            var phim = db.LichChieus.Where(u=>u.MaPhim==phimId);
            var ngonNgu = "";
            
            if (ngayChieu.HasValue)
            {
                phim = phim.Where(lc => lc.NgayChieu == DateTime.Now.Date.AddDays(ngayChieu.Value));
            }
            else
            {
                phim = phim.Where(lc => lc.NgayChieu == DateTime.Now.Date);
            }
            ngonNgu = phim.FirstOrDefault()?.NgonNgu; // Thay bằng dữ liệu thực tế
            List<Time> gioChieuList = phim
      .Where(lc => lc.GioChieu != null)
      .Select(lc => new Time
      {
          MaChieuPhim = lc.MaChieuPhim,
          hours = lc.GioChieu.Value.Hours,
          minutes = lc.GioChieu.Value.Minutes,
          seconds = lc.GioChieu.Value.Seconds
      }) 
      .ToList();
            // Trả về dữ liệu dưới dạng JSON
            return Json(new { phimId = phimId,ngonNgu = ngonNgu, gioChieuList = gioChieuList, ngayChieu=  ngayChieu }, JsonRequestBehavior.AllowGet);
        }
        [SavePreviousPage]
        public ActionResult Detail(int id)
        {
            var binhLuans = db.BinhLuans.Where(u=>u.MaPhim==id)
    .Include(b => b.ThongTinCaNhan) 
    .ToList();

            ViewBag.BL = binhLuans;
            var ct = db.Phims.SingleOrDefault(u=>u.MaPhim==id);
            return View(ct);
        }
        [SavePreviousPage]
        public ActionResult DetailNew(int id)
        {
            var ct = db.Tintucs.SingleOrDefault(u => u.Matin == id);
            return View(ct);
        }
        public ActionResult DetailsHD (long id)
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
                             cthd =hdcb,
                             cb = cb,
                             nuoc = nuoc,
                             doan = doan
                         };
            var hd = db.HoaDons.SingleOrDefault(u=>u.MaHoaDon==id);
            ViewBag.hd = hd;
            ViewBag.cb = cthdcb.ToList();
            ViewBag.ghe = cthd.ToList();
            string orderCode = $"HD{id}"; // Mã đơn hàng

            ViewBag.QRCode = QRCodeGenerator.GenerateQRCode(orderCode);
            return View();
        }
         
       
        private void SendNewPasswordByEmail(string userEmail, string newPassword)
        {
            // Thực hiện việc gửi email
            // Thông tin cấu hình SMTP và Email Server
            string smtpServer = "smtp.gmail.com";
            int smtpPort = 587; // Port của SMTP Server
            string smtpUsername = "thanhtroll0915@gmail.com";
            string smtpPassword = "zpycpcoezgkwsbdk";

            // Địa chỉ email người gửi
            string fromEmail = "thanhtroll0915@gmail.com";

            // Tạo đối tượng MailMessage
            MailMessage mailMessage = new MailMessage(fromEmail, userEmail)
            {
                Subject = "Reset Password",
                Body = $"Mật khẩu mới của bạn là: {newPassword}",
                IsBodyHtml = false
            };

            // Tạo đối tượng SmtpClient
            SmtpClient smtpClient = new SmtpClient(smtpServer, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                EnableSsl = true
            };

            // Gửi email
            try
            {
                smtpClient.Send(mailMessage);
            }
            catch (Exception ex)
            {
                // Xử lý nếu có lỗi khi gửi email
                // Log lỗi hoặc thông báo lỗi cho người quản trị hệ thống
                Console.WriteLine(ex.Message);
            }
        }
        private void SendEmail(long id)
        {
            string qrCode = $"HD{id}"; // Mã đơn hàng
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
            var nguoidung = Session["KH"] as ThongTinCaNhan;
            string email = nguoidung.Email;
            // Tạo nội dung email
            string emailBody = GenerateEmailHtml(id, qrCode, cthdcb.ToList(), cthd.ToList());

            // Thông tin email của bạn
            string senderEmail = "thanhtroll0915@gmail.com";
            string senderPassword = "zpycpcoezgkwsbdk";
            string smtpServer = "smtp.gmail.com";
            int smtpPort = 587; // Hoặc cổng SMTP tương ứng của bạn

            // Thông tin người nhận email
            string recipientEmail = email;

            // Tạo SmtpClient
            SmtpClient smtpClient = new SmtpClient(smtpServer)
            {
                Port = smtpPort,
                Credentials = new NetworkCredential(senderEmail, senderPassword),
                EnableSsl = true
            };

            // Tạo MailMessage
            MailMessage mailMessage = new MailMessage(senderEmail, recipientEmail)
            {
                Subject = "Thông Tin Hóa Đơn",
                Body = emailBody,
                IsBodyHtml = true
            };
            string serverPath = Server.MapPath("~");
            string imagePath = QRCodeGenerator.SaveQRCodeToFile(id, qrCode, serverPath);
            Attachment attachment = new Attachment(imagePath);
            mailMessage.Attachments.Add(attachment);
            try
            {
                smtpClient.Send(mailMessage);
            }
            catch (Exception ex)
            {
                // Xử lý nếu có lỗi khi gửi email
                // Log lỗi hoặc thông báo lỗi cho người quản trị hệ thống
                Console.WriteLine(ex.Message);
            }
        }

        private string GenerateEmailHtml(long id, string qrCode, List<CTHDCB> cb, List<CTHD> ghe)
        {
            var hd = db.HoaDons.SingleOrDefault(u=>u.MaHoaDon==id);
            // Chuỗi HTML cố định
            string html = $@"
        <div class='container mt-4 text-center'>
            <h2 class='text-white' style='font-weight:bold'>Thông Tin Hóa Đơn</h2>

            <p class='text-white' style='font-weight:bold'>Mã QR ở trong file đính kèm:</p>
             <div class='mt-4'>
        <table class='table table-bordered table-dark'>
            <thead>
                <tr>
                    <th>Mã hóa đơn</th>
                    <th>Họ tên</th>
                    <th>Số điện thoại</th>
                    <th>Email</th>
                    <th>Trạng thái</th>
                    <th>Tiền vé</th>
                    <th>Tiền combo</th>
                    <th>Tổng tiền</th>
                </tr>
            </thead>
            <tbody>";


                html += $@"  
                    <tr>
                        <td>{hd.MaHoaDon}</td>
                        <td>{hd.HoTen}</td>
                        <td>{hd.SoDienThoai}</td>
                        <td>{hd.Email}</td>
                        <td>{hd.TrangThai}</td>
                        <td>{hd.TienVe}</td>
                        <td>{hd.TienCombo}</td>
                        <td>{hd.TongTien}</td>
                   </tr>";
                
            html += @"
</tbody>
        </table>
    </div>
            <div class='mt-4'>
                <h3 class='text-white' style='font-weight:bold'>Chi Tiết Combo</h3>
                <table class='table table-bordered table-dark'>
                    <thead> 
                        <tr>
                            <th>Tên Combo</th>
                            <th>Mô tả</th>
                            <th>Tên đồ ăn</th>
                            <th>Số lượng đồ ăn</th>
                            <th>Size đồ ăn</th>
                            <th>Tên nước</th>
                            <th>Số lượng nước</th>
                            <th>Size nước</th>
                            <th>Số lượng combo đã mua</th>
                            <th>Giá Combo</th>
                        </tr>
                    </thead>
                    <tbody>";

            // Thêm dòng cho mỗi phần tử trong danh sách cb
            foreach (var item in cb)
            {
                html += $@"
                        <tr>
                            <td>{item.cb.TenCB}</td>
                            <td>{item.cb.MoTa}</td>
                            <td>{item.doan.TenDoAn}</td>
                            <td>{item.cb.SLDoAn}</td>
                            <td>{item.doan.Loai}</td>
                            <td>{item.nuoc.TenNuoc}</td>
                            <td>{item.cb.SLNuoc}</td>
                            <td>{item.nuoc.Loai}</td>
                            <td>{item.cthd.SoLuong}</td>
                            <td>{item.cthd.GiaTien}</td>
                        </tr>";
            }

            // Thêm phần HTML cho chi tiết ghế vào chuỗi
            html += @"
                    </tbody>
                </table>
            </div>

            <div class='mt-4'>
                <h3 class='text-white' style='font-weight:bold'>Chi Tiết Vé đã mua</h3>
                <table class='table table-bordered table-dark'>
                    <thead>
                        <tr>
                            <th>Tên phim</th>
                            <th>Thời lượng</th>
                            <th>Phòng</th>
                            <th>Tên ghế</th>
                            <th>Thời gian chiếu</th>
                            <th>Giá Tiền</th>
                        </tr>
                    </thead>
                    <tbody>";

            // Thêm dòng cho mỗi phần tử trong danh sách ghe
            foreach (var item in ghe)
            {
                html += $@"
                        <tr>
                            <td>{item.phim.TenPhim}</td>
                            <td>{item.phim.ThoiLuong} phút</td>
                            <td>{item.phong.TenPhong}</td>
                            <td>{item.ghe.TenGhe}</td>
                            <td>{item.lichChieu.NgayChieu:dd/MM/yyyy} {item.lichChieu.GioChieu}</td>
                            <td>{item.chiTiet.TongTien}</td>
                        </tr>";
            }

            // Kết thúc chuỗi HTML
            html += @"
                    </tbody>
                </table>
            </div>
        </div>";

            return html;
        }


        [SavePreviousPage]
        public ActionResult News(int? page)
        {
            int pageSize = 6; // Số sản phẩm trên mỗi trang
            int pageNumber = (page ?? 1); // Trang mặc định là trang 1 nếu không có tham số page
            var tt = db.Tintucs.ToPagedList(pageNumber, pageSize);
            ViewBag.Slider = db.Sliders.ToList();
            return View(tt);
        }
        [SavePreviousPage]
        // GET: Home
        public ActionResult Index()
        {
            var phim = db.Phims.ToList();
            return View(phim);
        }
    }
}