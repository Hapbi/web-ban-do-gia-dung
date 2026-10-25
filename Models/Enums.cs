namespace NovaShop.Models;

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Packing = 2,
    Shipping = 3,
    Delivered = 4,
    Cancelled = 5,
    Returned = 6
}

public enum PaymentStatus
{
    Unpaid = 0,
    Paid = 1,
    Refunded = 2
}

public enum PaymentMethod
{
    COD = 0,
    BankTransfer = 1
}

public enum ReviewStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public enum DiscountType
{
    Percentage = 0,
    FixedAmount = 1
}

public enum ListingStatus { NotListing = 0, Pending = 1, Approved = 2, Rejected = 3 }
public enum ContactType { Contact = 0, Feedback = 1, Complaint = 2 }
public enum ContactStatus { New = 0, Processing = 1, Resolved = 2 }

public static class EnumLabels
{
    public static string Label(this OrderStatus s) => s switch
    {
        OrderStatus.Pending => "Chờ xác nhận",
        OrderStatus.Confirmed => "Đã xác nhận",
        OrderStatus.Packing => "Đang đóng gói",
        OrderStatus.Shipping => "Đang giao",
        OrderStatus.Delivered => "Đã giao",
        OrderStatus.Cancelled => "Đã hủy",
        OrderStatus.Returned => "Đã trả hàng",
        _ => s.ToString()
    };

    public static string Label(this PaymentStatus s) => s switch
    {
        PaymentStatus.Unpaid => "Chưa thanh toán",
        PaymentStatus.Paid => "Đã thanh toán",
        PaymentStatus.Refunded => "Đã hoàn tiền",
        _ => s.ToString()
    };

    public static string Label(this PaymentMethod m) => m switch
    {
        PaymentMethod.COD => "Thanh toán khi nhận hàng (COD)",
        PaymentMethod.BankTransfer => "Chuyển khoản ngân hàng",
        _ => m.ToString()
    };


    public static string Label(this ListingStatus s) => s switch
    {
        ListingStatus.NotListing => "Không phải marketplace",
        ListingStatus.Pending => "Chờ duyệt",
        ListingStatus.Approved => "Đã duyệt",
        ListingStatus.Rejected => "Từ chối",
        _ => s.ToString()
    };

    public static string Label(this ContactType t) => t switch
    {
        ContactType.Contact => "Liên hệ",
        ContactType.Feedback => "Phản ánh",
        ContactType.Complaint => "Khiếu nại",
        _ => t.ToString()
    };

    public static string Label(this ContactStatus s) => s switch
    {
        ContactStatus.New => "Mới",
        ContactStatus.Processing => "Đang xử lý",
        ContactStatus.Resolved => "Đã xử lý",
        _ => s.ToString()
    };

    public static string Color(this OrderStatus s) => s switch
    {
        OrderStatus.Pending => "amber",
        OrderStatus.Confirmed => "sky",
        OrderStatus.Packing => "indigo",
        OrderStatus.Shipping => "purple",
        OrderStatus.Delivered => "emerald",
        OrderStatus.Cancelled => "red",
        OrderStatus.Returned => "orange",
        _ => "slate"
    };
}


