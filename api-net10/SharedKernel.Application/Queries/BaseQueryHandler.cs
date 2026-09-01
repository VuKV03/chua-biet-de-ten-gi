using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedKernel.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharedKernel.Application.Queries
{
    public class BaseQueryHandler<TDbContext, TEntity> // generic class (loại nào cũng được)
        where TDbContext : IBaseDbContext
        where TEntity : class
    {
        // proctected + readonly: chỉ có thể truy cập trong lớp này và các lớp kế thừa (con) , và không thể thay đổi giá trị sau khi khởi tạo
        protected readonly TDbContext _dbContext;
        protected readonly IMapper _mapper;
        protected readonly IQueryable<TEntity> _repo; // viết LINQ (truy vấn csdl)
        protected readonly IConfiguration _config; // đọc appsettings.json

        public BaseQueryHandler(TDbContext dbContext, IMapper mapper, IConfiguration config)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _repo = _dbContext.Set<TEntity>().AsNoTracking(); // AsNoTracking(): không theo dõi các thay đổi của thực thể,
                                                              // giúp tăng hiệu suất khi chỉ đọc dữ liệu
            _config = config;
            // => 3 tham số được tiêm vào tại thời điểm DI container tạo ra 1 instance của class con (GetTodoListQueryHandler),
            // rồi class con chủ động chuyển tiếp chúng xuống constructor của BaseQueryHandler qua từ khóa base(...).
            // BaseQueryHandler tự nó không hề biết gì về DI container — nó chỉ là 1 constructor bình thường nhận tham số,
            // giống hệt cách bất kỳ method nào nhận tham số.
        }
    }
}
