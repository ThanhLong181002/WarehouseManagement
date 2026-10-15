using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using WarehouseManagement.Api.Contracts;
using WarehouseManagement.Api.Data;
using WarehouseManagement.Api.Data.Entities;
using WarehouseManagement.Api.Entities;
using WarehouseManagement.Api.Services;
using WarehouseManagement.Api.Validation;

using System.Security.Claims;          
using Microsoft.AspNetCore.Authorization; 


namespace WarehouseManagement.Api.Endpoints;

public static class StockTransactionEndpoints
{
    public static IEndpointRouteBuilder MapStockTransactionEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/stock-transactions", async (
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] string? search,
            [FromQuery] string? transactionType,
            [FromQuery] string? status,
            [FromQuery] int? warehouseId,
            WarehouseDbContext db,
            CancellationToken cancellationToken) =>
        {
            // Thiết lập giá trị mặc định nếu tham số bị null
            page = page ?? 1;
            pageSize = pageSize ?? 10;

            // Chuyển đổi sang biến cục bộ có giá trị không null để xử lý logic nội bộ
            var currentPage = page.Value;
            var currentPageSize = pageSize.Value;

            // 1. Kiểm tra tính hợp lệ bằng biến cục bộ mới
            if (currentPage < 1)
            {
                return Results.BadRequest(new
                {
                    message = "page must be greater than or equal to 1."
                });
            }

            if (currentPageSize < 1 || currentPageSize > 100)
            {
                return Results.BadRequest(new
                {
                    message = "pageSize must be between 1 and 100."
                });
            }

            if (warehouseId.HasValue && warehouseId.Value <= 0)
            {
                return Results.BadRequest(new
                {
                    message = "warehouseId must be greater than 0."
                });
            }

            // 2. Xây dựng truy vấn bộ lọc dữ liệu (Filters)
            var query = db.StockTransactions.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();
                query = query.Where(x => x.TransactionNumber.Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(transactionType))
            {
                var type = transactionType.Trim();
                query = query.Where(x => x.TransactionType == type);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var transactionStatus = status.Trim();
                query = query.Where(x => x.Status == transactionStatus);
            }

            if (warehouseId.HasValue)
            {
                query = query.Where(x => x.WarehouseId == warehouseId.Value);
            }

            // 3. Thực hiện đếm tổng và phân trang dữ liệu
            var totalCount = await query.CountAsync(cancellationToken);

            var totalPages = (int)Math.Ceiling(totalCount / (double)currentPageSize);

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.StockTransactionId)
                .Skip((currentPage - 1) * currentPageSize)
                .Take(currentPageSize)
                .Select(x => new
                {
                    x.StockTransactionId,
                    x.TransactionNumber,
                    x.TransactionType,
                    x.Status,
                    x.WarehouseId,
                    x.CreatedByUserId,       
                    x.PostedByUserId,        
                    x.ReversesTransactionId,
                    x.Reason,
                    x.CreatedAt,
                    x.PostedAt,
                    x.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            // 4. Trả về kết quả đầu ra chuẩn hóa
            return Results.Ok(new
            {
                page = currentPage,
                pageSize = currentPageSize,
                totalCount,
                totalPages,
                items
            });
        });


        app.MapGet("/api/stock-transactions/{id:int}", async (
            int id,
            WarehouseDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(new
                {
                    message = "id must be greater than 0."
                });
            }

            var transaction = await db.StockTransactions
                .AsNoTracking()
                .Where(x => x.StockTransactionId == id)
                .Select(x => new
                {
                    x.StockTransactionId,
                    x.TransactionNumber,
                    x.TransactionType,
                    x.Status,
                    x.WarehouseId,
                    x.CreatedByUserId,
                    x.PostedByUserId,
                    x.ReversesTransactionId,
                    x.Reason,
                    x.CreatedAt,
                    x.PostedAt,
                    x.UpdatedAt,

                    Details = db.StockTransactionDetails
                        .Where(d => d.StockTransactionId == x.StockTransactionId)
                        .OrderBy(d => d.StockTransactionDetailId)
                        .Select(d => new
                        {
                            d.StockTransactionDetailId,
                            d.MaterialId,
                            MaterialCode = d.Material.MaterialCode,
                            MaterialName = d.Material.MaterialName,
                            BaseUnit = d.Material.BaseUnit,
                            d.Quantity
                        })
                        .ToList()
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (transaction is null)
            {
                return Results.NotFound(new
                {
                    message = $"Stock transaction with ID {id} was not found."
                });
            }

            return Results.Ok(transaction);
        });


        app.MapPost("/api/stock-transactions", async (
            CreateStockTransactionRequest request,
            ClaimsPrincipal userPrincipal,
            WarehouseDbContext db,
            CancellationToken cancellationToken) =>
        {
            var transactionType = request.TransactionType.Trim();

            //var userIdClaim = userPrincipal.FindFirst(ClaimTypes.NameIdentifier);
            //if (userIdClaim == null) return Results.Unauthorized();
            //int authenticatedUserId = int.Parse(userIdClaim.Value);

            int authenticatedUserId = 1;

            //var isUserActive = await db.Users.AnyAsync(u => u.UserId == authenticatedUserId && u.IsActive, cancellationToken);
            //if (!isUserActive) return Results.Json(new { message = "Your account has been locked or deactivated." }, statusCode: 403);

            if (transactionType != "StockIn" &&
                transactionType != "StockOut")
            {
                return Results.BadRequest(new
                {
                    message = "transactionType must be StockIn or StockOut."
                });
            }

            if (request.WarehouseId <= 0 ||
                request.CreatedByUserId <= 0)
            {
                return Results.BadRequest(new
                {
                    message = "WarehouseId and CreatedByUserId must be greater than 0."
                });
            }

            if (request.Details is null || request.Details.Count == 0)
            {
                return Results.BadRequest(new
                {
                    message = "At least one transaction detail is required."
                });
            }

            if (request.Details.Any(d =>
                d.MaterialId <= 0 ||
                !QuantityValidator.IsValid(d.Quantity)))
            {
                return Results.BadRequest(new
                {
                    message = "Each detail must have a valid MaterialId and Quantity with at most 3 decimal places."
                });
            }

            if (request.Details
                .GroupBy(d => d.MaterialId)
                .Any(g => g.Count() > 1))
            {
                return Results.BadRequest(new
                {
                    message = "A material can only appear once per transaction."
                });
            }

            var warehouse = await db.Warehouses
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    w => w.WarehouseId == request.WarehouseId &&
                         w.IsActive,
                    cancellationToken);

            if (warehouse is null)
            {
                return Results.BadRequest(new
                {
                    message = "Warehouse does not exist or is inactive."
                });
            }

            var materialIds = request.Details
                .Select(d => d.MaterialId)
                .ToList();

            var activeMaterialIds = await db.Materials
                .AsNoTracking()
                .Where(m => materialIds.Contains(m.MaterialId) && m.IsActive)
                .Select(m => m.MaterialId)
                .ToListAsync(cancellationToken);

            if (activeMaterialIds.Count != materialIds.Count)
            {
                return Results.BadRequest(new
                {
                    message = "One or more materials do not exist or are inactive."
                });
            }

            var stockTransaction = new StockTransaction
            {
                TransactionNumber =
                    StockTransactionNumberGenerator.Generate(transactionType),
                TransactionType = transactionType,
                Status = "Draft",
                WarehouseId = request.WarehouseId,
                CreatedByUserId = authenticatedUserId,
                Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                StockTransactionDetails = request.Details.Select(d =>
                    new StockTransactionDetail
                    {
                        MaterialId = d.MaterialId,
                        Quantity = d.Quantity
                    }).ToList()
            };

            await using var dbTransaction =
                await db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                db.StockTransactions.Add(stockTransaction);

                await db.SaveChangesAsync(cancellationToken);
                await dbTransaction.CommitAsync(cancellationToken);

                return Results.Created(
                    $"/api/stock-transactions/{stockTransaction.StockTransactionId}",
                    new
                    {
                        stockTransaction.StockTransactionId,
                        stockTransaction.TransactionNumber,
                        stockTransaction.TransactionType,
                        stockTransaction.Status,
                        stockTransaction.WarehouseId,
                        stockTransaction.CreatedByUserId,
                        stockTransaction.Reason,
                        stockTransaction.CreatedAt,
                        Details = stockTransaction.StockTransactionDetails.Select(d => new
                        {
                            d.StockTransactionDetailId,
                            d.MaterialId,
                            d.Quantity
                        })
                    });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                await dbTransaction.RollbackAsync(cancellationToken);

                return Results.Problem(
                    title: "Could not create stock transaction.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        });


        app.MapPost("/api/stock-transactions/{id:int}/post", async (
            int id,
            ClaimsPrincipal userPrincipal,
            WarehouseDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (id <= 0) return Results.BadRequest(new { message = "id must be greater than 0." });

            //var userIdClaim = userPrincipal.FindFirst(ClaimTypes.NameIdentifier);
            //if (userIdClaim == null) return Results.Unauthorized();
            //int authenticatedUserId = int.Parse(userIdClaim.Value);

            int authenticatedUserId = 1;

            // THAY BẰNG KHỐI LỆNH NÀY:
            if (authenticatedUserId <= 0)
            {
                return Results.BadRequest(new { message = "Authenticated user identity is invalid." });
            }


            await using var dbTransaction =
                await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

            try
            {
                // Khóa bản ghi phiếu trong transaction để tránh hai request
                // cùng ghi sổ một phiếu.
                var lockedTransaction = await db.Database
                    .SqlQueryRaw<int>(
                        """
                SELECT StockTransactionId AS [Value]
                FROM dbo.StockTransactions WITH (UPDLOCK, HOLDLOCK)
                WHERE StockTransactionId = {0}
                """,
                        id)
                    .SingleOrDefaultAsync(cancellationToken);

                if (lockedTransaction == 0)
                {
                    await dbTransaction.RollbackAsync(cancellationToken);

                    return Results.NotFound(new
                    {
                        message = $"Stock transaction with ID {id} was not found."
                    });
                }

                var activeUserId = await db.Database
                    .SqlQueryRaw<int>(
                        """
                        SELECT UserId AS [Value]
                        FROM dbo.Users
                        WHERE UserId = {0} AND IsActive = 1
                        """,
                        authenticatedUserId) // <-- SỬA CHỖ NÀY (Thay request.PostedByUserId)
                    .SingleOrDefaultAsync(cancellationToken);


                if (activeUserId == 0)
                {
                    await dbTransaction.RollbackAsync(cancellationToken);

                    return Results.BadRequest(new
                    {
                        message = "User does not exist or is inactive."
                    });
                }

                var stockTransaction = await db.StockTransactions
                    .Include(x => x.StockTransactionDetails)
                    .SingleOrDefaultAsync(
                        x => x.StockTransactionId == id,
                        cancellationToken);

                if (stockTransaction is null)
                {
                    await dbTransaction.RollbackAsync(cancellationToken);

                    return Results.NotFound(new
                    {
                        message = $"Stock transaction with ID {id} was not found."
                    });
                }

                if (stockTransaction.Status != "Draft")
                {
                    await dbTransaction.RollbackAsync(cancellationToken);

                    return Results.Conflict(new
                    {
                        message = "Only Draft transactions can be posted."
                    });
                }

                if (stockTransaction.TransactionType != "StockIn" &&
                    stockTransaction.TransactionType != "StockOut")
                {
                    await dbTransaction.RollbackAsync(cancellationToken);

                    return Results.BadRequest(new
                    {
                        message = "Only StockIn and StockOut can be posted."
                    });
                }

                if (stockTransaction.StockTransactionDetails.Count == 0)
                {
                    await dbTransaction.RollbackAsync(cancellationToken);

                    return Results.BadRequest(new
                    {
                        message = "Cannot post a transaction without details."
                    });
                }

                // 6. Cập nhật tồn kho theo thứ tự MaterialId để giảm deadlock.
                foreach (var detail in stockTransaction.StockTransactionDetails
                             .OrderBy(d => d.MaterialId))
                {
                    if (stockTransaction.TransactionType == "StockIn")
                    {
                        // Cộng tồn nếu đã có bản ghi.
                        var affectedRows = await db.Database.ExecuteSqlInterpolatedAsync(
                            $"""
                            UPDATE dbo.InventoryBalances WITH (UPDLOCK, HOLDLOCK)
                            SET QuantityOnHand = QuantityOnHand + {detail.Quantity},
                                UpdatedAt = SYSUTCDATETIME()
                            WHERE MaterialId = {detail.MaterialId}
                              AND WarehouseId = {stockTransaction.WarehouseId};
                            """,
                            cancellationToken);

                        // Chưa có bản ghi thì tạo mới.
                        if (affectedRows == 0)
                        {
                            await db.Database.ExecuteSqlInterpolatedAsync(
                                $"""
                                INSERT INTO dbo.InventoryBalances
                                    (MaterialId, WarehouseId, QuantityOnHand, UpdatedAt)
                                VALUES
                                    ({detail.MaterialId},
                                     {stockTransaction.WarehouseId},
                                     {detail.Quantity},
                                     SYSUTCDATETIME());
                                """,
                                cancellationToken);
                        }
                    }
                    else
                    {
                        // Chỉ trừ tồn khi bản ghi tồn tại và đủ số lượng.
                        var affectedRows = await db.Database.ExecuteSqlInterpolatedAsync(
                            $"""
                            UPDATE dbo.InventoryBalances WITH (UPDLOCK, HOLDLOCK)
                            SET QuantityOnHand = QuantityOnHand - {detail.Quantity},
                                UpdatedAt = SYSUTCDATETIME()
                            WHERE MaterialId = {detail.MaterialId}
                              AND WarehouseId = {stockTransaction.WarehouseId}
                              AND QuantityOnHand >= {detail.Quantity};
                            """,
                            cancellationToken);

                        if (affectedRows != 1)
                        {
                            await dbTransaction.RollbackAsync(cancellationToken);

                            return Results.Conflict(new
                            {
                                message =
                                    $"Insufficient stock for MaterialId {detail.MaterialId}.",
                                materialId = detail.MaterialId
                            });
                        }
                    }
                }

                // 7. Chỉ chuyển trạng thái sau khi mọi chi tiết đã xử lý thành công.
                var postedAt = DateTime.UtcNow;

                stockTransaction.Status = "Posted";
                stockTransaction.PostedByUserId = authenticatedUserId;
                stockTransaction.PostedAt = postedAt;
                stockTransaction.UpdatedAt = postedAt;

                await db.SaveChangesAsync(cancellationToken);

                // 8. Commit cả cập nhật tồn kho lẫn trạng thái phiếu.
                await dbTransaction.CommitAsync(cancellationToken);

                return Results.Ok(new
                {
                    message = "Stock transaction posted successfully.",
                    stockTransaction.StockTransactionId,
                    stockTransaction.TransactionNumber,
                    stockTransaction.TransactionType,
                    stockTransaction.Status,
                    stockTransaction.WarehouseId,
                    stockTransaction.PostedByUserId,
                    stockTransaction.PostedAt
                });

            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync(CancellationToken.None);

                return Results.Problem(
                    title: "Could not validate stock transaction for posting.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        });


        // Cancel a Draft stock transaction.
        app.MapPost("/api/stock-transactions/{id:int}/cancel", async (
            int id,
            WarehouseDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(new
                {
                    message = "id must be greater than 0."
                });
            }

            await using var dbTransaction =
                await db.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable,
                    cancellationToken);

            try
            {
                // Lock the transaction row so concurrent requests
                // cannot cancel the same transaction successfully.
                var lockedTransactionId = await db.Database
                    .SqlQueryRaw<int>(
                        """
                        SELECT StockTransactionId AS [Value]
                        FROM dbo.StockTransactions WITH (UPDLOCK, HOLDLOCK)
                        WHERE StockTransactionId = {0}
                        """,
                        id)
                    .SingleOrDefaultAsync(cancellationToken);

                if (lockedTransactionId == 0)
                {
                    await dbTransaction.RollbackAsync(cancellationToken);

                    return Results.NotFound(new
                    {
                        message = $"Stock transaction with ID {id} was not found."
                    });
                }

                var stockTransaction = await db.StockTransactions
                    .SingleAsync(
                        x => x.StockTransactionId == id,
                        cancellationToken);

                if (stockTransaction.Status != "Draft")
                {
                    await dbTransaction.RollbackAsync(cancellationToken);

                    return Results.Conflict(new
                    {
                        message = "Only Draft transactions can be cancelled.",
                        status = stockTransaction.Status
                    });
                }

                stockTransaction.Status = "Cancelled";
                stockTransaction.UpdatedAt = DateTime.UtcNow;

                await db.SaveChangesAsync(cancellationToken);
                await dbTransaction.CommitAsync(cancellationToken);

                return Results.Ok(new
                {
                    stockTransaction.StockTransactionId,
                    stockTransaction.TransactionNumber,
                    stockTransaction.Status,
                    stockTransaction.UpdatedAt
                });
            }
            catch (OperationCanceledException)
            {
                // The request was cancelled; let ASP.NET handle it.
                throw;
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync(cancellationToken);

                return Results.Problem(
                    title: "Could not cancel stock transaction.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        });

        app.MapPost("/api/stock-transactions/{id:int}/reverse", async (
            int id,
            ClaimsPrincipal userPrincipal,
            WarehouseDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(new { message = "Original transaction ID must be greater than zero." });
            }

            // Bắt đầu DB Transaction với IsolationLevel cao nhất để bảo vệ dữ liệu tồn kho
            await using var dbTransaction = await db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable,
                cancellationToken);

            try
            {
                // Tiêu chí chống trùng: Lock dòng dữ liệu bằng SQL Raw tương tự hàm Cancel của bạn
                var lockedTransactionId = await db.Database
                    .SqlQueryRaw<int>(
                        """
                        SELECT StockTransactionId AS [Value]
                        FROM dbo.StockTransactions WITH (UPDLOCK, HOLDLOCK)
                        WHERE StockTransactionId = {0}
                        """,
                        id)
                    .SingleOrDefaultAsync(cancellationToken);

                // Tiêu chí nghiệm thu: Phiếu không tồn tại trả về 404
                if (lockedTransactionId == 0)
                {
                    await dbTransaction.RollbackAsync(cancellationToken);
                    return Results.NotFound(new { message = $"Stock transaction with ID {id} was not found." });
                }

                // Tải đầy đủ thông tin phiếu gốc và danh sách chi tiết mặt hàng qua StockTransactionDetails
                var originalTransaction = await db.StockTransactions
                    .Include(t => t.StockTransactionDetails)
                    .SingleAsync(t => t.StockTransactionId == id, cancellationToken);

                // Tiêu chí nghiệm thu: Phiếu Draft không thể bị đảo ngược (Chỉ nhận Posted)
                if (originalTransaction.Status != "Posted")
                {
                    await dbTransaction.RollbackAsync(cancellationToken);
                    return Results.Conflict(new
                    {
                        message = "Only Posted transactions can be reversed.",
                        currentStatus = originalTransaction.Status
                    });
                }

                // Tiêu chí nghiệm thu: Một phiếu đã đảo ngược không thể bị đảo ngược lần nữa
                var alreadyReversed = await db.StockTransactions
                    .AnyAsync(t => t.ReversesTransactionId == id, cancellationToken);

                if (alreadyReversed)
                {
                    await dbTransaction.RollbackAsync(cancellationToken);
                    return Results.Conflict(new { message = "This transaction has already been reversed." });
                }

                var now = DateTime.UtcNow;

                // Quy tắc nghiệp vụ: Đảo ngược loại phiếu (StockIn -> StockOut / StockOut -> StockIn)
                string reversedType = originalTransaction.TransactionType == "StockIn" ? "StockOut" : "StockIn";

                var userIdClaim = userPrincipal.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null) return Results.Unauthorized();
                int authenticatedUserId = int.Parse(userIdClaim.Value);

                var reverseTransaction = new StockTransaction
                {
                    TransactionNumber = $"REV-{originalTransaction.TransactionNumber}-{now:yyyyMMddHHmmss}",
                    TransactionType = reversedType,
                    Status = "Posted", // Phiếu đảo ngược tự động Posted thành công luôn
                    WarehouseId = originalTransaction.WarehouseId,
                    CreatedByUserId = authenticatedUserId,
                    PostedByUserId = authenticatedUserId,
                    ReversesTransactionId = originalTransaction.StockTransactionId, // Lưu vết ID phiếu gốc chính xác
                    Reason = $"Reversal of transaction {originalTransaction.TransactionNumber}.",
                    CreatedAt = now,
                    PostedAt = now,
                    UpdatedAt = now,
                    StockTransactionDetails = new List<StockTransactionDetail>()
                };

                // Xử lý biến động tồn kho chi tiết trên bảng InventoryBalances (Tính nguyên tử - Atomicity)
                foreach (var originalDetail in originalTransaction.StockTransactionDetails)
                {
                    var balance = await db.InventoryBalances
                        .SingleOrDefaultAsync(b =>
                            b.WarehouseId == originalTransaction.WarehouseId &&
                            b.MaterialId == originalDetail.MaterialId,
                            cancellationToken);

                    if (originalTransaction.TransactionType == "StockIn")
                    {
                        // Quy tắc: Đảo ngược StockIn làm giảm tồn đúng số lượng (Trừ QuantityOnHand)
                        // Tiêu chí: Thiếu tồn không gây cập nhật một phần (Thất bại lập tức Rollback)
                        if (balance is null || balance.QuantityOnHand < originalDetail.Quantity)
                        {
                            await dbTransaction.RollbackAsync(cancellationToken);
                            return Results.BadRequest(new
                            {
                                message = $"Incomplete stock inventory. Material ID {originalDetail.MaterialId} does not have enough quantity to reverse."
                            });
                        }
                        balance.QuantityOnHand -= originalDetail.Quantity;
                        balance.UpdatedAt = now;
                    }
                    else
                    {
                        // Quy tắc: Đảo ngược StockOut làm tăng tồn đúng số lượng (Cộng QuantityOnHand)
                        if (balance is null)
                        {
                            balance = new InventoryBalance
                            {
                                WarehouseId = originalTransaction.WarehouseId,
                                MaterialId = originalDetail.MaterialId,
                                QuantityOnHand = 0m,
                                UpdatedAt = now
                            };
                            db.InventoryBalances.Add(balance);
                        }
                        balance.QuantityOnHand += originalDetail.Quantity;
                        balance.UpdatedAt = now;
                    }

                    reverseTransaction.StockTransactionDetails.Add(new StockTransactionDetail
                    {
                        MaterialId = originalDetail.MaterialId,
                        Quantity = originalDetail.Quantity
                    });
                }

                // Cập nhật trạng thái phiếu gốc sang Reversed để đánh dấu tường minh (Không xóa/sửa)
                originalTransaction.Status = "Reversed";
                originalTransaction.UpdatedAt = now;

                db.StockTransactions.Add(reverseTransaction);
                await db.SaveChangesAsync(cancellationToken);

                // Commit tất cả các thay đổi đồng thời trong cùng một Transaction
                await dbTransaction.CommitAsync(cancellationToken);

                return Results.Ok(new
                {
                    reverseTransaction.StockTransactionId,
                    reverseTransaction.TransactionNumber,
                    reverseTransaction.Status,
                    reverseTransaction.ReversesTransactionId
                });
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync(cancellationToken);
                return Results.Problem(title: "Could not reverse stock transaction.", statusCode: 500);
            }
        });


        return app;
    }
}
