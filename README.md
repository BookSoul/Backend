# BOOKCONSOUL API Documentation

Tài liệu này liệt kê các API hiện có trong solution `BOOKCONSOUL`, mục đích sử dụng và dữ liệu đầu vào cần gửi.

## 1. Tổng quan

- Backend chạy trên .NET 8
- Auth dùng JWT Bearer
- Database: SQL Server
- API chia theo 4 nhóm chính:
  - `guest`: public endpoints
  - `user`: endpoints cho người dùng đã đăng nhập
  - `admin`: endpoints quản trị
  - `auth`: đăng ký / đăng nhập

---

## 2. Authentication APIs

### 2.1 `POST /api/auth/signup`
Đăng ký tài khoản mới.

#### Request body
```json
{
  "fullName": "Nguyen Van A",
  "email": "a@gmail.com",
  "password": "123456",
  "userName": "nguyenvana",
  "address": "Ha Noi"
}
```

#### Ý nghĩa field
- `fullName`: họ tên người dùng
- `email`: email đăng nhập, phải unique
- `password`: mật khẩu
- `userName`: tên đăng nhập
- `address`: địa chỉ mặc định, có thể null

#### Response
- Thành công: trả về token JWT và thông tin cơ bản user

---

### 2.2 `POST /api/auth/login`
Đăng nhập hệ thống.

#### Request body
```json
{
  "email": "a@gmail.com",
  "password": "123456"
}
```

#### Ý nghĩa field
- `email`: email tài khoản
- `password`: mật khẩu

#### Response
- Thành công: token JWT
- Thất bại: unauthorized

---

## 3. Guest APIs

### 3.1 `GET /api/guest/books`
Xem danh sách sách cũ.

#### Query params
- `keyword` string: tìm theo tên sách / tác giả
- `categoryId` guid: lọc theo thể loại
- `condition` string: lọc theo tình trạng sách
- `minPrice` decimal: giá tối thiểu
- `maxPrice` decimal: giá tối đa
- `sortBy` string: tiêu chí sắp xếp

#### Ví dụ
```http
GET /api/guest/books?keyword=harry&minPrice=50000&maxPrice=200000
```

---

### 3.2 `GET /api/guest/books/{id}`
Xem chi tiết một cuốn sách.

#### Path param
- `id` guid: ID cuốn sách

#### Response
- Thông tin chi tiết sách: tên, giá, tồn kho, mô tả, tác giả, thể loại, trạng thái

---

### 3.3 `GET /api/guest/accessories`
Xem danh sách phụ kiện đọc sách.

#### Query params
- `keyword` string: từ khóa
- `brandId` guid: lọc theo thương hiệu
- `typeId` guid: lọc theo loại phụ kiện

#### Ví dụ
```http
GET /api/guest/accessories?keyword=bookmark
```

---

### 3.4 `GET /api/guest/blindbox/tiers`
Xem thông tin các gói Blind Box.

#### Không cần body.

#### Response
Danh sách tier gồm:
- `Normal`
- `Pro`
- `Deluxe`

Mỗi item có:
- `tier` number
- `name` string
- `price` decimal
- `description` string

---

### 3.5 `GET /api/guest/search/live`
Live search gợi ý nhanh.

#### Query params
- `keyword` string: từ khóa tìm kiếm

#### Response
Danh sách gợi ý tối đa 5 item.

---

## 4. User APIs

Tất cả API nhóm này yêu cầu JWT Bearer.

### 4.1 `GET /api/user/cart`
Xem giỏ hàng hiện tại.

#### Response
Danh sách item trong giỏ, thứ tự item mới thêm sẽ ưu tiên trên đầu.

---

### 4.2 `POST /api/user/cart`
Thêm item vào giỏ.

#### Request body
```json
{
  "productId": "00000000-0000-0000-0000-000000000001",
  "productType": 0,
  "quantity": 2
}
```

#### Ý nghĩa field
- `productId`: ID sách hoặc phụ kiện
- `productType`:
  - `0` = `Book`
  - `1` = `Accessory`
- `quantity`: số lượng muốn thêm

#### Rule
- `quantity` phải lớn hơn 0
- không được vượt tồn kho

---

### 4.3 `PUT /api/user/cart/{itemId}`
Cập nhật số lượng item trong giỏ.

#### Path param
- `itemId`: hiện controller đang nhận theo route, nhưng update logic dùng `productId` + `productType` trong body

#### Request body
```json
{
  "productId": "00000000-0000-0000-0000-000000000001",
  "productType": 0,
  "quantity": 3
}
```

#### Rule
- Nếu `quantity <= 0` thì item sẽ bị xóa khỏi giỏ

---

### 4.4 `GET /api/user/orders`
Xem lịch sử đơn hàng của user.

#### Response
Danh sách đơn hàng, bao gồm:
- `id`
- `orderDate`
- `totalAmount`
- `status`
- `paymentMethod`
- chi tiết items

---

### 4.5 `POST /api/user/orders/checkout`
Tạo đơn hàng mới từ giỏ hàng.

#### Request body
```json
{
  "receiverName": "Nguyen Van A",
  "receiverPhone": "0901234567",
  "receiverEmail": "a@gmail.com",
  "shippingAddress": "12 Nguyen Trai, Ha Noi",
  "notes": "Giao giờ hành chính",
  "voucherCode": "WELCOME10",
  "paymentMethod": 0,
  "blindBoxLines": [
    {
      "quantity": 1,
      "unitPrice": 99000
    }
  ]
}
```

#### Ý nghĩa field
- `receiverName`: tên người nhận
- `receiverPhone`: số điện thoại người nhận
- `receiverEmail`: email người nhận
- `shippingAddress`: địa chỉ giao hàng
- `notes`: ghi chú đơn hàng
- `voucherCode`: mã giảm giá, có thể null
- `paymentMethod`:
  - `0` = `COD`
  - `1` = `BankTransfer`
- `blindBoxLines`: các dòng Blind Box nếu có

#### Rule
- Nếu giỏ trống và không có blind box line thì trả lỗi
- Hệ thống check stock trước khi tạo đơn
- Nếu có Blind Box, hệ thống random sách hợp lệ để gán vào đơn

---

### 4.6 `POST /api/user/orders/{id}/cancel`
Hủy đơn hàng.

#### Path param
- `id`: ID đơn hàng

#### Rule
- Chỉ hủy được nếu trạng thái hiện tại là `Pending`

---

### 4.7 `POST /api/user/orders/{id}/reorder`
Mua lại đơn cũ.

#### Path param
- `id`: ID đơn hàng cũ

#### Logic
- Lấy lại items từ đơn cũ
- Đưa lại vào giỏ để user checkout tiếp

---

### 4.8 `POST /api/user/buyback/regular`
Gửi yêu cầu thu mua sách thường.

#### Request body
```json
{
  "proposedPrice": 50000
}
```

#### Rule
- Giá đề xuất phải lớn hơn 0
- Nếu muốn đúng nghiệp vụ đầy đủ, nên gửi thêm ảnh trong phần upload flow của UI

---

### 4.9 `POST /api/user/buyback/blindbox`
Gửi yêu cầu thu mua lại Blind Box.

#### Request body
```json
{
  "proposedPrice": 60000
}
```

#### Rule
- Dùng cho trường hợp bán lại sách từ Blind Box
- Giá duyệt thực tế sẽ do admin xét duyệt theo logic hệ thống

---

### 4.10 `GET /api/user/wishlist`
Xem danh sách yêu thích.

#### Response
Danh sách sản phẩm user đã thích.

---

### 4.11 `POST /api/user/wishlist/{productId}`
Toggle sản phẩm vào/ra wishlist.

#### Query params
- `productType`:
  - `0` = `Book`
  - `1` = `Accessory`

#### Ví dụ
```http
POST /api/user/wishlist/00000000-0000-0000-0000-000000000001?productType=0
```

---

### 4.12 `POST /api/user/reviews`
Tạo review cho sản phẩm.

#### Request body
```json
{
  "bookId": "00000000-0000-0000-0000-000000000001",
  "accessoryId": null,
  "rating": 5,
  "comment": "Sách rất hay"
}
```

#### Ý nghĩa field
- `bookId`: ID sách, có thể null nếu review phụ kiện
- `accessoryId`: ID phụ kiện, có thể null nếu review sách
- `rating`: số sao từ 1 đến 5
- `comment`: bình luận

#### Rule
- Chỉ được review khi user đã có đơn `Delivered` chứa sản phẩm đó

---

### 4.13 `PUT /api/user/reviews/{reviewId}`
Sửa review.

#### Request body
```json
{
  "rating": 4,
  "comment": "Mình chỉnh lại nhận xét"
}
```

#### Rule
- Chỉ owner của review mới sửa được

---

### 4.14 `DELETE /api/user/reviews/{reviewId}`
Xóa review.

#### Rule
- Chỉ owner của review mới xóa được

---

### 4.15 `POST /api/user/donate`
Tạo yêu cầu tặng sách.

#### Request body
```json
{
  "bookTitle": "Doraemon",
  "author": "Fujiko F. Fujio",
  "genre": "Manga",
  "condition": 1,
  "imageUrls": [
    "https://.../1.jpg",
    "https://.../2.jpg",
    "https://.../3.jpg"
  ],
  "cardTemplate": 1,
  "messageContent": "Mình muốn tặng sách cho cộng đồng",
  "donorName": "Nguyen Van A",
  "donorEmail": "a@gmail.com",
  "donorPhone": "0901234567",
  "donorAddress": "Ha Noi",
  "isAnonymous": false
}
```

#### Ý nghĩa field
- `condition`:
  - `0` = `New`
  - `1` = `LikeNew`
  - `2` = `Good`
  - `3` = `Acceptable`
- `cardTemplate`:
  - `0` = `None`
  - `1` = `VintageFlowers`
  - `2` = `MinimalistLines`
  - `3` = `WatercolorDream`
  - `4` = `AutumnLeaves`
- `imageUrls`: tối thiểu 3 ảnh
- `isAnonymous`: true thì ẩn danh

---

## 5. Admin APIs

Tất cả API nhóm này yêu cầu JWT và role `Admin`.

### 5.1 `GET /api/admin/dashboard/summary`
Xem số liệu tổng quan.

#### Response
- tổng doanh thu
- tổng đơn hàng
- đơn chờ xử lý
- số yêu cầu buyback mới

---

### 5.2 `POST /api/admin/books`
Tạo sách mới.

#### Request body
```json
{
  "title": "Doraemon",
  "authorId": "00000000-0000-0000-0000-000000000010",
  "categoryId": "00000000-0000-0000-0000-000000000020",
  "price": 120000,
  "stock": 10,
  "imageUrl": "https://.../book.jpg",
  "description": "Sách hay",
  "isActive": true
}
```

---

### 5.3 `PUT /api/admin/books/{id}`
Cập nhật sách.

#### Request body
Giống `BookUpsertRequest` ở trên.

---

### 5.4 `DELETE /api/admin/books/{id}`
Xóa sách.

---

### 5.5 `POST /api/admin/accessories`
Tạo phụ kiện mới.

#### Request body
```json
{
  "name": "Bookmark Sakura",
  "brandId": "00000000-0000-0000-0000-000000000030",
  "typeId": "00000000-0000-0000-0000-000000000031",
  "price": 35000,
  "stock": 20,
  "imageUrl": "https://.../accessory.jpg",
  "isActive": true
}
```

---

### 5.6 `PUT /api/admin/accessories/{id}`
Cập nhật phụ kiện.

---

### 5.7 `DELETE /api/admin/accessories/{id}`
Xóa phụ kiện.

---

### 5.8 `GET /api/admin/orders`
Liệt kê tất cả đơn hàng.

---

### 5.9 `PUT /api/admin/orders/{id}/status`
Cập nhật trạng thái đơn hàng.

#### Request body
```json
{
  "status": 2
}
```

#### `status` values
- `0` = `Pending`
- `1` = `Processing`
- `2` = `Shipped`
- `3` = `Delivered`
- `4` = `Cancelled`

---

### 5.10 `GET /api/admin/buyback`
Liệt kê các yêu cầu buyback.

---

### 5.11 `PUT /api/admin/buyback/{id}/approve`
Duyệt buyback.

#### Request body
```json
{
  "approvedPrice": 50000,
  "adminNotes": "Đã kiểm tra sách đạt yêu cầu"
}
```

---

### 5.12 `PUT /api/admin/buyback/{id}/reject`
Từ chối buyback.

#### Request body
```json
{
  "reason": "Sách rách, hỏng nặng"
}
```

---

### 5.13 `GET /api/admin/statistics/charts`
Lấy dữ liệu vẽ biểu đồ.

#### Response
Danh sách point theo label/value.

---

### 5.14 `GET /api/admin/statistics/export?format=excel|pdf`
Xuất báo cáo.

#### Query params
- `format`: định dạng xuất file

#### Ví dụ
```http
GET /api/admin/statistics/export?format=pdf
```

---

## 6. Enum Values

### `ProductType`
- `0` = `Book`
- `1` = `Accessory`

### `PaymentMethod`
- `0` = `COD`
- `1` = `BankTransfer`

### `OrderStatus`
- `0` = `Pending`
- `1` = `Processing`
- `2` = `Shipped`
- `3` = `Delivered`
- `4` = `Cancelled`

### `BookCondition`
- `0` = `New`
- `1` = `LikeNew`
- `2` = `Good`
- `3` = `Acceptable`

### `BuybackType`
- `0` = `Regular`
- `1` = `BlindBox`

### `BuybackRequestStatus`
- `0` = `Pending`
- `1` = `Approved`
- `2` = `Rejected`

### `BlindBoxTier`
- `0` = `Normal`
- `1` = `Pro`
- `2` = `Deluxe`

### `DonateCardTemplate`
- `0` = `None`
- `1` = `VintageFlowers`
- `2` = `MinimalistLines`
- `3` = `WatercolorDream`
- `4` = `AutumnLeaves`

---

## 7. Authentication khi test API

Khi gọi các API `user` hoặc `admin`, cần gắn header:

```http
Authorization: Bearer <your-jwt-token>
```

---

## 8. Ghi chú quan trọng

- Một số API admin hiện ưu tiên CRUD theo schema nội bộ đang dùng trong codebase.
- Nếu muốn khớp 100% UI spec frontend, có thể cần thêm các endpoint phụ cho:
  - upload ảnh
  - lọc sâu hơn
  - phân trang chi tiết
  - search theo nhiều tiêu chí hơn

---

## 9. Luồng hoạt động của hệ thống

Dưới đây là luồng hoạt động tổng quát từ lúc hệ thống vừa khởi chạy cho đến khi dữ liệu sách đã đầy đủ:

### 9.1 Khởi động hệ thống
1. Chạy `WebAPI`.
2. `Program.cs` thực hiện:
   - đăng ký service
   - kết nối SQL Server
   - chạy migration tự động
   - gọi `DbSeeder.SeedAsync(...)`
3. Hệ thống tự tạo các dữ liệu nền nếu chưa có:
   - role `Admin`, `Staff`, `Customer`
   - tài khoản mẫu nếu seeder có cấu hình
   - system setting
   - voucher mẫu
   - banner mẫu
   - category, author, brand, accessory type mẫu

### 9.2 Giai đoạn chuẩn bị catalog
Sau khi hệ thống lên, admin hoặc dữ liệu seed sẽ tiếp tục bổ sung:
- thể loại sách
- tác giả
- thương hiệu phụ kiện
- loại phụ kiện
- sách mẫu
- phụ kiện mẫu

Các dữ liệu này là nền để frontend có thể hiển thị trang chủ, trang danh sách và trang chi tiết.

### 9.3 Hiển thị cho khách truy cập
Khi người dùng chưa đăng nhập vào website:
- FE gọi `GET /api/home` để lấy dữ liệu trang chủ
- FE gọi `GET /api/guest/books` để xem danh sách sách
- FE gọi `GET /api/guest/accessories` để xem phụ kiện
- FE gọi `GET /api/guest/blindbox/tiers` để xem các gói Blind Box
- FE gọi `GET /api/guest/search/live` để gợi ý tìm kiếm nhanh

### 9.4 Người dùng đăng ký và sử dụng hệ thống
Khi đã có tài khoản:
1. User đăng ký bằng `POST /api/auth/signup`
2. Đăng nhập bằng `POST /api/auth/login`
3. Nhận JWT token
4. Sử dụng token để:
   - thêm vào giỏ
   - checkout
   - theo dõi đơn hàng
   - wishlist
   - review
   - donate
   - buyback

### 9.5 Giai đoạn dữ liệu sách đầy đủ
Khi admin đã nhập đủ sách thật vào hệ thống:
- `Books` sẽ chứa đầy đủ sách active/inactive
- `HomeService` sẽ lấy sách nổi bật từ `Books`
- `GuestService` sẽ cho phép lọc/sắp xếp sách theo nhu cầu
- `CheckoutService` sẽ kiểm tra tồn kho trước khi tạo đơn
- `ReviewService` chỉ cho review sau đơn `Delivered`

Nói ngắn gọn, hệ thống hoạt động theo chuỗi:

**Khởi động -> Seed dữ liệu nền -> Admin bổ sung catalog -> Guest xem catalog -> User đăng ký/đăng nhập -> User mua hàng / review / donate / buyback -> Admin xử lý đơn và thống kê**

## 10. Khởi chạy dự án

```bash
dotnet build BOOKCONSOUL.sln
dotnet run --project WebAPI
```

Swagger mặc định sẽ mở ở môi trường Development.
