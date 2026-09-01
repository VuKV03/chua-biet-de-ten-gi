using System;
using System.Collections.Generic;
using System.Text;

namespace SharedKernel.Application.DTO
{
    public class PagedResult<T>
    {
        public IReadOnlyList<T> items { get; set; } = []; // IReadOnlyList: Thuộc .net - Chỉ đọc không được sửa
                                                          // <T> là generic type parameter — tham số kiểu chung (mọi loại dl)
        public int totalCount { get; set; }
        public int page { get; set; }
        public int pageSize { get; set; }
        public int totalPages => pageSize <= 0 ? 0 : (int)Math.Ceiling((double)totalCount / pageSize);

    }
}
