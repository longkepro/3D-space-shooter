namespace VirtualRail
{
    /// <summary>
    /// Giao diện dành cho các thực thể được quản lý bởi Object Pool (Quái vật, Thiên thạch, v.v.),
    /// cho phép thu hồi và tái sử dụng sạch sẽ mà không gọi Destroy(gameObject).
    /// </summary>
    public interface IPoolableEntity
    {
        void Recycle();
    }
}
